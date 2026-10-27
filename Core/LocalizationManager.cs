using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ScrollV.Core
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private string _currentLanguage = "vi";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged(nameof(CurrentLanguage));
                    LanguageChanged?.Invoke(this, value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? LanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            ["vi"] = new Dictionary<string, string>
            {
                ["Settings"] = "HỆ THỐNG CƠ BẢN",
                ["Tuning"] = "THÔNG SỐ VẬT LÝ",
                ["Exceptions"] = "BỘ LỌC TIẾN TRÌNH",

                ["StatusActive"] = "TRỰC TUYẾN",
                ["StatusPaused"] = "NGẮT KẾT NỐI",

                ["EnableSmooth"] = "Cuộn mượt toàn hệ thống",
                ["EnableSmoothDesc"] = "Làm mượt con lăn chuột trên mọi cửa sổ",
                ["StartWithWindows"] = "Khởi động cùng hệ điều hành",
                ["StartWithWindowsDesc"] = "Chạy ngầm trong khay hệ thống khi bật máy",
                ["ScrollAcceleration"] = "Gia tốc theo lực lăn",
                ["ScrollAccelerationDesc"] = "Tăng quãng đường khi lăn chuột liên tục",

                ["Smoothness"] = "Độ mượt phản hồi",
                ["ScrollSpeed"] = "Tốc độ bước cuộn",
                ["Momentum"] = "Quán tính giữ đà",
                ["Glide"] = "Độ trôi cuối hành trình",
                ["Animation"] = "Đường cong nội suy",

                ["VerySmooth"] = "Rất mượt",
                ["Smooth"] = "Mượt",
                ["Medium"] = "Cân bằng",
                ["Fast"] = "Nhanh",
                ["Instant"] = "Tức thì",
                ["Low"] = "Thấp",
                ["High"] = "Cao",
                ["VeryHigh"] = "Rất cao",
                ["Subtle"] = "Nhẹ",
                ["Strong"] = "Mạnh",
                ["VeryStrong"] = "Rất mạnh",
                ["Maximum"] = "Tối đa",

                ["ExcludedApps"] = "Tiến trình ngoại lệ",
                ["NoAppsExcluded"] = "Chưa loại trừ ứng dụng nào",
                ["AppsExcluded"] = "Đang bỏ qua {0} tiến trình",
                ["Manage"] = "THIẾT LẬP",
                ["ManageTooltip"] = "Chọn ứng dụng để tắt cuộn mượt",

                ["RunningApps"] = "DANH SÁCH TIẾN TRÌNH",
                ["SearchApps"] = "Lọc theo tên tiến trình hoặc cửa sổ...",
                ["ToggleExcludeHint"] = "Chuyển sang TẮT để giữ nguyên cuộn mặc định của Windows trên ứng dụng đó.",
                ["BackgroundProcess"] = "Tiến trình chạy nền",
                ["Refresh"] = "QUÉT LẠI",

                ["TrayOpen"] = "Mở giao diện điều khiển",
                ["TrayToggle"] = "Bật / Tắt cuộn mượt",
                ["TrayExit"] = "Ngắt kết nối Scroll-V",

                ["MadeBy"] = "ĐƠN VỊ PHÁT TRIỂN: W00TDEV"
            },
            ["en"] = new Dictionary<string, string>
            {
                ["Settings"] = "SYSTEM PARAMETERS",
                ["Tuning"] = "PHYSICS CALIBRATION",
                ["Exceptions"] = "PROCESS EXCLUSIONS",

                ["StatusActive"] = "ONLINE",
                ["StatusPaused"] = "OFFLINE",

                ["EnableSmooth"] = "System-wide smooth scroll",
                ["EnableSmoothDesc"] = "Interpolate mouse wheel input across all windows",
                ["StartWithWindows"] = "Boot with operating system",
                ["StartWithWindowsDesc"] = "Launch silently in the system tray at sign-in",
                ["ScrollAcceleration"] = "Impulse acceleration",
                ["ScrollAccelerationDesc"] = "Cover more distance during rapid wheel flicks",

                ["Smoothness"] = "Response damping",
                ["ScrollSpeed"] = "Step multiplier",
                ["Momentum"] = "Inertia retention",
                ["Glide"] = "Coast distance",
                ["Animation"] = "Interpolation curve",

                ["VerySmooth"] = "Very smooth",
                ["Smooth"] = "Smooth",
                ["Medium"] = "Balanced",
                ["Fast"] = "Crisp",
                ["Instant"] = "Direct",
                ["Low"] = "Short",
                ["High"] = "Long",
                ["VeryHigh"] = "Extended",
                ["Subtle"] = "Subtle",
                ["Strong"] = "Strong",
                ["VeryStrong"] = "Very strong",
                ["Maximum"] = "Maximum",

                ["ExcludedApps"] = "Bypassed processes",
                ["NoAppsExcluded"] = "No applications bypassed",
                ["AppsExcluded"] = "{0} processes bypassed",
                ["Manage"] = "CONFIGURE",
                ["ManageTooltip"] = "Choose applications that keep native wheel behavior",

                ["RunningApps"] = "ACTIVE PROCESSES",
                ["SearchApps"] = "Filter by process or window title...",
                ["ToggleExcludeHint"] = "Set unit status to OFF to retain native Windows wheel scrolling.",
                ["BackgroundProcess"] = "Background process",
                ["Refresh"] = "RESCAN",

                ["TrayOpen"] = "Open HUD terminal",
                ["TrayToggle"] = "Toggle smooth scroll",
                ["TrayExit"] = "Terminate Scroll-V",

                ["MadeBy"] = "UNIT DEVELOPER: W00TDEV"
            }
        };

        public string Get(string key)
        {
            if (_strings.TryGetValue(_currentLanguage, out var langStrings) &&
                langStrings.TryGetValue(key, out var value))
            {
                return value;
            }
            return key;
        }

        public string this[string key] => Get(key);

        public void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == "vi" ? "en" : "vi";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
