using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using Microsoft.UI.Windowing;

namespace App3.Services
{
    public class SystemTrayManager : IDisposable
    {
        private readonly MainWindow _mainWindow;
        private bool _disposed = false;
        private readonly IntPtr _hwnd;
        private const int WM_USER = 0x0400;
        private const int WM_TRAYICON = WM_USER + 1;
        private const uint NIM_ADD = 0x00000000;
        private const uint NIM_MODIFY = 0x00000001;
        private const uint NIM_DELETE = 0x00000002;
        private const uint NIF_MESSAGE = 0x00000001;
        private const uint NIF_ICON = 0x00000002;
        private const uint NIF_TIP = 0x00000004;
        private const uint NIF_INFO = 0x00000010;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;

        public event Action? RestoreRequested;
        public event Action? ExitRequested;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NOTIFYICONDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uID;
            public uint uFlags;
            public uint uCallbackMessage;
            public IntPtr hIcon;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szTip;
            public uint dwState;
            public uint dwStateMask;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string szInfo;
            public uint uVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szInfoTitle;
            public uint dwInfoFlags;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        private const uint MF_STRING = 0x00000000;
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_RETURNCMD = 0x0100;

        private const int IDM_RESTORE = 1001;
        private const int IDM_EXIT = 1002;

        private NOTIFYICONDATA _notifyIconData;
        private IntPtr _icon;

        public SystemTrayManager(MainWindow mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
            _hwnd = WindowNative.GetWindowHandle(mainWindow);
            
            InitializeSystemTray();
        }

        private void InitializeSystemTray()
        {
            try
            {
                // Load application icon
                _icon = LoadApplicationIcon();

                // Initialize NOTIFYICONDATA structure
                _notifyIconData = new NOTIFYICONDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONDATA)),
                    hWnd = _hwnd,
                    uID = 1,
                    uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                    uCallbackMessage = WM_TRAYICON,
                    hIcon = _icon,
                    szTip = "Browser - Running in background"
                };

                // Hook into window procedure to handle tray icon messages
                HookWindowProc();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize system tray: {ex.Message}");
            }
        }

        private void HookWindowProc()
        {
            try
            {
                // For WinUI 3, we need to use a different approach
                // This is a simplified version - in a full implementation you'd use SetWindowSubclass
                System.Diagnostics.Debug.WriteLine("System tray initialized (simplified implementation)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to hook window procedure: {ex.Message}");
            }
        }

        private IntPtr LoadApplicationIcon()
        {
            try
            {
                // Try to load the application icon
                var hInstance = GetModuleHandle(null);
                var icon = LoadIcon(hInstance, new IntPtr(32512)); // IDI_APPLICATION
                return icon != IntPtr.Zero ? icon : LoadIcon(IntPtr.Zero, new IntPtr(32512));
            }
            catch
            {
                return LoadIcon(IntPtr.Zero, new IntPtr(32512)); // Default application icon
            }
        }

        public void ShowInTray()
        {
            try
            {
                Shell_NotifyIcon(NIM_ADD, ref _notifyIconData);
                ShowBalloonTip("Browser", "Application continues running in the background");
                System.Diagnostics.Debug.WriteLine("App added to system tray");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show in tray: {ex.Message}");
            }
        }

        public void HideFromTray()
        {
            try
            {
                Shell_NotifyIcon(NIM_DELETE, ref _notifyIconData);
                System.Diagnostics.Debug.WriteLine("App removed from system tray");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to hide from tray: {ex.Message}");
            }
        }

        private void ShowBalloonTip(string title, string text)
        {
            try
            {
                var data = _notifyIconData;
                data.uFlags = NIF_INFO;
                data.szInfoTitle = title;
                data.szInfo = text;
                data.dwInfoFlags = 1; // NIIF_INFO
                
                Shell_NotifyIcon(NIM_MODIFY, ref data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show balloon tip: {ex.Message}");
            }
        }

        private void ShowContextMenu()
        {
            try
            {
                var menu = CreatePopupMenu();
                AppendMenu(menu, MF_STRING, new IntPtr(IDM_RESTORE), "Open Browser");
                AppendMenu(menu, MF_SEPARATOR, IntPtr.Zero, "");
                AppendMenu(menu, MF_STRING, new IntPtr(IDM_EXIT), "Exit");

                GetCursorPos(out POINT point);
                SetForegroundWindow(_hwnd);

                // TrackPopupMenu returns a value of type IntPtr, but in your P/Invoke signature it is declared as bool.
                // The correct signature for TrackPopupMenu should be:
                // [DllImport("user32.dll")]
                // private static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);
                // For now, let's cast the result to int and check if it's not zero.

                var command = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_RETURNCMD, point.x, point.y, 0, _hwnd, IntPtr.Zero);
                if (command != 0)
                {
                    HandleMenuCommand(command);
                }

                DestroyMenu(menu);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show context menu: {ex.Message}");
            }
        }

        private void HandleMenuCommand(int command)
        {
            switch (command)
            {
                case IDM_RESTORE:
                    RestoreRequested?.Invoke();
                    break;
                case IDM_EXIT:
                    ExitRequested?.Invoke();
                    break;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    HideFromTray();
                    if (_icon != IntPtr.Zero)
                    {
                        DestroyIcon(_icon);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing system tray: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}