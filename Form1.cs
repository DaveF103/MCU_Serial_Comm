using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO.Ports;
using System.Xml.Linq;
using static System.Collections.Specialized.BitVector32;


namespace MCU_Serial_Comm
{

    public partial class Form1 : Form
    {
        // Declarations

        // See "IniFile.cs" class
        IniFile ini = new IniFile(@"..\..\..\settings.ini");
        // For release version, put ini in same directory as .exe
        //IniFile ini = new IniFile("settings.ini");

        string iniSectionSelected = "";

        // DataGridView variables
        private DataGridView dgv = new DataGridView();
        private DataGridViewColumn dgvType = new DataGridViewColumn();
        private DataGridViewColumn dgvSymbol = new DataGridViewColumn();
        private DataGridViewColumn dgvName = new DataGridViewColumn();
        private DataGridViewColumn dgvValue = new DataGridViewColumn();
        private DataGridViewColumn dgvAction = new DataGridViewColumn();
        private DataGridViewButtonColumn dgvBtns = new DataGridViewButtonColumn();
        private string[] inSyms = { "", "" };        // Symbols to look for incoming values from MCU, placeholder array until read from ini
        int grSize;

        // SerialPort Variables
        private SerialPort serPort1 = new SerialPort();
        private int serialCode;
        private int baudRate;
        private delegate void spDelegate(string sData);

        // Other controls and variables
        private RichTextBox rtbIn = new RichTextBox();
        private RichTextBox rtbOut = new RichTextBox();
        private TextBox manualOut = new TextBox();
        private Button manualSend = new Button();
        private Label labelDgv = new Label();
        private Label labelOut = new Label();
        private Label labelIn = new Label();
        private Label labelGraph = new Label();

        Stopwatch sw = new Stopwatch();
        double microsPerTick;

        // Graphics Test
        private PictureBox pictureBox1 = new PictureBox();
        // Cache font instead of recreating font objects each time we paint.
        private Font fnt = new Font("Arial", 10);

        private struct IniSummary
        {
            public int[] baudRates;
            public int[] commIDs;
            public string[] names;
        };

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            SetupLayout();
        }

