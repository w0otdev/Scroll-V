using Microsoft.Win32;
using ScrollV.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ScrollV
{
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, IntPtr min, IntPtr max);

        private const string MutexName = "ScrollV_SingleInstance_Mutex";

        private Mutex? _singleInstanceMutex;
        private ScrollVManager? _manager;
        private Timer? _memoryTimer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (e.Args.Contains("--uninstall"))
            {
                StopOtherInstances();
                UninstallApp();
                Environment.Exit(0);
                return;
            }

            string currentProcessName = Process.GetCurrentProcess().ProcessName;
            if (e.Args.Contains("--setup") || currentProcessName.Contains("Setup", StringComparison.OrdinalIgnoreCase))
            {
                InstallApp();
                Environment.Exit(0);
                return;
            }

            StopOtherInstances();

            _singleInstanceMutex = new Mutex(true, MutexName, out _);

            try
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.AboveNormal;
            }
            catch
            {
            }

            _manager = new ScrollVManager();
            _manager.Start();

            bool startHidden = e.Args.Contains("--tray") || e.Args.Contains("--background");
            var mainWindow = new MainWindow(_manager);
            MainWindow = mainWindow;

            if (startHidden)
            {
                mainWindow.HideWindow();
            }
            else
            {
                mainWindow.ShowWindow();
            }

            _memoryTimer = new Timer(_ => OptimizeMemory(false), null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(60));
        }

        public static void OptimizeMemory(bool aggressive = false)
        {
            try
            {
                if (aggressive)
                {
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: true);
                }
                else
                {
                    GC.Collect(0, GCCollectionMode.Optimized, blocking: false);
                }

                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
            }
            catch
            {
            }
        }

        private static void StopOtherInstances()
        {
            int currentPid = Environment.ProcessId;
            foreach (var proc in Process.GetProcessesByName("Scroll-V"))
            {
                using (proc)
                {
                    if (proc.Id == currentPid) continue;
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(2000);
                    }
                    catch
                    {
                    }
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _memoryTimer?.Dispose();
            if (_singleInstanceMutex != null)
            {
                try { _singleInstanceMutex.ReleaseMutex(); } catch { }
                _singleInstanceMutex.Dispose();
            }
            _manager?.Dispose();
            base.OnExit(e);
        }

        public ScrollVManager? GetManager() => _manager;

        private static void InstallApp()
        {
            string sourceExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
            if (string.IsNullOrEmpty(sourceExe) || !File.Exists(sourceExe)) return;

            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Scroll-V");
            string targetExe = Path.Combine(installDir, "Scroll-V.exe");

            StopOtherInstances();

            Directory.CreateDirectory(installDir);
            if (!string.Equals(Path.GetFullPath(sourceExe), Path.GetFullPath(targetExe), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourceExe, targetExe, overwrite: true);
            }

            using (var runKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"))
            {
                runKey?.SetValue("Scroll-V", $"\"{targetExe}\" --tray");
            }

            using (var uninstallKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Scroll-V"))
            {
                uninstallKey?.SetValue("DisplayName", "Scroll-V");
                uninstallKey?.SetValue("DisplayVersion", "1.1");
                uninstallKey?.SetValue("Publisher", "w00tdev");
                uninstallKey?.SetValue("DisplayIcon", targetExe);
                uninstallKey?.SetValue("InstallLocation", installDir);
                uninstallKey?.SetValue("UninstallString", $"\"{targetExe}\" --uninstall");
                uninstallKey?.SetValue("NoModify", 1, RegistryValueKind.DWord);
                uninstallKey?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = targetExe,
                Arguments = "--show",
                UseShellExecute = true
            });
        }

        private static void UninstallApp()
        {
            try
            {
                using var runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                runKey?.DeleteValue("Scroll-V", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Scroll-V", false);
            }
            catch
            {
            }
        }

        public static void SetStartWithWindows(bool enable)
        {
            const string appName = "Scroll-V";
            string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null) return;

                if (enable && !string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(appName, $"\"{exePath}\" --tray");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
            }
            catch
            {
            }
        }

        public static bool IsStartWithWindowsEnabled()
        {
            const string appName = "Scroll-V";
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(appName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
