// From: https://stackoverflow.com/questions/217902/reading-writing-an-ini-file
//   Some modifications for handle possible nulls, added GetSections()

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MCU_Serial_Comm
{
    class IniFile   // revision 11
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name ?? "Null Application Name";

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern long GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);

        public IniFile(string? IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE + ".ini").FullName;
        }

        public string Read(string Key, string? Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public void Write(string Key, string Value, string? Section = null)
        {
            WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
        }

        public void DeleteKey(string Key, string? Section = null)
        {
            Write(Key, "", Section ?? EXE);
        }

        public void DeleteSection(string? Section = null)
        {
            Write("", "", Section ?? EXE);
        }

        public bool KeyExists(string Key, string? Section = null)
        {
            return Read(Key, Section).Length > 0;
        }

        public string[] GetSections()
        {
            // Added with help from:
            //   http://pinvoke.net/default.aspx/kernel32/GetPrivateProfileSectionNames.html
            
            string[] rv = { "NoSections0", "NoSections1" };
            uint MAX_BUFFER = 32767;
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MAX_BUFFER);
            long bytesReturned = GetPrivateProfileSectionNames(pReturnedString, MAX_BUFFER, Path);
            
            if (bytesReturned == 0) return rv;

            string ret = Marshal.PtrToStringAnsi(pReturnedString, (int)(bytesReturned * 2)).ToString();
            Marshal.FreeCoTaskMem(pReturnedString);

            // convert ASCII string to Unicode string
            byte[] bytes = Encoding.ASCII.GetBytes(ret);
            string local = Encoding.Unicode.GetString(bytes);

            //use of Substring below removes terminating null for split
            rv = local.Substring(0, local.Length - 1).Split('\0');
            return rv;
        }

        public string GetPath()
        {
            return Path;
        }
    }
}
