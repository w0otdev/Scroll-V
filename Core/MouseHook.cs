using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScrollV.Core
{
    /// <summary>
    /// Low-level mouse hook to intercept scroll events system-wide
    /// </summary>
    public class MouseHook : IDisposable
    {
        #region Win32 API

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;

        private static readonly UIntPtr SMOOTH_SCROLL_SIGNATURE = new UIntPtr(0x53534D53); // "SSMS"

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, System.Text.StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private const int VK_CONTROL = 0x11;

        private static readonly HashSet<string> IDM_WINDOW_CLASSES = new(StringComparer.OrdinalIgnoreCase)
        {
            "IEFrame_idm",
            "IDM_DownloadWindow",
            "IEFrame",
            "IDMGrabbedVideo"
        };

        #endregion

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelMouseProc _proc;
        private bool _isHooked;
        private bool _disposed;

        private static readonly ConcurrentDictionary<uint, (string Name, DateTime CachedAt)> _processCache = new();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(30);

        public event EventHandler<MouseScrollEventArgs>? MouseScroll;

        public bool IsEnabled { get; set; } = true;
        public bool IsPaused { get; set; }

        public static UIntPtr ScrollSignature => SMOOTH_SCROLL_SIGNATURE;

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_isHooked) return;

            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule!)
            {
                IntPtr hMod = GetModuleHandle(curModule.ModuleName);
                if (hMod == IntPtr.Zero)
                {
                    hMod = GetModuleHandle(null!);
                }
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, hMod, 0);
            }

            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to install mouse hook");
            }

            _isHooked = true;
            IsPaused = false;
        }

        public void Stop()
        {
            if (!_isHooked) return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _isHooked = false;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (IsPaused || !IsEnabled || nCode < 0)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            int msg = wParam.ToInt32();
            if (msg != WM_MOUSEWHEEL && msg != WM_MOUSEHWHEEL)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            if (hookStruct.dwExtraInfo == (IntPtr)SMOOTH_SCROLL_SIGNATURE)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            if (IsIDMWindowActive())
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            short delta = (short)(hookStruct.mouseData >> 16);
            bool isHorizontal = msg == WM_MOUSEHWHEEL;

            IntPtr windowUnderCursor = WindowFromPoint(hookStruct.pt);
            GetWindowThreadProcessId(windowUnderCursor, out uint processId);
            string processName = GetProcessNameCached(processId);

            var args = new MouseScrollEventArgs
            {
                Delta = delta,
                IsHorizontal = isHorizontal,
                X = hookStruct.pt.x,
                Y = hookStruct.pt.y,
                WindowHandle = windowUnderCursor,
                ProcessId = processId,
                ProcessName = processName
            };

            try
            {
                MouseScroll?.Invoke(this, args);
            }
            catch
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            if (args.Handled)
            {
                return (IntPtr)1;
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static string GetProcessNameCached(uint processId)
        {
            if (processId == 0) return "Idle";

            if (_processCache.TryGetValue(processId, out var cached) &&
                DateTime.UtcNow - cached.CachedAt < CacheExpiry)
            {
                return cached.Name;
            }

            string name = GetProcessNameWin32(processId);
            _processCache[processId] = (name, DateTime.UtcNow);

            if (_processCache.Count > 100)
            {
                CleanCache();
            }

            return name;
        }

        private static string GetProcessNameWin32(uint processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero) return "Unknown";

            try
            {
                var sb = new System.Text.StringBuilder(1024);
                int size = sb.Capacity;
                if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                {
                    return System.IO.Path.GetFileNameWithoutExtension(sb.ToString());
                }
            }
            finally
            {
                CloseHandle(hProcess);
            }

            return "Unknown";
        }

        private static void CleanCache()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _processCache)
            {
                if (now - kvp.Value.CachedAt > CacheExpiry)
                {
                    _processCache.TryRemove(kvp.Key, out _);
                }
            }
        }

        private static bool IsIDMWindowActive()
        {
            try
            {
                IntPtr foreground = GetForegroundWindow();
                if (foreground == IntPtr.Zero) return false;

                var className = new System.Text.StringBuilder(256);
                int result = GetClassName(foreground, className, 256);
                if (result > 0)
                {
                    string classStr = className.ToString();
                    return IDM_WINDOW_CLASSES.Contains(classStr) ||
                           classStr.StartsWith("IDM", StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
            }

            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            Stop();
            _disposed = true;
        }

        ~MouseHook()
        {
            Dispose(false);
        }
    }

    public class MouseScrollEventArgs : EventArgs
    {
        public short Delta { get; set; }
        public bool IsHorizontal { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public IntPtr WindowHandle { get; set; }
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool Handled { get; set; }
    }
}
