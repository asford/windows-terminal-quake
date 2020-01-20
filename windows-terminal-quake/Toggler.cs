using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsTerminalQuake.Native;

namespace WindowsTerminalQuake
{

    public class Toggler : IDisposable
    {
        private static readonly int stepCount = 10;

        private Process process;
        private Config config;

        public Toggler(Process _process, Config _config)
        {
            process = _process;
            config = _config;

            // Hide from taskbar
            User32.SetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE, (User32.GetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE) | User32.WS_EX_TOOLWINDOW) & ~User32.WS_EX_APPWINDOW);

            if (config.Maximize)
            {
                User32.ShowWindow(process.MainWindowHandle, NCmdShow.MAXIMIZE);
            }

            User32.Rect rect = default;
            User32.GetWindowRect(process.MainWindowHandle, ref rect);
            var terminalWindow = new TerminalWindow(rect, GetScreenWithCursor().Bounds);
            if (config.Center)
            {
                terminalWindow.CenterHorizontally();
            }
            terminalWindow.EnsureVisible();
            if (config.Width == 0 || config.Height == 0)
            {
                SaveTerminalState(terminalWindow); 
            } else {
                RestoreTerminalState(terminalWindow);
            }

            User32.MoveWindow(_process.MainWindowHandle, terminalWindow.ScreenX, terminalWindow.ScreenY, terminalWindow.Width, terminalWindow.Height, true);
            User32.ShowWindow(_process.MainWindowHandle, NCmdShow.SHOW);
            User32.SetForegroundWindow(_process.MainWindowHandle);

