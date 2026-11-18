using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WingetUpgrade.Models;

namespace WingetUpgrade.Services
{
    public static class PopularAppsCatalog
    {
        public static readonly List<string> Categories = new()
        {
            "All",
            "Browsers",
            "Development",
            "Utilities",
            "Media",
            "Communication",
            "Gaming",
            "Productivity"
        };

        private static readonly List<PackageItem> _popularApps = new()
        {
            // --- Browsers ---
            new PackageItem
            {
                Name = "Google Chrome",
                Id = "Google.Chrome",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Chrome",
                Category = "Browsers",
                Description = "Fast, secure, and customizable web browser by Google",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Mozilla Firefox",
                Id = "Mozilla.Firefox",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Firefox",
                Category = "Browsers",
                Description = "Free, open-source, and privacy-focused web browser",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Brave Browser",
                Id = "Brave.Brave",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Browsers",
                Description = "Fast, privacy-first browser with built-in ad blocking",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Microsoft Edge",
                Id = "Microsoft.Edge",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Browsers",
                Description = "Fast and secure Chromium-based browser with Copilot",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Opera GX",
                Id = "Opera.OperaGX",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Browsers",
                Description = "Gaming browser with CPU, RAM, and network limiters",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Vivaldi",
                Id = "VivaldiTechnologies.Vivaldi",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Browsers",
                Description = "Feature-packed browser with unmatched customization",
                IsPopular = true
            },

            // --- Development ---
            new PackageItem
            {
                Name = "Visual Studio Code",
                Id = "Microsoft.VisualStudioCode",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "VSCode",
                Category = "Development",
                Description = "Powerful, lightweight code editor with vast extensions",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Git",
                Id = "Git.Git",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Git",
                Category = "Development",
                Description = "Fast, scalable, distributed version control system",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "GitHub Desktop",
                Id = "GitHub.GitHubDesktop",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Git",
                Category = "Development",
                Description = "Seamless Git GUI for collaborating on GitHub repositories",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Python 3",
                Id = "Python.Python.3.12",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Python",
                Category = "Development",
                Description = "High-level programming language for web, AI, and scripts",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Node.js (LTS)",
                Id = "OpenJS.NodeJS.LTS",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Node",
                Category = "Development",
                Description = "Asynchronous event-driven JavaScript runtime environment",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Windows Terminal",
                Id = "Microsoft.WindowsTerminal",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Terminal",
                Category = "Development",
                Description = "Modern tabbed terminal for PowerShell, CMD, and WSL",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Notepad++",
                Id = "Notepad++.Notepad++",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Development",
                Description = "Fast, versatile source code editor and Notepad replacement",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Docker Desktop",
                Id = "Docker.DockerDesktop",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Development",
                Description = "Container platform to build, share, and run apps locally",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Postman",
                Id = "Postman.Postman",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Development",
                Description = "Comprehensive API platform for building and testing APIs",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Go Programming Language",
                Id = "GoLang.Go",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Go",
                Category = "Development",
                Description = "Open-source programming language for fast, reliable software",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Rust (rustup)",
                Id = "Rustlang.Rustup",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Rust",
                Category = "Development",
                Description = "The official installer for the Rust programming language",
                IsPopular = true
            },

            // --- Utilities ---
            new PackageItem
            {
                Name = "7-Zip",
                Id = "7zip.7zip",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "High compression file archiver utility with high speed",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Microsoft PowerToys",
                Id = "Microsoft.PowerToys",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Productivity utilities for Windows power users",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Everything (Search)",
                Id = "voidtools.Everything",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Instant desktop file and folder search engine for Windows",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Rufus",
                Id = "Rufus.Rufus",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Format and create bootable USB drives with ease",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "WinRAR",
                Id = "RARLab.WinRAR",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Powerful archive manager supporting RAR, ZIP, and more",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "ShareX",
                Id = "ShareX.ShareX",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Screen capture, file sharing and productivity tool",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "CPU-Z",
                Id = "CPUID.CPU-Z",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Hardware detection and monitoring software for processor and RAM",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "TreeSize Free",
                Id = "JAMSoftware.TreeSizeFree",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Fast hard drive space manager and folder size analyzer",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "AutoHotkey",
                Id = "AutoHotkey.AutoHotkey",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Powerful scripting language for Windows desktop automation",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "BleachBit",
                Id = "BleachBit.BleachBit",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Utilities",
                Description = "Free disk space cleaner and privacy manager",
                IsPopular = true
            },

            // --- Media ---
            new PackageItem
            {
                Name = "VLC media player",
                Id = "VideoLAN.VLC",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "VLC",
                Category = "Media",
                Description = "Universal multimedia player for all video and audio formats",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Spotify",
                Id = "Spotify.Spotify",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Spotify",
                Category = "Media",
                Description = "Music and podcast streaming with millions of songs",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "OBS Studio",
                Id = "OBSProject.OBSStudio",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "OBS",
                Category = "Media",
                Description = "Free and open-source software for video recording and live streaming",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Audacity",
                Id = "Audacity.Audacity",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Media",
                Description = "Easy-to-use, multi-track audio editor and recorder",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Blender",
                Id = "BlenderFoundation.Blender",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Media",
                Description = "Free and open-source 3D creation suite",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "GIMP",
                Id = "GIMP.GIMP",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Media",
                Description = "Cross-platform image editor and photo manipulation tool",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Paint.NET",
                Id = "dotPDNLLC.paintdotnet",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Media",
                Description = "Image and photo editing software for Windows PCs",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "HandBrake",
                Id = "HandBrake.HandBrake",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Media",
                Description = "Open-source video transcoder for converting video files",
                IsPopular = true
            },

            // --- Communication ---
            new PackageItem
            {
                Name = "Discord",
                Id = "Discord.Discord",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Discord",
                Category = "Communication",
                Description = "Voice, video, and text communication platform for communities",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Telegram Desktop",
                Id = "Telegram.TelegramDesktop",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Communication",
                Description = "Fast, secure desktop messaging with cloud synchronization",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "WhatsApp",
                Id = "WhatsApp.WhatsApp",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Communication",
                Description = "Simple, secure, and reliable messaging and calling",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Slack",
                Id = "SlackTechnologies.Slack",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Communication",
                Description = "Team communication and productivity platform for work",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Zoom",
                Id = "Zoom.Zoom",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Communication",
                Description = "Video conferencing, online meetings, and chat solution",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Microsoft Teams",
                Id = "Microsoft.Teams",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Communication",
                Description = "Workspace chat, video conferencing, and file storage",
                IsPopular = true
            },

            // --- Gaming ---
            new PackageItem
            {
                Name = "Steam",
                Id = "Valve.Steam",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Steam",
                Category = "Gaming",
                Description = "The ultimate destination for playing, discussing, and creating games",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Epic Games Launcher",
                Id = "EpicGames.EpicGamesLauncher",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Gaming",
                Description = "Storefront for PC games, free weekly titles, and Unreal Engine",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "GOG Galaxy",
                Id = "GOG.Galaxy",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Gaming",
                Description = "All your games and friends in one place, DRM-free",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Battle.net",
                Id = "Blizzard.BattleNet",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Gaming",
                Description = "Launcher for Blizzard games including Diablo, Overwatch, and WoW",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "EA app",
                Id = "ElectronicArts.EADesktop",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Gaming",
                Description = "Electronic Arts premier PC gaming platform",
                IsPopular = true
            },

            // --- Productivity ---
            new PackageItem
            {
                Name = "Notion",
                Id = "Notion.Notion",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Productivity",
                Description = "Connected workspace for wiki, docs, and project management",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Obsidian",
                Id = "Obsidian.Obsidian",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Productivity",
                Description = "Sharp and flexible markdown knowledge base that stays local",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "LibreOffice",
                Id = "TheDocumentFoundation.LibreOffice",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Productivity",
                Description = "Free, open-source office suite (Writer, Calc, Impress)",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Adobe Acrobat Reader",
                Id = "Adobe.Acrobat.Reader.64-bit",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "Adobe",
                Category = "Productivity",
                Description = "The trusted global standard for viewing, printing, and signing PDFs",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "Bitwarden",
                Id = "Bitwarden.Bitwarden",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Productivity",
                Description = "Secure, open-source password manager for all your devices",
                IsPopular = true
            },
            new PackageItem
            {
                Name = "AnyDesk",
                Id = "AnyDeskSoftwareGmbH.AnyDesk",
                AvailableVersion = "Latest",
                Source = "winget",
                IconKey = "GenericApp",
                Category = "Productivity",
                Description = "Fast, lightweight remote desktop application",
                IsPopular = true
            }
        };

