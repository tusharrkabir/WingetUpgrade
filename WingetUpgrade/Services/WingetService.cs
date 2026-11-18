using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WingetUpgrade.Models;

namespace WingetUpgrade.Services
{
    public class WingetService
    {
        private List<PackageItem>? _cachedInstalledPackages;
        private DateTime _lastInstalledFetch = DateTime.MinValue;
        private readonly SemaphoreSlim _cacheLock = new(1, 1);

        private Process? _activeProcess;
        private readonly object _processLock = new();

        public void KillActiveProcesses()
        {
            lock (_processLock)
            {
                try
                {
                    if (_activeProcess != null && !_activeProcess.HasExited)
                    {
                        _activeProcess.Kill(entireProcessTree: true);
                        _activeProcess = null;
                    }
                }
                catch { }
            }

            try
            {
                foreach (var p in Process.GetProcessesByName("winget"))
                {
                    try { p.Kill(true); } catch { }
                }
            }
            catch { }
        }

        public void InvalidateCache()
        {
            _cachedInstalledPackages = null;
        }

        public static bool IsWingetAvailable()
        {
            try
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                p.Start();
                p.WaitForExit(3000);
                return p.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<PackageItem>> GetUpgradesAsync(bool includeUnknown = false)
        {
            var results = new List<PackageItem>();
            string args = "upgrade";
            if (includeUnknown)
            {
                args += " --include-unknown";
            }

            var outputLines = await RunProcessAndCaptureLinesAsync("winget", args);
            if (outputLines.Count == 0)
            {
                return results;
            }

            // Locate header row containing "Name", "Id", "Version", "Available"
            int headerIndex = -1;
            for (int i = 0; i < outputLines.Count; i++)
            {
                string line = outputLines[i];
                if (line.Contains("Name") && line.Contains("Id") && line.Contains("Version") && line.Contains("Available"))
                {
                    headerIndex = i;
                    break;
                }
            }

            if (headerIndex == -1 || headerIndex + 2 > outputLines.Count)
            {
                return results;
            }

            string header = outputLines[headerIndex];
            int idIdx = header.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
            int verIdx = header.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
            int availIdx = header.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
            int sourceIdx = header.IndexOf("Source", StringComparison.OrdinalIgnoreCase);

            if (idIdx == -1 || verIdx == -1 || availIdx == -1)
            {
                return results;
            }

            for (int i = headerIndex + 2; i < outputLines.Count; i++)
            {
                string line = outputLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith("-") || line.Contains("upgrades available") || line.Contains("package(s) have version"))
                    continue;

                if (line.Length <= idIdx) continue;

                try
                {
                    string name = line.Substring(0, Math.Min(line.Length, idIdx)).Trim();
                    string id = "";
                    string installed = "";
                    string available = "";
                    string source = "winget";

                    if (line.Length > idIdx)
                    {
                        int idLen = (verIdx > idIdx && line.Length >= verIdx) ? verIdx - idIdx : line.Length - idIdx;
                        id = line.Substring(idIdx, idLen).Trim();
                    }

                    if (verIdx != -1 && line.Length > verIdx)
                    {
                        int verLen = (availIdx > verIdx && line.Length >= availIdx) ? availIdx - verIdx : line.Length - verIdx;
                        installed = line.Substring(verIdx, verLen).Trim();
                    }

                    if (availIdx != -1 && line.Length > availIdx)
                    {
                        int availLen = (sourceIdx != -1 && sourceIdx > availIdx && line.Length >= sourceIdx) ? sourceIdx - availIdx : line.Length - availIdx;
                        available = line.Substring(availIdx, availLen).Trim();
                    }

                    if (sourceIdx != -1 && line.Length > sourceIdx)
                    {
                        source = line.Substring(sourceIdx).Trim();
                    }

                    if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(name))
                    {
                        results.Add(new PackageItem
                        {
                            Name = name,
                            Id = id,
                            InstalledVersion = installed,
                            AvailableVersion = available,
                            Source = string.IsNullOrEmpty(source) ? "winget" : source,
                            IconKey = DetermineIconKey(id, name),
                            IsSelected = true
                        });
                    }
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            return results;
        }

        public async Task<(int TotalInstalled, int UpToDateCount)> GetInstalledStatsAsync(int updateCount)
        {
            var packages = await GetInstalledPackagesAsync();
            int total = packages.Count > 0 ? packages.Count : Math.Max(updateCount, 15);
            int upToDate = Math.Max(0, total - updateCount);
            return (total, upToDate);
        }

        public async Task<List<PackageItem>> GetInstalledPackagesAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && _cachedInstalledPackages != null && (DateTime.Now - _lastInstalledFetch).TotalMinutes < 5)
            {
                return _cachedInstalledPackages;
            }

