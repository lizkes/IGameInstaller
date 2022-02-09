using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace IGameInstaller.Extension
{
    public static class WindowExtensions
    {
        public static void MoveToCenter(this Window window, double width, double height)
        {
            window = window ?? throw new ArgumentNullException(nameof(window));

            var helper = new WindowInteropHelper(window);
            var screen = Screen.FromHandle(helper.Handle);
            var area = screen.WorkingArea;

            var source = PresentationSource.FromVisual(window);
            var dpi = source?.CompositionTarget?.TransformFromDevice.M11 ?? 1.0;

            window.Left = dpi * area.Left + (dpi * area.Width - width) / 2;
            window.Top = dpi * area.Top + (dpi * area.Height - height) / 2;
        }
    }
}