        private void SetupLayout()
        {
            // Start stopwatch
            sw.Start();

            this.Size = new Size(800, 800);
            this.Location = new Point(2100, 1100);
            this.Text = "MCU Serial Control and Monitor";

            // Stopwatch setup
            if (Stopwatch.IsHighResolution)
            {
                Debug.WriteLine("Stopwatch.IsHighResolution = true");
            }
            else
            {
                Debug.WriteLine("Stopwatch.IsHighResolution = false");
            }
            var swf = Stopwatch.Frequency;
            microsPerTick = (1000000.0) / (double)swf;
            Debug.WriteLine(microsPerTick.ToString() + " micro seconds (us) per Tick");

            // General Form Setup
            this.BackColor = Color.Black;
            this.ForeColor = Color.White;

            // Control Placement
            this.Location = new Point(100, 100);
            this.ClientSize = new Size(850, 800);
            dgv.Location = new Point(25, 25);
            dgv.Size = new Size(450, 525);
            manualOut.Location = new Point(500, 25);
            manualOut.Size = new Size(250, 25);
            manualSend.Location = new Point(775, 25);
            manualSend.Size = new Size(50, 25);
            rtbOut.Location = new Point(500, 50);
            rtbOut.Size = new Size(325, 225);
            rtbIn.Location = new Point(500, 300);
            rtbIn.Size = new Size(325, 250);
            pictureBox1.Location = new Point(25, 575);
            pictureBox1.Size = new Size(800, 200);
            labelDgv.Location = new Point(25, 8);
            labelDgv.Size = new Size(175, 17);
            labelOut.Location = new Point(500, 8);
            labelOut.Size = new Size(225, 17);
            labelIn.Location = new Point(500, 283);
            labelIn.Size = new Size(225, 17);
            labelGraph.Location = new Point(25, 558);
            labelGraph.Size = new Size(175, 17);

            // Labels
            labelDgv.Text = "Data Structure for MCU";
            labelOut.Text = "Serial Out to MCU";
            labelIn.Text = "Serial In from MCU";
            labelGraph.Text = "Graph";

            Controls.Add(labelDgv);
            Controls.Add(labelOut);
            Controls.Add(labelIn);
            Controls.Add(labelGraph);

            // DataGridView Control
            dgv.RowHeadersDefaultCellStyle.BackColor = Color.Black;
            dgv.RowHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.EnableHeadersVisualStyles = false;

            dgvType.CellTemplate = new DataGridViewTextBoxCell();
            dgvSymbol.CellTemplate = new DataGridViewTextBoxCell();
            dgvName.CellTemplate = new DataGridViewTextBoxCell();
            dgvValue.CellTemplate = new DataGridViewTextBoxCell();
            dgvAction.CellTemplate = new DataGridViewTextBoxCell();

            dgvType.DefaultCellStyle.BackColor = Color.Black;
            dgvSymbol.DefaultCellStyle.BackColor = Color.Black;
            dgvName.DefaultCellStyle.BackColor = Color.Black;
            dgvValue.DefaultCellStyle.BackColor = Color.Black;
            dgvAction.DefaultCellStyle.BackColor = Color.Black;
            dgvBtns.DefaultCellStyle.BackColor = Color.Black;

            dgv.Columns.Add(dgvType);
            dgv.Columns.Add(dgvSymbol);
            dgv.Columns.Add(dgvName);
            dgv.Columns.Add(dgvValue);
            dgv.Columns.Add(dgvAction);
            dgv.Columns.Add(dgvBtns);

            dgvType.HeaderText = "Type";
            dgvSymbol.HeaderText = "Symbol";
            dgvName.HeaderText = "Name";
            dgvValue.HeaderText = "Value";
            dgvValue.ValueType = typeof(int);
            dgvAction.HeaderText = "Action";

            dgv.CellContentClick += Dgv_CellContentClick;
            dgv.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
            dgv.CellValueChanged += Dgv_CellValueChanged;
            dgv.RowPostPaint += Dgv_RowPostPaint;

            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            Controls.Add(dgv);

            // TextBox for manual entry of Serial input to MCU
            manualOut.BorderStyle = BorderStyle.Fixed3D;
            manualOut.BackColor = Color.Black;
            manualOut.ForeColor = Color.White;
            Controls.Add(manualOut);

            // Button to send Manual Text to MCU
            manualSend.Text = "Send";
            Controls.Add(manualSend);
            manualSend.Click += ManualSend_Click!;

            // RichTextBox = Serial messages OUT to MCU
            rtbOut.BorderStyle = BorderStyle.Fixed3D;
            rtbOut.BackColor = Color.Black;
            rtbOut.ForeColor = Color.White;
            rtbOut.HideSelection = false;
            Controls.Add(rtbOut);

            // RichTextBox = Serial messages IN from MCU
            rtbIn.BorderStyle = BorderStyle.Fixed3D;
            rtbIn.BackColor = Color.Black;
            rtbIn.ForeColor = Color.White;
            rtbIn.HideSelection = false;
            Controls.Add(rtbIn);

            // PictureBox for some graphics
            pictureBox1.BackColor = Color.FromArgb(25, 25, 25);
            // Connect the Paint event of the PictureBox to the event handler method.
            //pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(this.pictureBox1_Paint);
            pictureBox1.Paint += new System.Windows.Forms.PaintEventHandler(pictureBox1_Paint!);
            // Add the PictureBox control to the Form.
            this.Controls.Add(pictureBox1);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            IniSummary iniSummary = new IniSummary();
            int sectionIndex;

            // In know this is poor form, but am still learning about thread safety in C#
            Application.DoEvents();

            // Ini - Get sections, unique baudRates and commID's
            iniSummary = GetIniBZ();
            WriteIniDataToRTB(iniSummary);
            Application.DoEvents();
            sectionIndex = FindPortWithIniData(iniSummary);
            OpenSerialPort(iniSummary, sectionIndex);
        }