            await _cacheLock.WaitAsync();
            try
            {
                if (!forceRefresh && _cachedInstalledPackages != null && (DateTime.Now - _lastInstalledFetch).TotalMinutes < 5)
                {
                    return _cachedInstalledPackages;
                }

                var results = new List<PackageItem>();
                var outputLines = await RunProcessAndCaptureLinesAsync("winget", "list");
                int headerIndex = -1;
                for (int i = 0; i < outputLines.Count; i++)
                {
                    string line = outputLines[i];
                    if (line.Contains("Name") && line.Contains("Id") && line.Contains("Version"))
                    {
                        headerIndex = i;
                        break;
                    }
                }

                if (headerIndex == -1 || headerIndex + 2 > outputLines.Count)
                {
                    _cachedInstalledPackages = results;
                    _lastInstalledFetch = DateTime.Now;
                    return results;
                }

                string header = outputLines[headerIndex];
                int idIdx = header.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                int verIdx = header.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                int availIdx = header.IndexOf("Available", StringComparison.OrdinalIgnoreCase);
                int sourceIdx = header.IndexOf("Source", StringComparison.OrdinalIgnoreCase);

                for (int i = headerIndex + 2; i < outputLines.Count; i++)
                {
                    string line = outputLines[i];
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("-")) continue;
                    if (line.Length <= idIdx) continue;

                    try
                    {
                        string name = line.Substring(0, Math.Min(line.Length, idIdx)).Trim();
                        int idLen = (verIdx > idIdx && line.Length >= verIdx) ? verIdx - idIdx : line.Length - idIdx;
                        string id = line.Substring(idIdx, idLen).Trim();

                        // FILTER: Genuine packages, exclude unmanaged ARP / driver / registry entries
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) continue;
                        if (id.StartsWith("ARP\\", StringComparison.OrdinalIgnoreCase) || id.StartsWith("{")) continue;

                        string source = "";
                        if (sourceIdx != -1 && line.Length > sourceIdx)
                        {
                            source = line.Substring(sourceIdx).Trim();
                        }

                        // Must have a valid supported repository (winget or msstore)
                        if (string.IsNullOrWhiteSpace(source)) continue;
                        if (!source.Equals("winget", StringComparison.OrdinalIgnoreCase) &&
                            !source.Equals("msstore", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string ver = "";
                        if (verIdx != -1 && line.Length > verIdx)
                        {
                            int verLen = (availIdx != -1 && availIdx > verIdx && line.Length >= availIdx) ? availIdx - verIdx :
                                         (sourceIdx != -1 && sourceIdx > verIdx && line.Length >= sourceIdx) ? sourceIdx - verIdx :
                                         line.Length - verIdx;
                            ver = line.Substring(verIdx, verLen).Trim();
                        }

                        results.Add(new PackageItem
                        {
                            Name = name,
                            Id = id,
                            InstalledVersion = ver,
                            AvailableVersion = ver,
                            Source = source,
                            IconKey = DetermineIconKey(id, name)
                        });
                    }
                    catch
                    {
                    }
                }

                _cachedInstalledPackages = results;
                _lastInstalledFetch = DateTime.Now;
                return results;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async Task<List<PackageItem>> SearchPackagesAsync(string query, int maxResults = 40)
        {
            string searchQuery = string.IsNullOrWhiteSpace(query) ? "" : query.Trim();

            // If empty search, return curated popular apps directly (instant & popular apps on top)
            if (string.IsNullOrEmpty(searchQuery))
            {
                return PopularAppsCatalog.GetAllPopularApps();
            }

            var results = new List<PackageItem>();
            string args = $"search \"{searchQuery}\" --count {maxResults}";

            var outputLines = await RunProcessAndCaptureLinesAsync("winget", args);
            if (outputLines.Count > 0)
            {
                int headerIndex = -1;
                for (int i = 0; i < outputLines.Count; i++)
                {
                    string line = outputLines[i];
                    if (line.Contains("Name") && line.Contains("Id") && line.Contains("Version"))
                    {
                        headerIndex = i;
                        break;
                    }
                }

                if (headerIndex != -1 && headerIndex + 2 <= outputLines.Count)
                {
                    string header = outputLines[headerIndex];
                    int idIdx = header.IndexOf("Id", StringComparison.OrdinalIgnoreCase);
                    int verIdx = header.IndexOf("Version", StringComparison.OrdinalIgnoreCase);
                    int matchIdx = header.IndexOf("Match", StringComparison.OrdinalIgnoreCase);
                    int sourceIdx = header.IndexOf("Source", StringComparison.OrdinalIgnoreCase);

                    if (idIdx != -1 && verIdx != -1)
                    {
                        for (int i = headerIndex + 2; i < outputLines.Count; i++)
                        {
                            string line = outputLines[i];
                            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("-")) continue;
                            if (line.Length <= idIdx) continue;

                            try
                            {
                                string name = line.Substring(0, Math.Min(line.Length, idIdx)).Trim();
                                int endOfId = (verIdx > idIdx && line.Length >= verIdx) ? verIdx : line.Length;
                                string id = line.Substring(idIdx, endOfId - idIdx).Trim();

                                int nextAfterVer = line.Length;
                                if (matchIdx != -1 && matchIdx > verIdx) nextAfterVer = Math.Min(nextAfterVer, matchIdx);
                                if (sourceIdx != -1 && sourceIdx > verIdx) nextAfterVer = Math.Min(nextAfterVer, sourceIdx);

                                string version = "";
                                if (line.Length > verIdx)
                                {
                                    version = line.Substring(verIdx, Math.Min(line.Length, nextAfterVer) - verIdx).Trim();
                                }

                                string source = "winget";
                                if (sourceIdx != -1 && line.Length > sourceIdx)
                                {
                                    source = line.Substring(sourceIdx).Trim();
                                }

                                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(name))
                                {
                                    results.Add(new PackageItem
                                    {
                                        Name = name,
                                        Id = id,
                                        InstalledVersion = "",
                                        AvailableVersion = version,
                                        Source = string.IsNullOrEmpty(source) ? "winget" : source,
                                        IconKey = DetermineIconKey(id, name),
                                        Status = "Ready"
                                    });
                                }
                            }
                            catch
                            {
                                // Ignore line parsing errors
                            }
                        }
                    }
                }
            }

