using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace IGameInstaller.Helper
{
    public class WindowHelper
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        public const uint MF_GRAYED = 0x00000001;
        public const uint MF_ENABLED = 0x00000000;
        public const uint SC_CLOSE = 0xF060;

        public static readonly Window MainWindow = Application.Current.MainWindow;
        public static readonly IntPtr hwnd = new WindowInteropHelper(MainWindow).Handle;
        public static readonly IntPtr hMenu = GetSystemMenu(hwnd, false);

        private static void CancelClose(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
        }
        public static void DisableWindowCloseButton()
        {
            if (hMenu != IntPtr.Zero)
            {
                EnableMenuItem(hMenu, SC_CLOSE, MF_GRAYED);
            }
            MainWindow.Closing += CancelClose;
        }
        public static void EnableWindowCloseButton()
        {
            if (hMenu != IntPtr.Zero)
            {
                EnableMenuItem(hMenu, SC_CLOSE, MF_ENABLED);
            }
            MainWindow.Closing -= CancelClose;
        }
    }
}
