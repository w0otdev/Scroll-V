using ScrollV.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScrollV
{
    public partial class RunningAppsDialog : Window
    {
        private static readonly string[] CommonApps =
        {
            "chrome", "firefox", "msedge", "opera", "brave",
            "explorer", "notepad", "code", "devenv",
            "discord", "telegram", "slack", "teams",
            "spotify", "vlc", "mpc-hc64", "potplayer",
            "word", "excel", "powerpoint", "outlook"
        };

        private readonly AppSettings _settings;
        private readonly LocalizationManager _loc = LocalizationManager.Instance;
        private readonly List<RunningAppInfo> _allApps = new();
        private ObservableCollection<RunningAppInfo> _filteredApps = new();

        public RunningAppsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            UpdateLanguageUI();
            LoadRunningApps();
        }

        private void UpdateLanguageUI()
        {
            Title = _loc["RunningApps"];
            TitleText.Text = _loc["RunningApps"];
            RefreshButton.Content = _loc["Refresh"];
            SearchPlaceholder.Text = _loc["SearchApps"];
            HintText.Text = _loc["ToggleExcludeHint"];
        }

        private void LoadRunningApps()
        {
            _allApps.Clear();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string currentProcessName = "Scroll-V";

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                UpdateFilteredList(SearchBox.Text);
                return;
            }

            foreach (var process in processes)
            {
                using (process)
                {
                    try
                    {
                        string procName = process.ProcessName;
                        if (string.IsNullOrWhiteSpace(procName) ||
                            procName.Equals(currentProcessName, StringComparison.OrdinalIgnoreCase) ||
                            !seenNames.Add(procName))
                        {
                            continue;
                        }

                        string title = process.MainWindowTitle;
                        if (string.IsNullOrEmpty(title) && !IsCommonApp(procName))
                        {
                            continue;
                        }

                        _allApps.Add(new RunningAppInfo
                        {
                            ProcessName = procName,
                            WindowTitle = string.IsNullOrEmpty(title) ? _loc["BackgroundProcess"] : title,
                            IsEnabled = !_settings.IsAppExcluded(procName)
                        });
                    }
                    catch
                    {
                        // Skip inaccessible system processes
                    }
                }
            }

            _allApps.Sort((a, b) => string.Compare(a.ProcessName, b.ProcessName, StringComparison.OrdinalIgnoreCase));
            UpdateFilteredList(SearchBox.Text);
        }

        private static bool IsCommonApp(string processName)
        {
            foreach (var app in CommonApps)
            {
                if (processName.Contains(app, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateFilteredList(string? searchText = null)
        {
            IEnumerable<RunningAppInfo> filtered = _allApps;

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(a =>
                    a.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    a.WindowTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            _filteredApps = new ObservableCollection<RunningAppInfo>(filtered);
            AppsList.ItemsSource = _filteredApps;
        }

        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchInputBorder.BorderBrush = (Brush)FindResource("AccentBrush");
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            SearchInputBorder.BorderBrush = (Brush)FindResource("BorderStrongBrush");
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(text)
                ? Visibility.Visible
                : Visibility.Collapsed;
            UpdateFilteredList(text);
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadRunningApps();
        }

        private void AppToggle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox checkBox || checkBox.Tag is not string processName) return;

            bool isEnabled = checkBox.IsChecked == true;
            if (isEnabled)
            {
                _settings.RemoveExcludedApp(processName);
            }
            else
            {
                _settings.AddExcludedApp(processName);
            }

            var app = _allApps.FirstOrDefault(a =>
                string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase));
            if (app != null)
            {
                app.IsEnabled = isEnabled;
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    public class RunningAppInfo : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value) return;
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
