using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ui;
using Properties = NovoServico.Properties;

namespace outros
{
    /// <summary>
    /// Helper class that saves the window's state to a User Settings string
    /// Setting for each window (based on it's className) must be created in the Project's Properties window
    /// Usage: `new WindowStateSaveHelper(MyWindow);`
    /// </summary>
    public class WindowStateSaveHelper
    {
        private Window _window;
        private string _windowName;

        public WindowStateSaveHelper(Window window)
        {
            _window = window;
            _windowName = _window.GetType().Name;
            _window.Loaded += LoadedEventHandler;
            _window.Closed += ClosedEventHandler;
        }

        private void LoadedEventHandler(object sender, RoutedEventArgs e)
        {
            string s = (string)Properties.Settings.Default[_windowName];
            WState w = StringToState(s);
            if (w == null) return;
            SizeToFit(w);
            MoveIntoView(w);
            _window.Left = w.Left;
            _window.Top = w.Top;
            _window.Width = w.Width;
            _window.Height = w.Height;

            Async.runOnUI(500, () => {
                _window.WindowState = w.WindowState;
            }); 
        }

        private void ClosedEventHandler(object sender, EventArgs e)
        {
            if (_window.WindowState == WindowState.Minimized) return;
            WState w = new WState();
            w.Left = _window.Left;
            w.Top = _window.Top;
            w.Width = _window.Width;
            w.Height = _window.Height;
            w.WindowState = _window.WindowState;
            Properties.Settings.Default[_windowName] = StateToString(w);
            Properties.Settings.Default.Save();
        }

        private string StateToString(WState state)
        {
            var s = new object[]
            {
                state.Left.ToString(),
                state.Top.ToString(),
                state.Width.ToString(),
                state.Height.ToString(),
                state.WindowState.ToString()
            };
            return String.Join(",", s);
        }

        private WState StringToState(string state)
        {
            var s = state.Split(new char[] { ',' }, 5, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                WState w = new WState()
                {
                    Left = double.Parse(s[0]),
                    Top = double.Parse(s[1]),
                    Width = double.Parse(s[2]),
                    Height = double.Parse(s[3]),
                    WindowState = (WindowState)Enum.Parse(typeof(WindowState), s[4])
                };
                return w;
            }
            catch { return null; }
        }

        // Based on http://www.codeproject.com/Articles/50761/Save-and-Restore-WPF-Window-Size-Position-and-or-S
        private void SizeToFit(WState state)
        {
            if (state.Height > SystemParameters.VirtualScreenHeight)
            {
                state.Height = SystemParameters.VirtualScreenHeight;
            }

            if (state.Width > SystemParameters.VirtualScreenWidth)
            {
                state.Width = SystemParameters.VirtualScreenWidth;
            }
        }

        private void MoveIntoView(WState state)
        {
            if (state.Top + state.Height / 2 > SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop)
            {
                state.Top = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - state.Height;
            }

            if (state.Left + state.Width / 2 > SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft)
            {
                state.Left = SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft - state.Width;
            }

            if (state.Top < SystemParameters.VirtualScreenTop)
            {
                state.Top = SystemParameters.VirtualScreenTop;
            }

            if (state.Left < SystemParameters.VirtualScreenLeft)
            {
                state.Left = SystemParameters.VirtualScreenLeft;
            }
        }
    }

    class WState
    {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public WindowState WindowState { get; set; }
    }
}