        public static List<PackageItem> GetAllPopularApps(string category = "All")
        {
            var apps = _popularApps.Select(ClonePackage).ToList();
            if (!string.IsNullOrEmpty(category) && category != "All")
            {
                apps = apps.Where(a => a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            return apps;
        }

        public static bool IsPopularId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return _popularApps.Any(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        public static bool ContainsCjkCharacters(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            // Matches Chinese, Japanese, Korean character ranges
            return Regex.IsMatch(text, @"[\u4e00-\u9fff\u3400-\u4dbf\uF900-\uFAFF\u3040-\u30ff\uac00-\ud7af]");
        }

        public static List<PackageItem> PrioritizeAndFilterApps(List<PackageItem>? wingetResults, string? searchQuery)
        {
            searchQuery ??= string.Empty;
            bool userSearchedCjk = ContainsCjkCharacters(searchQuery);
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var combined = new List<PackageItem>();

            // 1. First add matching curated popular apps
            var matchingPopular = _popularApps
                .Where(p => string.IsNullOrWhiteSpace(searchQuery) ||
                            (p.Name?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                            (p.Id?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                            (p.Category?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true) ||
                            (p.Description?.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) == true))
                .Select(ClonePackage)
                .ToList();

            foreach (var pop in matchingPopular)
            {
                if (!string.IsNullOrEmpty(pop.Id) && seenIds.Add(pop.Id))
                {
                    combined.Add(pop);
                }
            }

            // 2. Then add Winget search results, filtering out CJK/obscure items unless requested
            if (wingetResults != null)
            {
                foreach (var item in wingetResults)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;
                    if (seenIds.Contains(item.Id)) continue;

                    // Filter out Chinese / East Asian characters if user did not query in CJK
                    if (!userSearchedCjk && (ContainsCjkCharacters(item.Name) || ContainsCjkCharacters(item.Id)))
                    {
                        continue; // Skip Chinese apps as requested by user
                    }

                    // Filter out obscure numeric-prefixed IDs (e.g. 0-don.clippy, 115.115Chrome, 0x192...) when not specifically searched
                    if (string.IsNullOrWhiteSpace(searchQuery) && !string.IsNullOrEmpty(item.Id) && char.IsDigit(item.Id[0]))
                    {
                        continue;
                    }

                    seenIds.Add(item.Id);
                    combined.Add(item);
                }
            }

            return combined;
        }

        private static PackageItem ClonePackage(PackageItem src)
        {
            return new PackageItem
            {
                Name = src.Name,
                Id = src.Id,
                InstalledVersion = src.InstalledVersion,
                AvailableVersion = src.AvailableVersion,
                Source = src.Source,
                IconKey = src.IconKey,
                Category = src.Category,
                Description = src.Description,
                IsPopular = src.IsPopular,
                Status = "Ready"
            };
        }
    }
}