            return PopularAppsCatalog.PrioritizeAndFilterApps(results, searchQuery);
        }

        public async Task<bool> InstallPackageAsync(
            string packageId,
            Action<string>? onOutput,
            Action<string, int, string>? onProgress,
            CancellationToken ct)
        {
            // Primary attempt: Exact ID + include unknown + full silent + accept agreements + force
            string arguments = $"install --exact --id \"{packageId}\" --include-unknown --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --force";
            bool success = await ExecuteWingetCommandAsync(arguments, onOutput, onProgress, ct);

            // Smart fallback if silent mode is rejected by installer or manifest
            if (!success && !ct.IsCancellationRequested)
            {
                onOutput?.Invoke("[Info] Retrying with interactive/unattended mode...");
                string fallbackArgs = $"install --exact --id \"{packageId}\" --include-unknown --accept-package-agreements --accept-source-agreements --force";
                success = await ExecuteWingetCommandAsync(fallbackArgs, onOutput, onProgress, ct);
            }
            return success;
        }

        public async Task<bool> UpgradePackageAsync(
            string packageId,
            Action<string>? onOutput,
            Action<string, int, string>? onProgress,
            CancellationToken ct)
        {
            // Primary attempt: Exact ID + include unknown + full silent + accept agreements + force
            string arguments = $"upgrade --exact --id \"{packageId}\" --include-unknown --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --force";
            bool success = await ExecuteWingetCommandAsync(arguments, onOutput, onProgress, ct);

            // Smart fallback if silent mode fails or needs standard installer wizard
            if (!success && !ct.IsCancellationRequested)
            {
                onOutput?.Invoke("[Info] Retrying upgrade with interactive/unattended mode...");
                string fallbackArgs = $"upgrade --exact --id \"{packageId}\" --include-unknown --accept-package-agreements --accept-source-agreements --force";
                success = await ExecuteWingetCommandAsync(fallbackArgs, onOutput, onProgress, ct);
            }
            return success;
        }

