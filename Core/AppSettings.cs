using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ScrollV.Core
{
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Scroll-V",
            "settings.json"
        );

        public bool Enabled { get; set; } = true;
        public bool StartWithWindows { get; set; } = true;
        public string Language { get; set; } = "vi";

        public double SmoothnessFactor { get; set; } = 0.05;
        public double AccelerationFactor { get; set; } = 1.2;
        public double FrictionFactor { get; set; } = 0.97;
        public double MomentumFactor { get; set; } = 3.2;
        public EasingType EasingFunction { get; set; } = EasingType.EaseOutQuad;
        public bool UseAcceleration { get; set; } = true;
        public double ScrollMultiplier { get; set; } = 1.4;

        public List<string> ExcludedApps { get; set; } = new()
        {
            "vlc",
            "mpc-hc64",
            "mpc-hc",
            "potplayermini64",
            "photoshop",
            "illustrator",
            "idman",
            "idmsettings",
            "iemonitor"
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                    settings.NormalizeExcludedApps();
                    return settings;
                }
            }
            catch
            {
                // Fall back to defaults on corrupted settings file
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                NormalizeExcludedApps();
                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Ignore transient I/O errors
            }
        }

        public void NormalizeExcludedApps()
        {
            if (ExcludedApps == null)
            {
                ExcludedApps = new List<string>();
                return;
            }

            ExcludedApps = ExcludedApps
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => a.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public bool IsAppExcluded(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return false;
            return ExcludedApps.Contains(processName.Trim(), StringComparer.OrdinalIgnoreCase);
        }

        public void AddExcludedApp(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            string name = processName.Trim().ToLowerInvariant();
            if (!IsAppExcluded(name))
            {
                ExcludedApps.Add(name);
            }
        }

        public void RemoveExcludedApp(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName)) return;
            ExcludedApps.RemoveAll(a => string.Equals(a, processName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