        private IniSummary GetIniBZ()
        {
            IniSummary rv;
            int iRow = 0;
            int j = 0;
            int sectCount = 0;
            int baudRateCount = 0;
            int commIDCount = 0;
            int val = 0;
            string strItemPrefix = "row_";
            string strRow = "";
            string iniItem = "";
            bool foundNextRow = true;
            bool baudRateAlreadyAdded;
            int[] baudRates = { 0, 1 };
            int[] commIDs = { 0, 1 };
            string[] names = { "MCU1", "MCU2" };

            string[] sects = ini.GetSections();
            if (sects[0] == "NoSections0")
            {
                MessageBox.Show("No Sections found in " + ini.GetPath());
                rv.baudRates = baudRates;
                rv.commIDs = commIDs;
                rv.names = names;
            }
            else
            {
                // Populate baudRates[] and commIDs[] arrays
                foreach (string s in sects)
                {
                    sectCount++;
                    Array.Resize(ref names, sectCount);
                    names[sectCount - 1] = s;

                    //programList.Items.Add(s);
                    iniItem = strItemPrefix + "0";
                    foundNextRow = true;
                    iRow = 0;
                    while (foundNextRow)
                    {
                        // Read Ini value as string
                        strRow = ini.Read(iniItem, s);
                        // Split string into comma separated substrings (Type, Symbol, Name, Value, Button, Action)
                        string[] gridRow = strRow.Substring(0).Split(',');
                        for (j = 0; j < 6; j++)
                        {
                            gridRow[j] = gridRow[j].Trim();
                        }
                        val = Convert.ToInt32(gridRow[3]);
                        if (gridRow[1] == "B")
                        {
                            // Don't add the same baudRate again
                            baudRateAlreadyAdded = false;
                            foreach (int b in baudRates)
                            {
                                if (b == val) baudRateAlreadyAdded = true;
                            }
                            if (!baudRateAlreadyAdded)
                            {
                                baudRateCount++;
                                Array.Resize(ref baudRates, baudRateCount);
                                baudRates[baudRateCount - 1] = val;
                            }
                        }
                        if (gridRow[1] == "Z")
                        {
                            commIDCount++;
                            Array.Resize(ref commIDs, commIDCount);
                            commIDs[commIDCount - 1] = val;
                        }
                        iRow++;
                        iniItem = strItemPrefix + iRow.ToString();
                        if (ini.KeyExists(iniItem, s) == false) foundNextRow = false;
                    }
                }
                rv.baudRates = baudRates;
                rv.commIDs = commIDs;
                rv.names = names;
            }
            return rv;
        }

        private void WriteIniDataToRTB(IniSummary iniSummary)
        {
            int i;
            string logAppend;
            logAppend = "Found " + iniSummary.names.GetLength(0).ToString() + " ini sections:\n";
            rtbOut.AppendText(logAppend);
            for (i = 0; i < iniSummary.names.GetLength(0); i++)
            {
                logAppend = "  " + i.ToString() + ". " + iniSummary.names[i] + "\n";
                rtbOut.AppendText(logAppend);
            }

            logAppend = "Found " + iniSummary.baudRates.GetLength(0).ToString() + " baudRates:\n";
            rtbOut.AppendText(logAppend);
            for (i = 0; i < iniSummary.baudRates.GetLength(0); i++)
            {
                logAppend = "  " + i.ToString() + ". " + iniSummary.baudRates[i].ToString() + "\n";
                rtbOut.AppendText(logAppend);
            }

            logAppend = "Found " + iniSummary.commIDs.GetLength(0).ToString() + " commID's:\n";
            rtbOut.AppendText(logAppend);
            for (i = 0; i < iniSummary.commIDs.GetLength(0); i++)
            {
                logAppend = "  " + i.ToString() + ". " + iniSummary.commIDs[i].ToString() + "\n";
                rtbOut.AppendText(logAppend);
            }
        }