        public async Task<bool> UpgradeAllAsync(
            Action<string>? onOutput,
            Action<string, int, string>? onProgress,
            CancellationToken ct)
        {
            string arguments = "upgrade --all --include-unknown --silent --accept-package-agreements --accept-source-agreements --disable-interactivity --force";
            return await ExecuteWingetCommandAsync(arguments, onOutput, onProgress, ct);
        }

        public async Task<bool> UninstallPackageAsync(
            string packageId,
            Action<string>? onOutput,
            CancellationToken ct)
        {
            string arguments = $"uninstall --exact --id \"{packageId}\" --silent --disable-interactivity --force";
            return await ExecuteWingetCommandAsync(arguments, onOutput, null, ct);
        }

        private async Task<bool> ExecuteWingetCommandAsync(
            string arguments,
            Action<string>? onOutput,
            Action<string, int, string>? onProgress,
            CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var controller = new WingetProgressController(onProgress);
            var outputLog = new List<string>();

            try
            {
                using var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        string line = e.Data;
                        outputLog.Add(line);
                        onOutput?.Invoke(line);
                        controller.OnOutputLine(line);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        outputLog.Add(e.Data);
                        onOutput?.Invoke("[ERR] " + e.Data);
                    }
                };

                controller.StartDownload();
                lock (_processLock)
                {
                    _activeProcess = process;
                }
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (ct.Register(() =>
                {
                    KillActiveProcesses();
                }))
                {
                    await process.WaitForExitAsync(ct);

                    bool hasSuccessText = outputLog.Any(l =>
                        l.Contains("Successfully installed", StringComparison.OrdinalIgnoreCase) ||
                        l.Contains("Successfully upgraded", StringComparison.OrdinalIgnoreCase) ||
                        l.Contains("Restart the application to complete the upgrade", StringComparison.OrdinalIgnoreCase));

                    bool success = (process.ExitCode == 0 || process.ExitCode == 3010 || hasSuccessText);

                    if (outputLog.Any(l => l.Contains("Restart the application to complete the upgrade", StringComparison.OrdinalIgnoreCase) ||
                                           l.Contains("apps need to be closed", StringComparison.OrdinalIgnoreCase)))
                    {
                        onOutput?.Invoke("[Notice] Update completed/staged. Please restart the application to finalize the upgrade.");
                    }

                    if (success) InvalidateCache();
                    await controller.CompleteAsync(success);
                    return success;
                }
            }
            catch (OperationCanceledException)
            {
                controller.Cancel();
                onOutput?.Invoke("Operation cancelled by user.");
                return false;
            }
            catch (Exception ex)
            {
                controller.Cancel();
                onOutput?.Invoke("Error: " + ex.Message);
                return false;
            }
            finally
            {
                lock (_processLock)
                {
                    _activeProcess = null;
                }
            }
        }

        private class WingetProgressController
        {
            private readonly Action<string, int, string>? _onProgress;
            private string _phase = "Downloading";
            private int _progressPercent = 0;
            private readonly object _lock = new object();
            private CancellationTokenSource? _tickerCts;
            private readonly Stopwatch _stopwatch = new Stopwatch();
            private bool _transitioning = false;
            private bool _isCompleted = false;

            public WingetProgressController(Action<string, int, string>? onProgress)
            {
                _onProgress = onProgress;
            }

            public void StartDownload()
            {
                lock (_lock)
                {
                    _phase = "Downloading";
                    _progressPercent = 10;
                    _stopwatch.Restart();
                    _onProgress?.Invoke("Downloading", 10, "Starting download...");
                    StartDownloadTicker();
                }
            }

            private void StartDownloadTicker()
            {
                _tickerCts?.Cancel();
                _tickerCts = new CancellationTokenSource();
                var token = _tickerCts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await Task.Delay(400, token);
                            lock (_lock)
                            {
                                if (_phase != "Downloading" || _transitioning || _isCompleted) break;
                                // Smoothly advance download bar towards 92% so it never looks stuck at 10%
                                if (_progressPercent < 90)
                                {
                                    _progressPercent += 2;
                                    _onProgress?.Invoke("Downloading", _progressPercent, $"Downloading package... ({_progressPercent}%)");
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }, token);
            }

            public async Task TransitionToInstallingAsync()
            {
                lock (_lock)
                {
                    if (_transitioning || _phase == "Installing" || _isCompleted) return;
                    _transitioning = true;
                }

                _tickerCts?.Cancel();

                // 1. Full 100% for downloading phase as requested!
                lock (_lock)
                {
                    _progressPercent = 100;
                    _onProgress?.Invoke("Downloading", 100, "Download complete • 100%");
                }

                // Brief visual pause so user clearly sees 100% download bar
                await Task.Delay(400);

                // 2. Restart from 0% for installing phase!
                lock (_lock)
                {
                    if (_isCompleted) return;
                    _phase = "Installing";
                    _progressPercent = 0;
                    _transitioning = false;
                    _stopwatch.Restart();
                    _onProgress?.Invoke("Installing", 0, "Initializing installer...");
                    StartInstallTicker();
                }
            }

            private void StartInstallTicker()
            {
                _tickerCts?.Cancel();
                _tickerCts = new CancellationTokenSource();
                var token = _tickerCts.Token;

                Task.Run(async () =>
                {
                    try
                    {
                        while (!token.IsCancellationRequested)
                        {
                            await Task.Delay(750, token);
                            lock (_lock)
                            {
                                if (_phase != "Installing" || _isCompleted) break;
                                int elapsed = (int)_stopwatch.Elapsed.TotalSeconds;

                                // Progress through realistic installation stages with live elapsed seconds
                                if (_progressPercent < 15)
                                {
                                    _progressPercent = Math.Min(15, _progressPercent + 3);
                                    _onProgress?.Invoke("Installing", _progressPercent, "Preparing installer...");
                                }
                                else if (_progressPercent < 45)
                                {
                                    _progressPercent = Math.Min(45, _progressPercent + 2);
                                    _onProgress?.Invoke("Installing", _progressPercent, $"Running silent installer... ({elapsed}s)");
                                }
                                else if (_progressPercent < 72)
                                {
                                    _progressPercent = Math.Min(72, _progressPercent + 1);
                                    _onProgress?.Invoke("Installing", _progressPercent, $"Configuring components... ({elapsed}s)");
                                }
                                else if (_progressPercent < 94)
                                {
                                    if (elapsed % 2 == 0 && _progressPercent < 94) _progressPercent += 1;
                                    _onProgress?.Invoke("Installing", _progressPercent, $"Writing system files... ({elapsed}s)");
                                }
                                else
                                {
                                    _onProgress?.Invoke("Installing", _progressPercent, $"Finalizing installation... ({elapsed}s)");
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                }, token);
            }

            public void OnOutputLine(string line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                string lower = line.ToLowerInvariant();

                lock (_lock)
                {
                    if (_phase == "Downloading")
                    {
                        // Check if winget shifted to hash verification or install
                        if (lower.Contains("hash") || lower.Contains("verified") ||
                            lower.Contains("starting package install") || lower.Contains("starting install") ||
                            lower.Contains("extracting"))
                        {
                            _ = TransitionToInstallingAsync();
                            return;
                        }

                        // Check for real download percentage and speed
                        var percentMatch = Regex.Match(line, @"(\d{1,3})%");
                        if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int pct))
                        {
                            _progressPercent = Math.Clamp(pct, 10, 99);
                            string detail = $"{pct}%";

                            var sizeMatch = Regex.Match(line, @"([\d\.]+\s*(?:MB|KB|GB)\s*/\s*[\d\.]+\s*(?:MB|KB|GB))");
                            if (sizeMatch.Success) detail = sizeMatch.Groups[1].Value;

                            var speedMatch = Regex.Match(line, @"([\d\.]+\s*(?:MB/s|KB/s|GB/s))");
                            if (speedMatch.Success) detail += " • " + speedMatch.Groups[1].Value;

                            _onProgress?.Invoke("Downloading", _progressPercent, detail);
                        }
                    }
                    else if (_phase == "Installing")
                    {
                        var percentMatch = Regex.Match(line, @"(\d{1,3})%");
                        if (percentMatch.Success && int.TryParse(percentMatch.Groups[1].Value, out int pct))
                        {
                            _progressPercent = Math.Clamp(pct, 0, 99);
                            _onProgress?.Invoke("Installing", _progressPercent, $"Installing... ({pct}%)");
                        }
                        else if (lower.Contains("extracting"))
                        {
                            _progressPercent = Math.Max(_progressPercent, 20);
                            _onProgress?.Invoke("Installing", _progressPercent, "Extracting package files...");
                        }
                    }
                }
            }

            public async Task CompleteAsync(bool success)
            {
                _tickerCts?.Cancel();
                lock (_lock)
                {
                    _isCompleted = true;
                }

                if (success)
                {
                    // Ensure download finished visual before showing install complete
                    if (_phase == "Downloading")
                    {
                        _onProgress?.Invoke("Downloading", 100, "Download complete • 100%");
                        await Task.Delay(200);
                    }
                    _phase = "Installing";
                    _progressPercent = 100;
                    _onProgress?.Invoke("Installing", 100, "Installation completed successfully");
                }
                else
                {
                    _onProgress?.Invoke(_phase, 0, "Operation failed or cancelled");
                }
            }

            public void Cancel()
            {
                _tickerCts?.Cancel();
                lock (_lock)
                {
                    _isCompleted = true;
                    _onProgress?.Invoke(_phase, 0, "Cancelled");
                }
            }
        }

        private static async Task<List<string>> RunProcessAndCaptureLinesAsync(string exe, string args)
        {
            var lines = new List<string>();
            try
            {
                using var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                p.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null) lines.Add(e.Data);
                };

                p.Start();
                p.BeginOutputReadLine();
                await p.WaitForExitAsync();
            }
            catch
            {
                // Fallback / error
            }
            return lines;
        }

        public static string DetermineIconKey(string id, string name)
        {
            string lowerId = id.ToLowerInvariant();
            string lowerName = name.ToLowerInvariant();

            if (lowerId.Contains("chrome") || lowerName.Contains("chrome")) return "Chrome";
            if (lowerId.Contains("vlc") || lowerName.Contains("vlc")) return "VLC";
            if (lowerId.Contains("git") || lowerName.Contains("git")) return "Git";
            if (lowerId.Contains("code") || lowerName.Contains("visual studio code")) return "VSCode";
            if (lowerId.Contains("firefox") || lowerName.Contains("firefox")) return "Firefox";
            if (lowerId.Contains("discord") || lowerName.Contains("discord")) return "Discord";
            if (lowerId.Contains("spotify") || lowerName.Contains("spotify")) return "Spotify";
            if (lowerId.Contains("steam") || lowerName.Contains("steam")) return "Steam";
            if (lowerId.Contains("terminal") || lowerName.Contains("terminal")) return "Terminal";
            if (lowerId.Contains("obs") || lowerName.Contains("obs")) return "OBS";
            if (lowerId.Contains("node") || lowerName.Contains("node")) return "Node";
            if (lowerId.Contains("python") || lowerName.Contains("python")) return "Python";
            if (lowerId.Contains("adobe") || lowerName.Contains("acrobat")) return "Adobe";
            if (lowerId.Contains("antigravity")) return "Antigravity";
            if (lowerId.Contains("claude")) return "Claude";
            if (lowerId.Contains("cursor")) return "Cursor";
            if (lowerId.Contains("rust") || lowerName.Contains("rust")) return "Rust";
            if (lowerId.Contains("golang") || lowerName.Contains("go programming")) return "Go";

            return "GenericApp";
        }
    }
}

