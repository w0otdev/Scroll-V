using ScrollV.Core;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScrollV
{
    public partial class MainWindow : Window
    {
        private readonly ScrollVManager _manager;
        private readonly LocalizationManager _loc = LocalizationManager.Instance;
        private readonly SmoothScrollBehavior _smoothScroll;
        private bool _isLoading;

        public MainWindow() : this(((App)Application.Current).GetManager() ?? new ScrollVManager())
        {
        }

        public MainWindow(ScrollVManager manager)
        {
            _manager = manager;
            _isLoading = true;

            InitializeComponent();
            _smoothScroll = SmoothScrollBehavior.Attach(ContentScrollViewer);

            var ver = typeof(MainWindow).Assembly.GetName().Version;
            SysVersionText.Text = ver != null ? $"SYS.VER // {ver.Major}.{ver.Minor}" : "SYS.VER // 1.1";

            _loc.CurrentLanguage = _manager.Settings.Language;
            _loc.LanguageChanged += OnLanguageChanged;

            LoadSettings();
            UpdateLanguageUI();
            UpdateStatusUI();
        }

        private void LoadSettings()
        {
            _isLoading = true;
            try
            {
                var settings = _manager.Settings;
                EnableToggle.IsChecked = settings.Enabled;
                StartupToggle.IsChecked = App.IsStartWithWindowsEnabled();
                AccelerationToggle.IsChecked = settings.UseAcceleration;

                SmoothnessSlider.Value = settings.SmoothnessFactor * 100;
                SpeedSlider.Value = settings.ScrollMultiplier * 100;
                FrictionSlider.Value = settings.FrictionFactor * 100;
                GlideSlider.Value = settings.MomentumFactor * 100;

                foreach (ComboBoxItem item in EasingComboBox.Items)
                {
                    if (item.Tag?.ToString() == settings.EasingFunction.ToString())
                    {
                        EasingComboBox.SelectedItem = item;
                        break;
                    }
                }

                RefreshExcludedAppsList();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RefreshExcludedAppsList()
        {
            int count = _manager.Settings.ExcludedApps?.Count ?? 0;
            ExcludedAppsCount.Text = count == 0
                ? _loc["NoAppsExcluded"]
                : string.Format(_loc["AppsExcluded"], count);
        }

        private void UpdateStatusUI()
        {
            bool active = _manager.IsRunning;
            StatusIndicator.Fill = (Brush)FindResource(active ? "TextInvertedBrush" : "WarningBrush");
            StatusText.Text = active ? _loc["StatusActive"] : _loc["StatusPaused"];

            if (EnableToggle.IsChecked != active)
            {
                _isLoading = true;
                EnableToggle.IsChecked = active;
                _isLoading = false;
            }
        }

        #region Language

        private void LangViButton_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("vi");
        }

        private void LangEnButton_Click(object sender, RoutedEventArgs e)
        {
            SetLanguage("en");
        }

        private void SetLanguage(string lang)
        {
            _loc.CurrentLanguage = lang;
            _manager.Settings.Language = lang;
            SaveSettings();
        }

        private void OnLanguageChanged(object? sender, string lang)
        {
            Dispatcher.Invoke(UpdateLanguageUI);
        }

        private void UpdateLanguageUI()
        {
            LblSettings.Text = _loc["Settings"];
            LblTuning.Text = _loc["Tuning"];
            LblExceptions.Text = _loc["Exceptions"];

            LblEnableSmooth.Text = _loc["EnableSmooth"];
            LblEnableSmoothDesc.Text = _loc["EnableSmoothDesc"];
            LblStartWindows.Text = _loc["StartWithWindows"];
            LblStartWindowsDesc.Text = _loc["StartWithWindowsDesc"];
            LblAcceleration.Text = _loc["ScrollAcceleration"];
            LblAccelerationDesc.Text = _loc["ScrollAccelerationDesc"];

            LblSmoothness.Text = _loc["Smoothness"];
            LblSpeed.Text = _loc["ScrollSpeed"];
            LblMomentum.Text = _loc["Momentum"];
            LblGlide.Text = _loc["Glide"];
            LblAnimation.Text = _loc["Animation"];

            LblExcludedApps.Text = _loc["ExcludedApps"];
            BrowseAppsButton.Content = _loc["Manage"];
            BrowseAppsButton.ToolTip = _loc["ManageTooltip"];

            TrayOpenMenuItem.Header = _loc["TrayOpen"];
            TrayToggleMenuItem.Header = _loc["TrayToggle"];
            TrayExitMenuItem.Header = _loc["TrayExit"];

            LblMadeBy.Text = _loc["MadeBy"];

            RefreshExcludedAppsList();
            UpdateSliderLabels();
            UpdateLanguageSwitchUI();
            UpdateStatusUI();
        }

        private void UpdateLanguageSwitchUI()
        {
            var activeBg = (Brush)FindResource("SurfaceBrush");
            var activeFg = (Brush)FindResource("TextPrimaryBrush");
            var inactiveFg = (Brush)FindResource("TextInvertedBrush");

            bool isVi = _loc.CurrentLanguage == "vi";
            LangViButton.Background = isVi ? activeBg : Brushes.Transparent;
            LangViButton.Foreground = isVi ? activeFg : inactiveFg;

            LangEnButton.Background = !isVi ? activeBg : Brushes.Transparent;
            LangEnButton.Foreground = !isVi ? activeFg : inactiveFg;
        }

        private void UpdateSliderLabels()
        {
            FormatSmoothnessLabel(_manager.Settings.SmoothnessFactor);
            FormatSpeedLabel(_manager.Settings.ScrollMultiplier);
            FormatFrictionLabel(_manager.Settings.FrictionFactor);
            FormatGlideLabel(_manager.Settings.MomentumFactor);
        }

        private void FormatSmoothnessLabel(double value)
        {
            if (SmoothnessValue == null) return;
            if (value < 0.08) SmoothnessValue.Text = _loc["VerySmooth"];
            else if (value < 0.12) SmoothnessValue.Text = _loc["Smooth"];
            else if (value < 0.18) SmoothnessValue.Text = _loc["Medium"];
            else if (value < 0.25) SmoothnessValue.Text = _loc["Fast"];
            else SmoothnessValue.Text = _loc["Instant"];
        }

        private void FormatSpeedLabel(double value)
        {
            if (SpeedValue == null) return;
            SpeedValue.Text = $"{value:F1}x";
        }

        private void FormatFrictionLabel(double value)
        {
            if (FrictionValue == null) return;
            if (value < 0.85) FrictionValue.Text = _loc["Low"];
            else if (value < 0.90) FrictionValue.Text = _loc["Medium"];
            else if (value < 0.95) FrictionValue.Text = _loc["High"];
            else FrictionValue.Text = _loc["VeryHigh"];
        }

        private void FormatGlideLabel(double value)
        {
            if (GlideValue == null) return;
            if (value < 1.5) GlideValue.Text = _loc["Subtle"];
            else if (value < 2.5) GlideValue.Text = _loc["Medium"];
            else if (value < 3.5) GlideValue.Text = _loc["Strong"];
            else if (value < 4.5) GlideValue.Text = _loc["VeryStrong"];
            else GlideValue.Text = _loc["Maximum"];
        }

        #endregion

        #region Social Links

        private void SocialGithub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/w00tdev");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        #endregion

        #region Window Controls

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        #endregion

        #region Settings Controls

        private void EnableToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _manager == null) return;
            _manager.Settings.Enabled = EnableToggle.IsChecked == true;
            _manager.ApplySettings();
            UpdateStatusUI();
            SaveSettings();
        }

        private void StartupToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _manager == null) return;
            bool enabled = StartupToggle.IsChecked == true;
            _manager.Settings.StartWithWindows = enabled;
            App.SetStartWithWindows(enabled);
            SaveSettings();
        }

        private void AccelerationToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading || _manager == null) return;
            _manager.Settings.UseAcceleration = AccelerationToggle.IsChecked == true;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void SmoothnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null) return;
            double value = SmoothnessSlider.Value / 100.0;
            FormatSmoothnessLabel(value);
            if (_isLoading) return;

            _manager.Settings.SmoothnessFactor = value;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null) return;
            double value = SpeedSlider.Value / 100.0;
            FormatSpeedLabel(value);
            if (_isLoading) return;

            _manager.Settings.ScrollMultiplier = value;
            _manager.Settings.AccelerationFactor = 1.0 + (value - 1.0) * 0.5;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void FrictionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null) return;
            double value = FrictionSlider.Value / 100.0;
            FormatFrictionLabel(value);
            if (_isLoading) return;

            _manager.Settings.FrictionFactor = value;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void GlideSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null) return;
            double value = GlideSlider.Value / 100.0;
            FormatGlideLabel(value);
            if (_isLoading) return;

            _manager.Settings.MomentumFactor = value;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void EasingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || _manager == null || EasingComboBox.SelectedItem is not ComboBoxItem item) return;

            if (Enum.TryParse<EasingType>(item.Tag?.ToString(), out var easing))
            {
                _manager.Settings.EasingFunction = easing;
                _manager.ApplySettings();
                SaveSettings();
            }
        }

        #endregion

        #region Excluded Apps Dialog

        private void BrowseAppsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RunningAppsDialog(_manager.Settings)
            {
                Owner = this
            };
            dialog.ShowDialog();

            RefreshExcludedAppsList();
            SaveSettings();
        }

        #endregion

        #region System Tray

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        public void ShowWindow()
        {
            ShowInTaskbar = true;
            Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
            Show();
            Activate();
            Focus();

            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ShowWindowAsync(hwnd, 9);
                SetForegroundWindow(hwnd);
            }
        }

        public void HideWindow()
        {
            ShowInTaskbar = false;
            Visibility = Visibility.Hidden;
            Hide();
            App.OptimizeMemory(true);
        }

        private void TrayToggle_Click(object sender, RoutedEventArgs e)
        {
            _manager.Toggle();
            UpdateStatusUI();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon.Dispose();
            _manager.Dispose();
            Application.Current.Shutdown();
        }

        #endregion

        private void SaveSettings()
        {
            _manager.SaveSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            _loc.LanguageChanged -= OnLanguageChanged;
            _smoothScroll.Detach();
            TrayIcon.Dispose();
            base.OnClosed(e);
        }
    }
}
