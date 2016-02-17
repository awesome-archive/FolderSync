using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace FolderSync
{
    static class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("Kernel32.dll", EntryPoint = "FreeConsole")]
        private static extern bool FreeConsole();
        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);
        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleTitle(string strMessage);
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {

            if (args.Length > 0 && string.Compare(args[0], "--cmd", true) == 0)
            {
                // using cmd style
                AllocConsole();
                IntPtr windowHandle = FindWindow(null, Process.GetCurrentProcess().MainModule.FileName);
                SetConsoleTitle("FolderSync v2 命令行");
                FreeConsole();
            }

            else
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new WinForm());
            }

        }
    }
}
