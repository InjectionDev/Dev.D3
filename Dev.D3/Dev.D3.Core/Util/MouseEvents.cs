using Enigma.D3;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dev.D3.Core.Util
{
    public class MouseEvents
    {

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        public static void LeftClick(int X, int Y)
        {
            BringToFrontIfNeeded();

            Cursor.Position = new Point(X, Y);
            mouse_event((int)0x0002 | 0x0004, X, Y, 0, 0);
        }

        public static void RightClick(int X, int Y)
        {
            BringToFrontIfNeeded();

            Cursor.Position = new Point(X, Y);
            mouse_event((int)0x0008 | 0x0010, X, Y, 0, 0);
        }

        private static void BringToFrontIfNeeded()
        {
            if (GetForegroundWindow() != Engine.Current.Process.MainWindowHandle)
            {
                ShowWindow(Engine.Current.Process.MainWindowHandle, 1);
                SetForegroundWindow(Engine.Current.Process.MainWindowHandle);

                BringWindowToFront();
                SetWindowPos(Engine.Current.Process.MainWindowHandle, 0, 0, 0, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
                System.Threading.Thread.Sleep(200);
            }
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);
        const short SWP_NOMOVE = 0X2;
        const short SWP_NOSIZE = 1;
        const short SWP_NOZORDER = 0X4;
        const int SWP_SHOWWINDOW = 0x0040;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);
        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
            Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
            Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
            Restore = 9, ShowDefault = 10, ForceMinimized = 11
        };

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SetActiveWindow(int hwnd);
        public static void BringWindowToFront()
        {
            //get the process
            System.Diagnostics.Process bProcess = System.Diagnostics.Process.GetProcessesByName("Diablo III").FirstOrDefault<System.Diagnostics.Process>();
            //check if the process is nothing or not.
            if (bProcess != null)
            {
                //get the (int) hWnd of the process
                int hwnd = (int)bProcess.MainWindowHandle;
                //check if its nothing
                if (hwnd != 0)
                {
                    //if the handle is other than 0, then set the active window
                    SetActiveWindow(hwnd);
                }
                else
                {
                    //we can assume that it is fully hidden or minimized, so lets show it!
                    ShowWindow(bProcess.Handle, ShowWindowEnum.Restore);
                    SetActiveWindow((int)bProcess.MainWindowHandle);
                }
            }
            else
            {
                //tthe process is nothing, so start it
                System.Diagnostics.Process.Start(@"C:\Program Files\B\B.exe");
            }
        }



    }
}
