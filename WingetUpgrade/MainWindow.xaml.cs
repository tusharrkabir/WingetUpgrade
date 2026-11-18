using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WingetUpgrade.Models;
using WingetUpgrade.ViewModels;
using WingetUpgrade.Views;

namespace WingetUpgrade
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Auto-scroll virtualized terminal log in sidebar to bottom on new output
            _viewModel.RequestScrollConsoleToBottom = () =>
            {
                Dispatcher.InvokeAsync(() =>
                {
                    if (SidebarConsoleListBox != null && SidebarConsoleListBox.Items.Count > 0)
                    {
                        SidebarConsoleListBox.ScrollIntoView(SidebarConsoleListBox.Items[^1]);
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            };

            // Full log window handler
            _viewModel.OpenFullLogAction = logText =>
            {
                var fullLogWindow = new FullLogWindow(logText)
                {
                    Owner = this
                };
                fullLogWindow.ShowDialog();
            };

            // Defer initial update scan until after the UI finishes rendering its first frame
            Loaded += async (s, e) =>
            {
                await _viewModel.InitializeOnLoadedAsync();
            };

            StateChanged += MainWindow_StateChanged;
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                if (MaximizeGlyph != null) MaximizeGlyph.Text = "\u29C9"; // Two overlapping squares restore icon
                RootWindowBorder.Margin = new Thickness(7); // Prevent 6-8px window overflow on borderless maximized window
            }
            else
            {
                if (MaximizeGlyph != null) MaximizeGlyph.Text = "\u25A2"; // Single square maximize icon
                RootWindowBorder.Margin = new Thickness(0);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { DragMove(); } catch { }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PackageRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If user clicked directly on the CheckBox or More button, let them handle it naturally
            if (e.OriginalSource is DependencyObject dep)
            {
                var current = dep;
                while (current != null && current != sender)
                {
                    if (current is Button || current is CheckBox || current is MenuItem)
                    {
                        return;
                    }
                    current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                }
            }

            if (sender is FrameworkElement fe && fe.DataContext is PackageItem item)
            {
                item.IsSelected = !item.IsSelected;
                e.Handled = true;
            }
        }

        private void PackageMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.DataContext = btn.DataContext;
                btn.ContextMenu.Tag = this.DataContext;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void PackageContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
            {
                if (menu.PlacementTarget is FrameworkElement fe)
                {
                    menu.DataContext = fe.DataContext;
                }
                menu.Tag = this.DataContext;
            }
        }

        private void UpgradeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && DataContext is MainViewModel vm)
            {
                var targetPkg = (mi.DataContext as PackageItem) ?? (mi.CommandParameter as PackageItem);
                if (targetPkg != null && vm.UpgradeSingleCommand.CanExecute(targetPkg))
                {
                    vm.UpgradeSingleCommand.Execute(targetPkg);
                }
            }
        }

        private void IgnoreMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && DataContext is MainViewModel vm)
            {
                var targetPkg = (mi.DataContext as PackageItem) ?? (mi.CommandParameter as PackageItem);
                if (targetPkg != null && vm.IgnorePackageCommand.CanExecute(targetPkg))
                {
                    vm.IgnorePackageCommand.Execute(targetPkg);
                }
            }
        }

        private void CopyIdMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && DataContext is MainViewModel vm)
            {
                var targetPkg = (mi.DataContext as PackageItem) ?? (mi.CommandParameter as PackageItem);
                if (targetPkg != null && vm.CopyPackageIdCommand.CanExecute(targetPkg))
                {
                    vm.CopyPackageIdCommand.Execute(targetPkg);
                }
            }
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/microsoft/winget-cli",
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void HeartButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Winget Upgrade • Modern GUI\n\nHigh-performance, hardware-accelerated desktop client for Windows Package Manager.\nBuilt with .NET 8 & WPF.",
                "About Winget Upgrade",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
