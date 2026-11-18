using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WingetUpgrade.Models
{
    public class PackageItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isIgnored;
        private string _status = "Ready";
        private int _progressPercent = 0;
        private string _progressText = "";

        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string InstalledVersion { get; set; } = string.Empty;
        public string AvailableVersion { get; set; } = string.Empty;
        public string Source { get; set; } = "winget";
        public string IconKey { get; set; } = "GenericApp";
        public string Category { get; set; } = "General";
        public string Description { get; set; } = string.Empty;
        public bool IsPopular { get; set; } = false;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsIgnored
        {
            get => _isIgnored;
            set
            {
                if (_isIgnored != value)
                {
                    _isIgnored = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProgressPercent
        {
            get => _progressPercent;
            set
            {
                if (_progressPercent != value)
                {
                    _progressPercent = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProgressText
        {
            get => _progressText;
            set
            {
                if (_progressText != value)
                {
                    _progressText = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class UpgradeHistoryItem
    {
        public string PackageName { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public string FromVersion { get; set; } = string.Empty;
        public string ToVersion { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsSuccess { get; set; } = true;
        public string Details { get; set; } = string.Empty;
        public string FormattedDate => Timestamp.ToString("MMM dd, yyyy - hh:mm tt");
    }

    public class AppSettings
    {
        public List<string> IgnoredPackageIds { get; set; } = new();
        public List<UpgradeHistoryItem> History { get; set; } = new();
        public bool IncludeUnknown { get; set; } = false;
        public bool AutoCheckOnStartup { get; set; } = true;
        public bool DarkTheme { get; set; } = true;
        public DateTime LastScanTime { get; set; } = DateTime.MinValue;
    }
}