            HotKeyManager.RegisterHotKey(Keys.Oemtilde, KeyModifiers.Alt);
            HotKeyManager.HotKeyPressed += handleHotKeyPressed;
        }

        private void handleHotKeyPressed(object e, HotKeyEventArgs a)
        {
            User32.Rect rect = default;
            User32.GetWindowRect(process.MainWindowHandle, ref rect);
            var terminalWindow = new TerminalWindow(rect, GetScreenWithCursor().Bounds);

            var isVisible = terminalWindow.IsVisible();
            var isDocked = terminalWindow.IsDocked();

            var foregroundWindow = Native.User32.GetForegroundWindow();
            var isForeground = foregroundWindow == process.MainWindowHandle;

            if (isVisible && isDocked && isForeground)
            {
                HideTerminalWithEffects(terminalWindow);
            }
            else if (isVisible && isDocked)
            {
                User32.SetForegroundWindow(process.MainWindowHandle);
            }
            else
            {
                ShowTerminalWithEffects(terminalWindow);
            }
        }

        private void HideTerminalWithEffects(TerminalWindow terminalWindow)
        {
            SaveTerminalState(terminalWindow);

            // if the window isn't docked then 
            if (!terminalWindow.IsDocked())
            {
                HideProcessWindow();
                return;
            }

            Console.WriteLine("Close");

            User32.ShowWindow(process.MainWindowHandle, NCmdShow.SHOW);
            User32.SetForegroundWindow(process.MainWindowHandle);

            var stepSize = (double)config.Height / (double)stepCount;
            for (int i = 1; i <= stepCount; i++)
            {

                User32.MoveWindow(
                    process.MainWindowHandle, 
                    terminalWindow.ScreenX, 
                    terminalWindow.ScreenY - (int)Math.Round(stepSize * i), 
                    terminalWindow.Width, 
                    terminalWindow.Height, 
                    true
                );

                Task.Delay(1).GetAwaiter().GetResult();
            }

            HideProcessWindow();
        }

        private void ShowTerminalWithEffects(TerminalWindow terminalWindow)
        {
            Console.WriteLine("Open");
            terminalWindow.Left = config.OffsetLeft;
            terminalWindow.Width = config.Width;
            terminalWindow.Top = -config.Height;
            terminalWindow.Height = config.Height;
            terminalWindow.EnsureWillFitOnScreen();

            if (config.Center)
            {
                terminalWindow.CenterHorizontally();
            }

            User32.MoveWindow(
                process.MainWindowHandle,
                terminalWindow.ScreenX,
                terminalWindow.ScreenY - terminalWindow.Height,
                terminalWindow.Width,
                terminalWindow.Height,
                true
            );

            // todo - this flashes the window at it's prior position if it was minimized.
            User32.ShowWindow(process.MainWindowHandle, NCmdShow.RESTORE);
            
            User32.SetForegroundWindow(process.MainWindowHandle);

            var stepSize = (double)config.Height / (double)stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                User32.MoveWindow(
                    process.MainWindowHandle,
                    terminalWindow.ScreenX,
                    terminalWindow.ScreenY + (int)Math.Round(stepSize * i),
                    terminalWindow.Width,
                    terminalWindow.Height,
                    true
                );

                Task.Delay(1).GetAwaiter().GetResult();
            }
            User32.ShowWindow(process.MainWindowHandle, NCmdShow.SHOW);
        }

        private void ShowTerminal(TerminalWindow terminalWindow)
        {
            User32.MoveWindow(
                  process.MainWindowHandle,
                  terminalWindow.ScreenX,
                  terminalWindow.ScreenY,
                  terminalWindow.Width,
                  terminalWindow.Height,
                  true
              );
        }
        private void HideProcessWindow() { 
            // Minimize, so the last window gets focus
            User32.ShowWindow(process.MainWindowHandle, NCmdShow.MINIMIZE);

            // Hide, so the terminal windows doesn't linger on the desktop
            User32.ShowWindow(process.MainWindowHandle, NCmdShow.HIDE);
        }

        private void SaveTerminalState(TerminalWindow terminalWindow)
        {
            var placement = new User32.WINDOWPLACEMENT();
            if (User32.GetWindowPlacement(process.MainWindowHandle, ref placement))
            {
                if (placement.showCmd == User32.SW_SHOWMINIMIZED)
                {
                    return;
                }
            }
            config.Width = terminalWindow.Width;
            config.Height = terminalWindow.Height;
            config.OffsetLeft = terminalWindow.Left;
        }

        private void RestoreTerminalState(TerminalWindow terminalWindow)
        {
            terminalWindow.Width = config.Width;
            terminalWindow.Height = config.Height;
            terminalWindow.Left = config.OffsetLeft;
            if (config.Center) {
                terminalWindow.CenterHorizontally();
            }
        }

        public void ResizeAndPosition()
        {
            User32.Rect rect = default;
            User32.GetWindowRect(process.MainWindowHandle, ref rect);

            var terminalWindow = new TerminalWindow(rect, GetScreenWithCursor().Bounds);
            SaveTerminalState(terminalWindow);
            if (config.Center)
            {
                terminalWindow.CenterHorizontally();
            }
            ShowTerminal(terminalWindow);
        }

        private static Screen GetScreenWithCursor()
        {
            return Screen.AllScreens.FirstOrDefault(s => s.Bounds.Contains(Cursor.Position));
        }

        public void Dispose()
        {
            ResetTerminal(process);
        }
        private static void ResetTerminal(Process process)
        {
            User32.Rect rect = default;
            User32.GetWindowRect(process.MainWindowHandle, ref rect);

            var terminalWindow = new TerminalWindow(rect, GetScreenWithCursor().Bounds);

            // Restore taskbar icon
            User32.SetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE, (User32.GetWindowLong(process.MainWindowHandle, User32.GWL_EX_STYLE) | User32.WS_EX_TOOLWINDOW) & User32.WS_EX_APPWINDOW);

            // Reset position
            User32.MoveWindow(process.MainWindowHandle, terminalWindow.ScreenX, terminalWindow.ScreenY, terminalWindow.Width, terminalWindow.Height, true);

            // Restore window
            User32.ShowWindow(process.MainWindowHandle, NCmdShow.SHOW);
        }
    }
}