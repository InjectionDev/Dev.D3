using Enigma.D3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Hook
{
    public static class WindowHook
    {
        [DllImport("user32.dll")]
        private static extern bool SetWindowText(IntPtr hWnd, string pText);

        public static bool SetD3WindowText(string pText)
        {
            if (Engine.Current == null)
                return false;

            return SetWindowText(Engine.Current.Process.MainWindowHandle, pText);

        }

    }
}
