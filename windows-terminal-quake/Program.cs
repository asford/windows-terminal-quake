using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using WindowsTerminalQuake.Native;
using WindowsTerminalQuake.UI;
using System.IO;
using System.Text;

namespace WindowsTerminalQuake
{
    public class Program
    {
        private static Toggler _toggler;
        private static TrayIcon _trayIcon;
        private static DebounceDispatcher _winEventHookDispatcher;
        private static IntPtr _mousehook = default(IntPtr);
        private static Config _config;
        private static bool _closed = false;

        public static void Main(string[] args)
        {
            _config = new Config();
            _config.Reload();

            if (_config.Width < 200 || _config.Height < 40)
            {
                _config.Width = 0;
                _config.Height = 0;
            }

            // don't allow more than one instance to be running simultaneously
            var existingProcesses = Process.GetProcessesByName("windows-terminal-quake");
            if (existingProcesses.Count() > 1)
            {
                try
                {
                    var process = GetWindowsTerminalProcess();
                    if (process != null)
                    {
                        User32.SetForegroundWindow(process.MainWindowHandle);
                    }
                } catch (Exception) { }
                return;
            }

            Application.ApplicationExit += (sender, e) =>
            {
                Close();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                WriteToLog(e.ToString());
                
            };

            _trayIcon = new TrayIcon((s, a) => Close());

            try
            {

                var process = GetWindowsTerminalProcess();
                if (process == null)
                {
                    process = CreateWindowsTerminalProcess();
                }
                _toggler = new Toggler(process, _config);
                _trayIcon.Notify(ToolTipIcon.Info, $"Windows Terminal Quake is running, press Alt+~ to toggle.");


                // capture mouse click event - can't seem to make this work
                // we want to be able to handle mouse-up event to make resizing seamless
                //
                //_mousehook = User32.SetWindowsHookEx(
                //  User32.HookType.WH_MOUSE,
                //  new User32.CBTProc(WinMouseEvent),
                //  IntPtr.Zero,
                //  User32.GetCurrentThreadId()
                // );
                
                // capture window resize event
                _winEventHookDispatcher = new DebounceDispatcher();
                User32.SetWinEventHook(
                    User32.EVENT_OBJECT_LOCATIONCHANGE,
                    User32.EVENT_OBJECT_LOCATIONCHANGE,
                    //0x8000,
                    //0x8FFF,
                    System.IntPtr.Zero,
                    WinEventProc,
                    (uint)process.Id,
                    (uint)0,
                    User32.WINEVENT_OUTOFCONTEXT
                 );

                User32.MSG msg;
                while (!_closed)
                {
                    int result = User32.GetMessage(out msg, IntPtr.Zero, 0, 0);
                    if (result == 0) break;
                    if (result == -1) throw new Exception();
                    User32.TranslateMessage(msg);
                    User32.DispatchMessage(msg);
                }
            }
            catch (Exception ex)
            {
                _trayIcon.Notify(ToolTipIcon.Error, $"Cannot start: '{ex.Message}'.");
                Close();
            }
            
        }

        private static void WriteToLog(string text, ConsoleColor? color = null)
        {
            var oldColor = Console.ForegroundColor;

            try
            {
                Console.ForegroundColor = color ?? oldColor;
                using (var file = File.Open("log.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    var bytes = Encoding.UTF8.GetBytes(text);
                    file.Write(bytes, 0, bytes.Length);
                }
            }
            finally
            {
                Console.Out.WriteLine(text);
                Console.ForegroundColor = oldColor;
            }
        }

        private static IntPtr WinMouseEvent(int code, IntPtr wParam, IntPtr lParam)
        {

            return User32.CallNextHookEx(IntPtr.Zero, code, wParam, lParam);
        }

        private static void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (hwnd != IntPtr.Zero)
            {
                _winEventHookDispatcher.Debounce(500, e =>
                {
                    // Debug.WriteLine(eventType);
                    if (_toggler == null) {
                        return;
                    }
                    _toggler.ResizeAndPosition();
                });
            }
        }
        private static Process GetWindowsTerminalProcess()
        {
            var processes = Process.GetProcessesByName("WindowsTerminal").Where(e => e.MainWindowHandle != default(IntPtr)).ToArray();
            Process process = null;
            int i = 0;
            while (i < processes.Length)
            {
                try
                {
                    process = processes[i];
                    bindProcessEvents(process);
                    break;
                }
                catch (Exception)
                {
                    i++;
                }
            }
            return process;
        }
        private static Process CreateWindowsTerminalProcess()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "wt",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas"
            };
            Process process;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Exception)
            {
                startInfo.Verb = "";
                process = Process.Start(startInfo);
            }
            while (process.MainWindowTitle == "" || process.MainWindowTitle == "DesktopWindowXamlSource")
            {
                Thread.Sleep(10);
                process.Refresh();
            }
            bindProcessEvents(process);
            return process;
        }

        private static void bindProcessEvents(Process process)
        {
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) =>
            {
                Close();
            };
        }
        private static void Close()
        {
            _config.Save();

            if (_mousehook != IntPtr.Zero)
            {
                User32.UnhookWindowsHookEx(_mousehook);
            }

            _toggler?.Dispose();
            _toggler = null;

            _trayIcon?.Dispose();
            _trayIcon = null;

            _closed = true;
        }

    }
}
