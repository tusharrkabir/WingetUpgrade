using System;
using System.IO;
using System.Text.Json;
using WingetUpgrade.Models;

namespace WingetUpgrade.Services
{
    public class SettingsService
    {
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WingetUpgrade"
        );
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        private AppSettings _settings = new();

        public AppSettings Settings => _settings;

        public SettingsService()
        {
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                    if (loaded != null)
                    {
                        _settings = loaded;
                        return;
                    }
                }
            }
            catch
            {
                // Fallback to default
            }

            _settings = new AppSettings();
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
                // Ignore failure to write
            }
        }

        public bool IsIgnored(string packageId)
        {
            return _settings.IgnoredPackageIds.Contains(packageId, StringComparer.OrdinalIgnoreCase);
        }

        public void AddIgnored(string packageId)
        {
            if (!IsIgnored(packageId))
            {
                _settings.IgnoredPackageIds.Add(packageId);
                Save();
            }
        }

        public void RemoveIgnored(string packageId)
        {
            _settings.IgnoredPackageIds.RemoveAll(id => string.Equals(id, packageId, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        public void AddHistory(string name, string id, string fromVer, string toVer, bool success, string details)
        {
            _settings.History.Insert(0, new UpgradeHistoryItem
            {
                PackageName = name,
                PackageId = id,
                FromVersion = fromVer,
                ToVersion = toVer,
                Timestamp = DateTime.Now,
                IsSuccess = success,
                Details = details
            });

            if (_settings.History.Count > 200)
            {
                _settings.History.RemoveRange(200, _settings.History.Count - 200);
            }

            Save();
        }
    }
}
