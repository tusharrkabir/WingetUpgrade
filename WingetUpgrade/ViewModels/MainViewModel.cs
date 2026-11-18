using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WingetUpgrade.Models;
using WingetUpgrade.Services;

namespace WingetUpgrade.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly WingetService _wingetService;
        private readonly SettingsService _settingsService;
        private CancellationTokenSource? _upgradeCts;

        // Debounce tokens for fast typing
        private CancellationTokenSource? _searchDebounceCts;
        private CancellationTokenSource? _appsSearchDebounceCts;
        private CancellationTokenSource? _installedSearchDebounceCts;

        // Full log buffer for viewing full output without lagging UI
        private readonly StringBuilder _fullLogBuilder = new();
        private DateTime _lastConsoleScrollTime = DateTime.MinValue;

        // Active View/Tab
        private string _activeTab = "Overview";

        // Stats
        private int _updatesCount = 0;
        private int _upToDateCount = 0;
        private int _ignoredCount = 0;
        private int _installedPackagesCount = 0;
        private string _lastScannedText = "Never scanned";

        // Loading states
        private bool _isScanning = false;
        private bool _isUpgrading = false;
        private bool _isLoadingInstalled = false;

        // Search & Filter
        private string _searchText = string.Empty;
        private string _selectedSource = "All sources";
        private string _selectedSort = "Sort by: Name";

        // Selection
        private int _selectedCount = 0;
        private bool? _selectAllChecked = false;

        // Progress & Live console
        private double _currentProgressPercent = 0;
        private string _currentProgressText = "Ready";
        private string _downloadStatsText = "Idle";
        private bool _isConsoleVisible = false;

        // Winget Apps tab state
        private string _wingetAppsSearchText = string.Empty;
        private bool _isSearchingApps = false;
        private string _selectedAppsSource = "All sources";
        private string _selectedCategory = "All";

        // Installed tab state
        private string _installedSearchText = string.Empty;

        // Settings & Theme
        private bool _isOledBlackTheme = false;
        private bool _disableInteractivity = true;

        // Collections
        private readonly ObservableCollection<PackageItem> _allPackages = new();
        public ObservableCollection<PackageItem> FilteredPackages { get; } = new();
        public ObservableCollection<PackageItem> IgnoredPackages { get; } = new();
        public ObservableCollection<PackageItem> InstalledPackages { get; } = new();
        public ObservableCollection<PackageItem> FilteredInstalledPackages { get; } = new();
        public ObservableCollection<PackageItem> WingetApps { get; } = new();
        public ObservableCollection<PackageItem> FilteredWingetApps { get; } = new();
        public ObservableCollection<UpgradeHistoryItem> History { get; } = new();
        public ObservableCollection<string> ConsoleLogs { get; } = new();

        public List<string> SourcesList { get; } = new() { "All sources", "winget", "msstore" };
        public List<string> SortOptionsList { get; } = new() { "Sort by: Name", "Sort by: Version", "Sort by: ID", "Sort by: Source" };
        public List<string> CategoriesList { get; } = PopularAppsCatalog.Categories;

        // Tracking for packages upgraded in this session to prevent background scan from re-adding them
        private readonly HashSet<string> _recentlyUpgradedPackageIds = new(StringComparer.OrdinalIgnoreCase);

        public Action? RequestScrollConsoleToBottom { get; set; }
        public Action<string>? OpenFullLogAction { get; set; }

        public MainViewModel()
        {
            _wingetService = new WingetService();
            _settingsService = new SettingsService();

            // Commands
            NavigateCommand = new RelayCommand(p => ActiveTab = p?.ToString() ?? "Overview");
            ScanForUpdatesCommand = new AsyncRelayCommand(ScanForUpdatesAsync, () => !IsScanning && !IsUpgrading);
            UpgradeAllCommand = new AsyncRelayCommand(UpgradeAllAsync, () => !IsScanning && !IsUpgrading && FilteredPackages.Count > 0);
            UpgradeSelectedCommand = new AsyncRelayCommand(UpgradeSelectedAsync, () => !IsScanning && !IsUpgrading && SelectedCount > 0);
            UpgradeSingleCommand = new AsyncRelayCommand(async p =>
            {
                if (p is PackageItem item) await UpgradeSingleAsync(item);
            }, () => !IsScanning && !IsUpgrading);

            SearchWingetAppsCommand = new AsyncRelayCommand(async () => await SearchWingetAppsAsync(WingetAppsSearchText), () => !IsSearchingApps);
            SelectCategoryCommand = new RelayCommand(p => SelectedCategory = p?.ToString() ?? "All");
            InstallWingetAppCommand = new AsyncRelayCommand(async p =>
            {
                if (p is PackageItem item) await InstallWingetAppAsync(item);
            }, () => !IsUpgrading && !IsScanning);

            IgnorePackageCommand = new RelayCommand(p =>
            {
                if (p is PackageItem item) IgnorePackage(item);
            });

            UnignorePackageCommand = new RelayCommand(p =>
            {
                if (p is PackageItem item) UnignorePackage(item);
            });

            CopyPackageIdCommand = new RelayCommand(p =>
            {
                string? idToCopy = null;
                if (p is PackageItem item && !string.IsNullOrWhiteSpace(item.Id))
                {
                    idToCopy = item.Id;
                }
                else if (p is string s && !string.IsNullOrWhiteSpace(s))
                {
                    idToCopy = s.Trim();
                }

                if (!string.IsNullOrWhiteSpace(idToCopy))
                {
                    try
                    {
                        Clipboard.SetText(idToCopy);
                        AddLog($"Copied package ID '{idToCopy}' to clipboard.");
                    }
                    catch (Exception ex)
                    {
                        AddLog($"Failed to copy ID to clipboard: {ex.Message}");
                    }
                }
            });

            SelectAllCommand = new RelayCommand(() => SetAllSelected(true));
            DeselectAllCommand = new RelayCommand(() => SetAllSelected(false));
            ClearLogsCommand = new RelayCommand(() =>
            {
                ConsoleLogs.Clear();
                lock (_fullLogBuilder)
                {
                    _fullLogBuilder.Clear();
                }
                AddLog("Console output cleared.");
            });
            CancelOperationCommand = new RelayCommand(CancelCurrentOperation, () => IsUpgrading || IsScanning);
            ViewFullLogCommand = new RelayCommand(() =>
            {
                string fullLog;
                lock (_fullLogBuilder)
                {
                    fullLog = _fullLogBuilder.ToString();
                }
                OpenFullLogAction?.Invoke(fullLog);
            });

            ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
            ClearAppsSearchCommand = new RelayCommand(() => WingetAppsSearchText = string.Empty);
            ClearInstalledSearchCommand = new RelayCommand(() => InstalledSearchText = string.Empty);
            ToggleThemeCommand = new RelayCommand(() => IsOledBlackTheme = !IsOledBlackTheme);

            ClearHistoryCommand = new RelayCommand(() =>
            {
                History.Clear();
                _settingsService.Settings.History.Clear();
                _settingsService.Save();
                AddLog("Upgrade history records cleared.");
            });

            RefreshCacheCommand = new AsyncRelayCommand(async () =>
            {
                _wingetService.InvalidateCache();
                AddLog("Cache invalidated. Refreshing packages...");
                await ScanForUpdatesAsync();
                if (ActiveTab == "Installed")
                {
                    await LoadInstalledPackagesAsync(force: true);
                }
            }, () => !IsScanning && !IsUpgrading);

            // Initial state
            CurrentProgressPercent = 0;
            CurrentProgressText = "Winget Upgrade ready";
            DownloadStatsText = "Idle";
            IsConsoleVisible = true;

            ConsoleLogs.Clear();
            ConsoleLogs.Add("> Winget Upgrade client initialized.");
            ConsoleLogs.Add("> Ready for operations.");

            // Load saved settings
            foreach (var h in _settingsService.Settings.History)
            {
                History.Add(h);
            }

            _isOledBlackTheme = _settingsService.Settings.DarkTheme;
            ApplyTheme(_isOledBlackTheme);
        }

        public async Task InitializeOnLoadedAsync()
        {
            if (_settingsService.Settings.AutoCheckOnStartup)
            {
                await ScanForUpdatesAsync();
            }
        }

        public string ActiveTab
        {
            get => _activeTab;
            set
            {
                if (SetProperty(ref _activeTab, value))
                {
                    if (value == "Installed" && InstalledPackages.Count == 0)
                    {
                        _ = LoadInstalledPackagesAsync();
                    }
                    else if (value == "WingetApps" && WingetApps.Count == 0)
                    {
                        _ = SearchWingetAppsAsync("");
                    }
                }
            }
        }

        public int UpdatesCount
        {
            get => _updatesCount;
            set => SetProperty(ref _updatesCount, value);
        }

        public int UpToDateCount
        {
            get => _upToDateCount;
            set => SetProperty(ref _upToDateCount, value);
        }

        public int IgnoredCount
        {
            get => _ignoredCount;
            set => SetProperty(ref _ignoredCount, value);
        }

        public int InstalledPackagesCount
        {
            get => _installedPackagesCount;
            set => SetProperty(ref _installedPackagesCount, value);
        }

        public string LastScannedText
        {
            get => _lastScannedText;
            set => SetProperty(ref _lastScannedText, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                if (SetProperty(ref _isScanning, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsUpgrading
        {
            get => _isUpgrading;
            set
            {
                if (SetProperty(ref _isUpgrading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsLoadingInstalled
        {
            get => _isLoadingInstalled;
            set => SetProperty(ref _isLoadingInstalled, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    DebounceOverviewFilter();
                }
            }
        }

        public string SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (SetProperty(ref _selectedSource, value))
                {
                    ApplyFilters();
                }
            }
        }

        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (SetProperty(ref _selectedSort, value))
                {
                    ApplyFilters();
                }
            }
        }

        public int SelectedCount
        {
            get => _selectedCount;
            set => SetProperty(ref _selectedCount, value);
        }

        public bool? SelectAllChecked
        {
            get => _selectAllChecked;
            set
            {
                if (SetProperty(ref _selectAllChecked, value) && value.HasValue)
                {
                    SetAllSelected(value.Value);
                }
            }
        }

        public double CurrentProgressPercent
        {
            get => _currentProgressPercent;
            set => SetProperty(ref _currentProgressPercent, value);
        }

        public string CurrentProgressText
        {
            get => _currentProgressText;
            set => SetProperty(ref _currentProgressText, value);
        }

        public string DownloadStatsText
        {
            get => _downloadStatsText;
            set => SetProperty(ref _downloadStatsText, value);
        }

        public bool IsConsoleVisible
        {
            get => _isConsoleVisible;
            set => SetProperty(ref _isConsoleVisible, value);
        }

        // Winget Apps tab properties
        public string WingetAppsSearchText
        {
            get => _wingetAppsSearchText;
            set
            {
                if (SetProperty(ref _wingetAppsSearchText, value))
                {
                    DebounceAppsFilter();
                }
            }
        }

        public bool IsSearchingApps
        {
            get => _isSearchingApps;
            set => SetProperty(ref _isSearchingApps, value);
        }

        public string SelectedAppsSource
        {
            get => _selectedAppsSource;
            set
            {
                if (SetProperty(ref _selectedAppsSource, value))
                {
                    ApplyAppsFilter();
                }
            }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    ApplyAppsFilter();
                }
            }
        }

        // Installed tab search property
        public string InstalledSearchText
        {
            get => _installedSearchText;
            set
            {
                if (SetProperty(ref _installedSearchText, value))
                {
                    DebounceInstalledFilter();
                }
            }
        }

        // Settings tab properties
        public bool AutoCheckOnStartup
        {
            get => _settingsService.Settings.AutoCheckOnStartup;
            set
            {
                if (_settingsService.Settings.AutoCheckOnStartup != value)
                {
                    _settingsService.Settings.AutoCheckOnStartup = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                }
            }
        }

        public bool IncludeUnknown
        {
            get => _settingsService.Settings.IncludeUnknown;
            set
            {
                if (_settingsService.Settings.IncludeUnknown != value)
                {
                    _settingsService.Settings.IncludeUnknown = value;
                    _settingsService.Save();
                    OnPropertyChanged();
                    _ = ScanForUpdatesAsync();
                }
            }
        }

        public bool DisableInteractivity
        {
            get => _disableInteractivity;
            set => SetProperty(ref _disableInteractivity, value);
        }

        public bool IsOledBlackTheme
        {
            get => _isOledBlackTheme;
            set
            {
                if (SetProperty(ref _isOledBlackTheme, value))
                {
                    _settingsService.Settings.DarkTheme = value;
                    _settingsService.Save();
                    ApplyTheme(value);
                }
            }
        }

        // Commands
        public ICommand NavigateCommand { get; }
        public ICommand ScanForUpdatesCommand { get; }
        public ICommand UpgradeAllCommand { get; }
        public ICommand UpgradeSelectedCommand { get; }
        public ICommand UpgradeSingleCommand { get; }
        public ICommand SearchWingetAppsCommand { get; }
        public ICommand SelectCategoryCommand { get; }
        public ICommand InstallWingetAppCommand { get; }
        public ICommand IgnorePackageCommand { get; }
        public ICommand UnignorePackageCommand { get; }
        public ICommand CopyPackageIdCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand DeselectAllCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand CancelOperationCommand { get; }
        public ICommand ViewFullLogCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ClearAppsSearchCommand { get; }
        public ICommand ClearInstalledSearchCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand RefreshCacheCommand { get; }

        public void ApplyTheme(bool oledBlack)
        {
            if (Application.Current == null) return;
            try
            {
                if (oledBlack)
                {
                    Application.Current.Resources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(0x00, 0x00, 0x00));
                    Application.Current.Resources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0x06, 0x08, 0x0A));
                    Application.Current.Resources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x11));
                    Application.Current.Resources["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x13, 0x16, 0x1D));
                    Application.Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x18, 0x1C, 0x24));
                    Application.Current.Resources["ConsoleBgBrush"] = new SolidColorBrush(Color.FromRgb(0x03, 0x04, 0x05));
                }
                else
                {
                    Application.Current.Resources["AppBgBrush"] = new SolidColorBrush(Color.FromRgb(0x0D, 0x0F, 0x12));
                    Application.Current.Resources["SidebarBgBrush"] = new SolidColorBrush(Color.FromRgb(0x11, 0x13, 0x17));
                    Application.Current.Resources["CardBgBrush"] = new SolidColorBrush(Color.FromRgb(0x15, 0x18, 0x1E));
                    Application.Current.Resources["CardHoverBrush"] = new SolidColorBrush(Color.FromRgb(0x1C, 0x20, 0x28));
                    Application.Current.Resources["CardBorderBrush"] = new SolidColorBrush(Color.FromRgb(0x22, 0x26, 0x30));
                    Application.Current.Resources["ConsoleBgBrush"] = new SolidColorBrush(Color.FromRgb(0x0B, 0x0D, 0x11));
                }
            }
            catch { }
        }

        private void DebounceOverviewFilter()
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts = new CancellationTokenSource();
            var token = _searchDebounceCts.Token;

            Task.Delay(180, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Application.Current?.Dispatcher?.Invoke(ApplyFilters);
                }
            }, TaskScheduler.Default);
        }

        private void DebounceAppsFilter()
        {
            _appsSearchDebounceCts?.Cancel();
            _appsSearchDebounceCts = new CancellationTokenSource();
            var token = _appsSearchDebounceCts.Token;

            Task.Delay(180, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Application.Current?.Dispatcher?.Invoke(ApplyAppsFilter);
                }
            }, TaskScheduler.Default);
        }

        private void DebounceInstalledFilter()
        {
            _installedSearchDebounceCts?.Cancel();
            _installedSearchDebounceCts = new CancellationTokenSource();
            var token = _installedSearchDebounceCts.Token;

            Task.Delay(180, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    Application.Current?.Dispatcher?.Invoke(ApplyInstalledFilter);
                }
            }, TaskScheduler.Default);
        }

        public async Task ScanForUpdatesAsync()
        {
            await ScanForUpdatesInternalAsync(isBackgroundRefresh: false);
        }

        private async Task ScanForUpdatesInternalAsync(bool isBackgroundRefresh)
        {
            if (IsScanning || IsUpgrading) return;

            IsScanning = true;
            if (!isBackgroundRefresh)
            {
                IsConsoleVisible = true;
                CurrentProgressPercent = 0;
                CurrentProgressText = "Scanning for available updates...";
                DownloadStatsText = "Querying repository in parallel...";
            }
            AddLog("Scanning for updates via winget upgrade...");

            try
            {
                // Run upgrade check and installed stats in parallel! Cuts scan wait time by ~50%
                var upgradesTask = _wingetService.GetUpgradesAsync(_settingsService.Settings.IncludeUnknown);
                var statsTask = _wingetService.GetInstalledStatsAsync(0);

                await Task.WhenAll(upgradesTask, statsTask);

                var upgrades = await upgradesTask;
                var (total, upToDate) = await statsTask;

                // Retain existing selections
                var previousSelections = _allPackages.ToDictionary(p => p.Id, p => p.IsSelected);
                bool hasPriorSelections = previousSelections.Count > 0;

                _allPackages.Clear();
                IgnoredPackages.Clear();

                foreach (var pkg in upgrades)
                {
                    if (_recentlyUpgradedPackageIds.Contains(pkg.Id))
                    {
                        continue;
                    }

                    pkg.PropertyChanged += OnPackagePropertyChanged;

                    if (_settingsService.IsIgnored(pkg.Id))
                    {
                        pkg.IsIgnored = true;
                        pkg.IsSelected = false;
                        IgnoredPackages.Add(pkg);
                    }
                    else
                    {
                        if (hasPriorSelections && previousSelections.TryGetValue(pkg.Id, out bool wasSelected))
                        {
                            pkg.IsSelected = wasSelected;
                        }
                        else if (hasPriorSelections)
                        {
                            pkg.IsSelected = false;
                        }
                        else
                        {
                            pkg.IsSelected = true;
                        }
                        _allPackages.Add(pkg);
                    }
                }

                UpdatesCount = _allPackages.Count;
                IgnoredCount = IgnoredPackages.Count;
                InstalledPackagesCount = total;
                UpToDateCount = Math.Max(0, total - UpdatesCount);

                ApplyFilters();
                LastScannedText = $"Today, {DateTime.Now:hh:mm tt}";
                AddLog($"Found {UpdatesCount} available update(s). Total installed: {InstalledPackagesCount}.");

                if (!isBackgroundRefresh)
                {
                    CurrentProgressPercent = 0;
                    CurrentProgressText = UpdatesCount > 0
                        ? $"{UpdatesCount} update(s) available."
                        : "All packages are up to date.";
                    DownloadStatsText = "Idle";
                }
            }
            catch (Exception ex)
            {
                AddLog($"Scan error: {ex.Message}");
                if (!isBackgroundRefresh)
                {
                    CurrentProgressText = "Scan error";
                    DownloadStatsText = "Failed";
                }
            }
            finally
            {
                IsScanning = false;
                if (!isBackgroundRefresh)
                {
                    CurrentProgressPercent = 0;
                }
                UpdateSelectedCount();
            }
        }

        public async Task LoadInstalledPackagesAsync(bool force = false)
        {
            if (IsLoadingInstalled) return;
            IsLoadingInstalled = true;
            try
            {
                AddLog("Loading installed software list...");
                var installed = await _wingetService.GetInstalledPackagesAsync(force);
                InstalledPackages.Clear();
                foreach (var p in installed)
                {
                    InstalledPackages.Add(p);
                }
                ApplyInstalledFilter();

                InstalledPackagesCount = Math.Max(InstalledPackagesCount, InstalledPackages.Count);
                UpToDateCount = Math.Max(0, InstalledPackagesCount - UpdatesCount);
                AddLog($"Loaded {InstalledPackages.Count} managed packages.");
            }
            catch (Exception ex)
            {
                AddLog($"Error loading installed packages: {ex.Message}");
            }
            finally
            {
                IsLoadingInstalled = false;
            }
        }

        public async Task UpgradeAllAsync()
        {
            var targets = FilteredPackages.ToList();
            if (targets.Count == 0) return;

            await UpgradeMultiplePackagesAsync(targets);
        }

        public async Task UpgradeSelectedAsync()
        {
            var targets = FilteredPackages.Where(p => p.IsSelected).ToList();
            if (targets.Count == 0) return;

            await UpgradeMultiplePackagesAsync(targets);
        }

        public async Task UpgradeSingleAsync(PackageItem item)
        {
            await UpgradeMultiplePackagesAsync(new List<PackageItem> { item });
        }

        private async Task UpgradeMultiplePackagesAsync(List<PackageItem> items)
        {
            if (IsUpgrading) return;

            IsUpgrading = true;
            IsConsoleVisible = true;
            _upgradeCts = new CancellationTokenSource();

            CurrentProgressPercent = 0;
            CurrentProgressText = "Starting upgrade...";
            DownloadStatsText = "Initializing...";

            int total = items.Count;
            int completed = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (_upgradeCts.IsCancellationRequested) break;

                var item = items[i];
                item.Status = "Downloading...";
                CurrentProgressPercent = 0;
                CurrentProgressText = $"Downloading {item.Name}...";
                AddLog($"Upgrading {item.Name} {item.AvailableVersion}...");

                bool success = await _wingetService.UpgradePackageAsync(
                    item.Id,
                    onOutput: line => AddLog(line),
                    onProgress: (phase, pct, detail) =>
                    {
                        CurrentProgressPercent = pct;
                        if (phase == "Downloading")
                        {
                            item.Status = "Downloading...";
                            CurrentProgressText = $"Downloading {item.Name}... ({pct}%)";
                        }
                        else
                        {
                            item.Status = "Installing...";
                            CurrentProgressText = $"Installing {item.Name}... ({pct}%)";
                        }

                        if (!string.IsNullOrEmpty(detail))
                        {
                            DownloadStatsText = detail;
                        }
                    },
                    _upgradeCts.Token
                );

                if (success)
                {
                    _recentlyUpgradedPackageIds.Add(item.Id);
                    item.Status = "Completed";
                    completed++;
                    _settingsService.AddHistory(item.Name, item.Id, item.InstalledVersion, item.AvailableVersion, true, "Success");
                    AddLog($"Successfully upgraded {item.Name}");

                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        _allPackages.Remove(item);
                        FilteredPackages.Remove(item);
                        UpdatesCount = _allPackages.Count;
                        UpToDateCount++;
                        UpdateSelectedCount();
                    });
                }
                else
                {
                    item.Status = "Failed";
                    _settingsService.AddHistory(item.Name, item.Id, item.InstalledVersion, item.AvailableVersion, false, "Failed or cancelled");
                    AddLog($"Failed to upgrade {item.Name}");
                }
            }

            CurrentProgressPercent = 100;
            CurrentProgressText = completed == 1 && total == 1
                ? $"Successfully upgraded {items[0].Name}!"
                : $"Finished: {completed} of {total} package(s) upgraded.";
            DownloadStatsText = "Completed";
            IsUpgrading = false;

            // Refresh history view
            History.Clear();
            foreach (var h in _settingsService.Settings.History) History.Add(h);

            // Silent background refresh to sync external state
            _ = ScanForUpdatesInternalAsync(isBackgroundRefresh: true);
        }

        public void CancelCurrentOperation()
        {
            AddLog("Forcefully cancelling active operation and terminating installer processes...");
            try
            {
                _upgradeCts?.Cancel();
            }
            catch { }

            _wingetService.KillActiveProcesses();

            IsUpgrading = false;
            IsScanning = false;
            CurrentProgressPercent = 0;
            CurrentProgressText = "Operation cancelled";
            DownloadStatsText = "Forcefully stopped by user";
            AddLog("Operation forcefully aborted.");
        }

        public void IgnorePackage(PackageItem item)
        {
            if (item == null || IgnoredPackages.Contains(item)) return;

            _settingsService.AddIgnored(item.Id);
            item.IsIgnored = true;
            item.IsSelected = false;

            _allPackages.Remove(item);
            IgnoredPackages.Add(item);

            UpdatesCount = _allPackages.Count;
            IgnoredCount = IgnoredPackages.Count;
            ApplyFilters();
            AddLog($"Ignored {item.Name}");
        }

        public void UnignorePackage(PackageItem item)
        {
            if (item == null || _allPackages.Contains(item)) return;

            _settingsService.RemoveIgnored(item.Id);
            item.IsIgnored = false;
            item.IsSelected = true;

            IgnoredPackages.Remove(item);
            _allPackages.Add(item);

            UpdatesCount = _allPackages.Count;
            IgnoredCount = IgnoredPackages.Count;
            ApplyFilters();
            AddLog($"Unignored {item.Name}");
        }

        private void SetAllSelected(bool isSelected)
        {
            foreach (var p in FilteredPackages)
            {
                p.IsSelected = isSelected;
            }
            UpdateSelectedCount();
        }

        private void OnPackagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackageItem.IsSelected))
            {
                UpdateSelectedCount();
            }
        }

        private void UpdateSelectedCount()
        {
            SelectedCount = FilteredPackages.Count(p => p.IsSelected);
            if (FilteredPackages.Count == 0)
            {
                _selectAllChecked = false;
            }
            else if (SelectedCount == FilteredPackages.Count)
            {
                _selectAllChecked = true;
            }
            else if (SelectedCount == 0)
            {
                _selectAllChecked = false;
            }
            else
            {
                _selectAllChecked = null;
            }
            OnPropertyChanged(nameof(SelectAllChecked));
        }

        public void ApplyFilters()
        {
            var query = _allPackages.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.Name.ToLowerInvariant().Contains(search) ||
                    p.Id.ToLowerInvariant().Contains(search));
            }

            if (!string.IsNullOrEmpty(SelectedSource) && SelectedSource != "All sources")
            {
                query = query.Where(p => p.Source.Equals(SelectedSource, StringComparison.OrdinalIgnoreCase));
            }

            query = SelectedSort switch
            {
                "Sort by: Version" => query.OrderBy(p => p.AvailableVersion),
                "Sort by: ID" => query.OrderBy(p => p.Id),
                "Sort by: Source" => query.OrderBy(p => p.Source),
                _ => query.OrderBy(p => p.Name)
            };

            FilteredPackages.Clear();
            foreach (var p in query)
            {
                FilteredPackages.Add(p);
            }

            UpdateSelectedCount();
        }

        public void ApplyInstalledFilter()
        {
            var query = InstalledPackages.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(InstalledSearchText))
            {
                string search = InstalledSearchText.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.Name.ToLowerInvariant().Contains(search) ||
                    p.Id.ToLowerInvariant().Contains(search));
            }

            FilteredInstalledPackages.Clear();
            foreach (var p in query)
            {
                FilteredInstalledPackages.Add(p);
            }
        }

        public async Task SearchWingetAppsAsync(string query)
        {
            if (IsSearchingApps) return;
            IsSearchingApps = true;
            string term = string.IsNullOrWhiteSpace(query) ? "" : query.Trim();
            AddLog($"Searching Winget repository{(string.IsNullOrEmpty(term) ? "..." : $" for '{term}'...")}");

            try
            {
                var results = await _wingetService.SearchPackagesAsync(term);
                WingetApps.Clear();
                foreach (var item in results)
                {
                    WingetApps.Add(item);
                }
                ApplyAppsFilter();
                AddLog($"Found {WingetApps.Count} app(s) in Winget repository.");
            }
            catch (Exception ex)
            {
                AddLog($"Error searching Winget apps: {ex.Message}");
            }
            finally
            {
                IsSearchingApps = false;
            }
        }

        public void ApplyAppsFilter()
        {
            var query = WingetApps.AsEnumerable();

            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
            {
                query = query.Where(p => p.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(WingetAppsSearchText))
            {
                string search = WingetAppsSearchText.Trim().ToLowerInvariant();
                query = query.Where(p =>
                    p.Name.ToLowerInvariant().Contains(search) ||
                    p.Id.ToLowerInvariant().Contains(search) ||
                    p.Category.ToLowerInvariant().Contains(search) ||
                    p.Description.ToLowerInvariant().Contains(search));
            }

            if (!string.IsNullOrEmpty(SelectedAppsSource) && SelectedAppsSource != "All sources")
            {
                query = query.Where(p => p.Source.Equals(SelectedAppsSource, StringComparison.OrdinalIgnoreCase));
            }

            FilteredWingetApps.Clear();
            foreach (var app in query)
            {
                FilteredWingetApps.Add(app);
            }
        }

        public async Task InstallWingetAppAsync(PackageItem item)
        {
            if (IsUpgrading || IsScanning) return;
            IsUpgrading = true;
            IsConsoleVisible = true;
            _upgradeCts = new CancellationTokenSource();

            CurrentProgressPercent = 0;
            CurrentProgressText = $"Downloading {item.Name}...";
            DownloadStatsText = "Starting download...";
            item.Status = "Downloading...";
            AddLog($"Installing {item.Name} ({item.Id})...");

            bool success = await _wingetService.InstallPackageAsync(
                item.Id,
                onOutput: line => AddLog(line),
                onProgress: (phase, pct, detail) =>
                {
                    CurrentProgressPercent = pct;
                    if (phase == "Downloading")
                    {
                        item.Status = "Downloading...";
                        CurrentProgressText = $"Downloading {item.Name}... ({pct}%)";
                    }
                    else
                    {
                        item.Status = "Installing...";
                        CurrentProgressText = $"Installing {item.Name}... ({pct}%)";
                    }

                    if (!string.IsNullOrEmpty(detail))
                    {
                        DownloadStatsText = detail;
                    }
                },
                _upgradeCts.Token
            );

            if (success)
            {
                _recentlyUpgradedPackageIds.Add(item.Id);
                item.Status = "Installed";
                CurrentProgressPercent = 100;
                CurrentProgressText = $"Successfully installed {item.Name}!";
                DownloadStatsText = "Completed";
                AddLog($"Successfully installed {item.Name}");
                _settingsService.AddHistory(item.Name, item.Id, "None", item.AvailableVersion, true, "Installed");

                InstalledPackagesCount++;
                UpToDateCount++;
            }
            else
            {
                item.Status = "Failed";
                CurrentProgressPercent = 0;
                CurrentProgressText = $"Failed to install {item.Name}";
                DownloadStatsText = "Failed or cancelled";
                AddLog($"Installation failed or was cancelled for {item.Name}");
            }

            IsUpgrading = false;
        }

        private void AddLog(string message)
        {
            string line = $"> {message}";
            lock (_fullLogBuilder)
            {
                _fullLogBuilder.AppendLine(line);
            }

            Application.Current?.Dispatcher?.InvokeAsync(() =>
            {
                ConsoleLogs.Add(line);
                while (ConsoleLogs.Count > 150)
                {
                    ConsoleLogs.RemoveAt(0);
                }

                if ((DateTime.Now - _lastConsoleScrollTime).TotalMilliseconds > 100)
                {
                    _lastConsoleScrollTime = DateTime.Now;
                    RequestScrollConsoleToBottom?.Invoke();
                }
            }, DispatcherPriority.Background);
        }
    }
}