        private int FindPortWithIniData(IniSummary iniSummary)
        {
            int rv;
            int spCount;
            int i;
            string[] spNames;
            string commID;
            string strFromSP;
            bool successOpen;
            bool successReadLn;
            // Find the Serial Port MCU is Connected to
            // From: https://stackoverflow.com/questions/25354134/programmatically-automating-getting-the-correct-com-port-based-on-the-serial-de
            rv = -1;
            spNames = SerialPort.GetPortNames();
            spCount = spNames.Count();
            if (spCount > 0)
            {
                foreach (string port in spNames)
                {
                    rtbOut.AppendText("Trying port: " + port + "\n");
                    foreach (int br in iniSummary.baudRates)
                    {
                        rtbOut.AppendText("  at " + br.ToString() + " baud\n");
                        SerialPort sp = new SerialPort(port, br, Parity.None, 8, StopBits.One);
                        sp.Handshake = Handshake.None;
                        sp.ReadTimeout = 250;
                        sp.DtrEnable = false;
                        successOpen = true;
                        try
                        {
                            sp.Open();
                            // sp.Open() restarts the MCU, tried to disable DTR to avoid this, but it failed
                            //sp.DtrEnable = false;
                        }
                        catch (Exception ex)
                        {
                            rtbOut.AppendText("    sp.Open(Exception) = " + ex + "\n");
                            successOpen = false;
                        }
                        if (successOpen)
                        {
                            rtbOut.AppendText("    sp.Open successful\n");
                            Thread.Sleep(2000);
                            sp.WriteLine("Z");
                            strFromSP = "";
                            successReadLn = true;
                            try
                            {
                                strFromSP = sp.ReadLine();
                            }
                            catch (Exception ex)
                            {
                                //MessageBox.Show(ex.Message);
                                rtbOut.AppendText("      sp.ReadLine(Exception) = " + ex + "\n");
                                successReadLn = false;
                            }
                            if (successReadLn)
                            {
                                strFromSP = strFromSP.Trim('\r', '\n');
                                for (i = 0; i < iniSummary.commIDs.GetLength(0); i++)
                                {
                                    commID = iniSummary.commIDs[i].ToString();
                                    if (strFromSP == commID)
                                    {
                                        serPort1.PortName = sp.PortName;
                                        serPort1.BaudRate = sp.BaudRate;
                                        serPort1.DtrEnable = sp.DtrEnable;
                                        serPort1.DataReceived += serPort1_DataReceived;
                                        rtbOut.AppendText("      commID " + commID + " found\n");
                                        sp.Close();
                                        rv = i;
                                        return rv;
                                    }
                                    else
                                    {
                                        rtbOut.AppendText("      commID " + commID + " not found\n");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                rtbOut.AppendText("No Serial Ports available\n");
            }
            return rv;
        }

        private void OpenSerialPort(IniSummary iniSummary, int sectionIndex)
        {
            bool success = true;
            if (sectionIndex > -1)
            {
                try
                {
                    serPort1.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    success = false;
                    rtbOut.AppendText("Failed to Open COM Port\n");
                }
                if (success)
                {
                    serPort1.ReadExisting();
                    rtbOut.AppendText(serPort1.PortName + " is Open!\n");
                    iniSectionSelected = iniSummary.names[sectionIndex];
                    SetupDGV();
                }
            }
        }

        private void Dgv_CellContentClick(object? sender, DataGridViewCellEventArgs? e)
        {
            // Event that fires when buttons in DataGridView are clicked
            var senderGrid = sender as DataGridView;
            if (senderGrid!.Columns[e!.ColumnIndex] is DataGridViewButtonColumn &&
                e.RowIndex >= 0)
            {
                processAction(e.RowIndex);
            }
        }

        private void Dgv_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs? e)
        {
            // Event used to format Row Header in DataGridView
            var grid = sender as DataGridView;
            var rowIdx = (e!.RowIndex + 1).ToString();

            var hdrFormat = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            SolidBrush foreBrush = new SolidBrush(Color.White);
            //SolidBrush backBrush = new SolidBrush(Color.DimGray);
            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid!.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, hdrFormat);
            //e.Graphics.FillRectangles(backBrush, new RectangleF[] { headerBounds });
            e.Graphics.DrawString(rowIdx, this.Font, foreBrush, headerBounds, hdrFormat);
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgv.IsCurrentCellDirty)
            {
                dgv.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            int rowID = e.RowIndex;
            if (dgv.Rows[rowID].Cells[5].Value is Int32)
            {
                dgv.Rows[rowID].Cells[3].Value = dgv.Rows[rowID].Cells[5].Value;
                processAction(rowID);
            }
        }

        private void SetupDGV()
        {
            int i = 0;
            int j = 0;
            int inQty = 0;
            string strItemPrefix = "row_";
            string iniItem = "";
            string strRow = "";
            bool foundNextRow = true;
            int gr3 = -1;
            bool gr5 = false;
            char[] gr5Separators = new char[] { '(', '<', ')' };
            int gr5Min = 0;
            int gr5Max = 100;
            iniItem = strItemPrefix + i.ToString();

            while (foundNextRow)
            {
                strRow = ini.Read(iniItem, iniSectionSelected);
                gr5 = false;
                string[] gridRow = strRow.Substring(0).Split(',');
                for (j = 0; j < 6; j++)
                {
                    gridRow[j] = gridRow[j].Trim();
                }
                if (gridRow[0] == "in") inQty++;
                gr3 = Convert.ToInt32(gridRow[3]);
                if (gridRow[1] == "B")
                {
                    baudRate = gr3;
                }
                if (gridRow[1] == "Z") serialCode = gr3;
                if (gridRow[5] == "1") gr5 = true;
                DataGridViewTextBoxCell txtCell = new DataGridViewTextBoxCell();
                txtCell.Style.BackColor = Color.Black;
                DataGridViewButtonCell btnCell = new DataGridViewButtonCell();
                btnCell.Style.BackColor = Color.Black;
                btnCell.Style.SelectionBackColor = Color.Black;
                dgv.Rows.Add(gridRow[0], gridRow[1], gridRow[2], gr3, gridRow[4], btnCell);
                dgv.Rows[i].Cells[5].Value = gridRow[2];
                if (gr5 == false)
                {
                    dgv.Rows[i].Cells[5] = txtCell;
                }
                //CalendarCell calCell = new CalendarCell();
                //calCell.Style.BackColor = Color.Blue;
                //if (gridRow[1] == "C")
                //{
                //    dgv.Rows[i].Cells[5] = calCell;
                //}
                if (gridRow[0] == "outSlider")
                {
                    string[] gr5Subs = gridRow[5].Split(gr5Separators, StringSplitOptions.RemoveEmptyEntries);
                    gr5Min = Convert.ToInt32(gr5Subs[0].Trim());
                    gr5Max = Convert.ToInt32(gr5Subs[1].Trim());
                    TrackBarCell tbCell = new TrackBarCell(gr5Min, gr5Max);
                    tbCell.Style.BackColor = Color.FromArgb(255, 0, 0, 150);
                    tbCell.Style.ForeColor = Color.Yellow;
                    tbCell.Value = gr3;
                    dgv.Rows[i].Cells[5] = tbCell;
                }
                i++;
                iniItem = strItemPrefix + i.ToString();
                if (ini.KeyExists(iniItem, iniSectionSelected) == false) foundNextRow = false;
            }
            grSize = i;

            // Populate array of symbols sent by MCU back to this program along with numeric data
            inSyms = new string[inQty];
            j = 0;
            for (i = 0; i < grSize; i++)
            {
                if ((string)dgv.Rows[i].Cells[0].Value == "in")
                {
                    inSyms[j] = (string)dgv.Rows[i].Cells[1].Value;
                    j++;
                }
            }
        }

        private void pictureBox1_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            // Create a local version of the graphics object for the PictureBox.
            Graphics g = e.Graphics;

            //g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

            //g.DrawString("Diagonal Line", fnt, System.Drawing.Brushes.White, 10.0F, 10.0F);

            //g.DrawLine(System.Drawing.Pens.White, 10.0F, 10.0F, 100.0F, 100.0F);

            double xc = 195.0f * 0.5f;
            double yc = xc;
            double pi = Math.PI;
            double dtr = pi / 180.0;
            double r1 = 180.0 * 0.5;
            double r2 = 190.0 * 0.5;
            for (int i = 0; i < 200; i++)
            {
                double di = (double)i;
                double ang = dtr * 360.0 * di / 200.0;
                double x1 = xc + r1 * Math.Cos(ang);
                double y1 = xc + r1 * Math.Sin(ang);
                double x2 = yc + r2 * Math.Cos(ang);
                double y2 = yc + r2 * Math.Sin(ang);
                float x1f = (float)x1;
                float y1f = (float)y1;
                float x2f = (float)x2;
                float y2f = (float)y2;
                g.DrawLine(System.Drawing.Pens.White, x1f, y1f, x2f, y2f);
            }
        }

        private void processAction(int rowID)
        {
            string? strAction;
            long actionQty;
            string[] args;
            int i;
            int dgvIdx;
            //string? strSlider;

            //var strSlider = dgv.Rows[rowID].Cells[5].Value;
            strAction = dgv.Rows[rowID].Cells[4].Value.ToString();
            actionQty = strAction!.Length;
            args = new string[actionQty];
            if (actionQty > 0)
            {
                for (i = 0; i < actionQty; i++)
                {
                    dgvIdx = GetDgvRowFromSymbol(strAction[i].ToString());
                    args[i] = dgv.Rows[dgvIdx].Cells[3].Value.ToString() ?? "No Arg";
                    if (args[i] == "-1")
                    {
                        write_To_Serial(strAction[i].ToString());
                    }
                    else
                    {
                        write_To_Serial(strAction[i].ToString() + args[i]);
                    }
                }
            }
        }

        private int GetDgvRowFromSymbol(string symbol)
        {
            int rv = 0;
            for (int i = 0; i < grSize; i++)
            {
                if ((string)(dgv.Rows[i].Cells[1].Value) == symbol)
                {
                    rv = i;
                }
            }
            return rv;
        }

        private void ManualSend_Click(object sender, EventArgs e)
        {
            string spData = manualOut.Text;
            spData = spData.Trim();
            manualOut.Text = "";
            write_To_Serial(spData);
        }

        private void write_To_Serial(string spData)
        {
            serPort1.WriteLine(spData);
            DateTime localDate = DateTime.Now;
            string strTime = localDate.ToString("HH:mm:ss.fff");
            rtbOut.Select(rtbOut.TextLength, 0);
            rtbOut.SelectionColor = Color.Yellow;
            rtbOut.AppendText(strTime + " - ");
            rtbOut.Select(rtbOut.TextLength, 0);
            rtbOut.SelectionColor = Color.LightGreen;
            rtbOut.AppendText(spData + "\n");
        }

        private void serPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serPort1.ReadTimeout = 200;
            string str = "";
            try
            {
                str = serPort1.ReadLine();
                str = str.Trim();
                //spDelegate from: https://forum.MCU.cc/t/reading-serial-data-from-MCU-in-windows-forms-application/898994/6
                this.BeginInvoke((new spDelegate(addStringRtbIn)), str);
            }
            catch (Exception ex)
            {
                if (ex.Message != "The operation has timed out.")
                {
                    MessageBox.Show(ex.Message);
                }
            }
            if ((str.Length > 2) && (str.Substring(1, 1) == ":"))
            {
                this.BeginInvoke((new spDelegate(showResponseInDGV)), str);
            }
        }

        private void addStringRtbIn(string sData)
        {
            int maxLength = 20000;
            int cutLength = 5000;
            DateTime localDate = DateTime.Now;
            string strTime = localDate.ToString("HH:mm:ss.fff");
            rtbIn.Select(rtbIn.TextLength, 0);
            rtbIn.SelectionColor = Color.Yellow;
            rtbIn.AppendText(strTime + " - ");
            rtbIn.Select(rtbIn.TextLength, 0);
            rtbIn.SelectionColor = Color.LightBlue;
            rtbIn.AppendText(sData + "\n");
            if (rtbIn.TextLength > maxLength)
            {
                rtbIn.Select(0, cutLength);
                rtbIn.Cut();
                rtbIn.Select(rtbIn.TextLength, 0);
            }
        }

        private void showResponseInDGV(string respArd)
        {
            int dgvRow;
            string inSymbol = respArd.Substring(0, 1);
            string inValue = respArd.Substring(2, respArd.Length - 2);
            for (int i = 0; i < inSyms.Length; i++)
            {
                if (inSymbol == inSyms[i])
                {
                    dgvRow = GetDgvRowFromSymbol(inSymbol);
                    dgv.Rows[dgvRow].Cells[3].Value = inValue;
                }
            }
        }

        private void DebugWriteStopWatch(string swMsg)
        {
            double elapsedMicros = (double)sw.ElapsedTicks * microsPerTick;
            double elapsedMillis = elapsedMicros * 0.001;
            double elapsedSeconds = elapsedMillis * 0.001;
            Debug.WriteLine(swMsg + " elapsed time = " + elapsedSeconds.ToString("N2") + " sec. (" +
                //sw.ElapsedTicks.ToString() + " ticks,  " + 
                elapsedMillis.ToString("N0") + " millis, " + elapsedMicros.ToString("N0") + " micros)");
        }
    }


    public class TrackBarCell : DataGridViewTextBoxCell
    {
        int iMin;
        int iMax;

        public TrackBarCell() : base()
        {
            // Don't need to access any base members for this control
            //this.Value = 55;
        }
        public TrackBarCell(int min, int max)
        {
            iMin = min;
            iMax = max;
        }

        public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);

            TrackBarEditingControl ctl = DataGridView.EditingControl as TrackBarEditingControl;
            // Use the default row value when Value property is null.
            if (this.Value == null)
            {
                ctl.Value = 0;      //(DateTime)this.DefaultNewRowValue;
            }
            else
            {
                ctl.Value = (int)this.Value;
                ctl.Minimum = this.iMin;
                ctl.Maximum = this.iMax;
            }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing control that CalendarCell uses.
                return typeof(TrackBarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.
                return typeof(int);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return 4;
            }
        }
    }

    class TrackBarEditingControl : TrackBar, IDataGridViewEditingControl
    {
        DataGridView ?dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public TrackBarEditingControl()
        {
            // Don't need these, set from data in settings.ini
            //this.Minimum = 0;
            //this.Maximum = 255;
        }

        // Implements the IDataGridViewEditingControl.EditingControlFormattedValue property.
        public object EditingControlFormattedValue
        {
            get
            {
                //Debug.WriteLine(this);
                return this.Value.ToString();
            }
            set
            {
                if (value is Int32)
                {
                    try
                    {
                        this.Value = (int)value;
                    }
                    catch
                    {
                        this.Value = 0;
                    }
                }
            }
        }

        // Implements the IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.ForeColor = dataGridViewCellStyle.ForeColor;
            this.BackColor = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }
            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey method.
        public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit method.
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl.RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlDataGridView property.
        public DataGridView? EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }
            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(eventargs);
        }
    }

    /*
    // Original code from MS article: "How to: Host Controls in Windows Forms DataGridView Cells"
    // From: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-host-controls-in-windows-forms-datagridview-cells?view=netframeworkdesktop-4.8
    // Modified above for TrackBar (Slider) control
    public class CalendarCell : DataGridViewTextBoxCell
    {

        public CalendarCell()
            : base()
        {
            // Use the short date format.
            this.Style.Format = "d";
        }

        public override void InitializeEditingControl(int rowIndex, object
            initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
        {
            // Set the value of the editing control to the current cell value.
            base.InitializeEditingControl(rowIndex, initialFormattedValue,
                dataGridViewCellStyle);
            CalendarEditingControl ctl =
                DataGridView.EditingControl as CalendarEditingControl;
            // Use the default row value when Value property is null.
            if (this.Value == null)
            {
                ctl.Value = (DateTime)this.DefaultNewRowValue;
            }
            else
            {
                ctl.Value = (DateTime)this.Value;
            }
        }

        public override Type EditType
        {
            get
            {
                // Return the type of the editing control that CalendarCell uses.
                return typeof(CalendarEditingControl);
            }
        }

        public override Type ValueType
        {
            get
            {
                // Return the type of the value that CalendarCell contains.

                return typeof(DateTime);
            }
        }

        public override object DefaultNewRowValue
        {
            get
            {
                // Use the current date and time as the default value.
                return DateTime.Now;
            }
        }
    }

    class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
    {
        DataGridView dataGridView;
        private bool valueChanged = false;
        int rowIndex;

        public CalendarEditingControl()
        {
            this.Format = DateTimePickerFormat.Short;
        }

        // Implements the IDataGridViewEditingControl.EditingControlFormattedValue
        // property.
        public object EditingControlFormattedValue
        {
            get
            {
                return this.Value.ToShortDateString();
            }
            set
            {
                if (value is String)
                {
                    try
                    {
                        // This will throw an exception of the string is
                        // null, empty, or not in the format of a date.
                        this.Value = DateTime.Parse((String)value);
                    }
                    catch
                    {
                        // In the case of an exception, just use the
                        // default value so we're not left with a null
                        // value.
                        this.Value = DateTime.Now;
                    }
                }
            }
        }

        // Implements the IDataGridViewEditingControl.GetEditingControlFormattedValue method.
        public object GetEditingControlFormattedValue(
            DataGridViewDataErrorContexts context)
        {
            return EditingControlFormattedValue;
        }

        // Implements the IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
        public void ApplyCellStyleToEditingControl(
            DataGridViewCellStyle dataGridViewCellStyle)
        {
            this.Font = dataGridViewCellStyle.Font;
            this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
            this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
        }

        // Implements the IDataGridViewEditingControl.EditingControlRowIndex property.
        public int EditingControlRowIndex
        {
            get
            {
                return rowIndex;
            }
            set
            {
                rowIndex = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey method.
        public bool EditingControlWantsInputKey(
            Keys key, bool dataGridViewWantsInputKey)
        {
            // Let the DateTimePicker handle the keys listed.
            switch (key & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.Right:
                case Keys.Home:
                case Keys.End:
                case Keys.PageDown:
                case Keys.PageUp:
                    return true;
                default:
                    return !dataGridViewWantsInputKey;
            }
        }

        // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit method.
        public void PrepareEditingControlForEdit(bool selectAll)
        {
            // No preparation needs to be done.
        }

        // Implements the IDataGridViewEditingControl.RepositionEditingControlOnValueChange property.
        public bool RepositionEditingControlOnValueChange
        {
            get
            {
                return false;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlDataGridView property.
        public DataGridView EditingControlDataGridView
        {
            get
            {
                return dataGridView;
            }
            set
            {
                dataGridView = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingControlValueChanged property.
        public bool EditingControlValueChanged
        {
            get
            {
                return valueChanged;
            }
            set
            {
                valueChanged = value;
            }
        }

        // Implements the IDataGridViewEditingControl.EditingPanelCursor property.
        public Cursor EditingPanelCursor
        {
            get
            {
                return base.Cursor;
            }
        }

        protected override void OnValueChanged(EventArgs eventargs)
        {
            // Notify the DataGridView that the contents of the cell have changed.
            valueChanged = true;
            this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
            base.OnValueChanged(eventargs);
        }
    }
    */

}