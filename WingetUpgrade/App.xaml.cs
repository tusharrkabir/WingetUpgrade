using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows;

namespace WingetUpgrade;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Ensure application runs elevated as Administrator for seamless silent installs
        if (!IsAdministrator())
        {
            try
            {
                var exePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                string args = string.Join(" ", e.Args);

                if (!string.IsNullOrEmpty(exePath))
                {
                    if (exePath.EndsWith("dotnet.exe", StringComparison.OrdinalIgnoreCase))
                    {
                        string dllPath = typeof(App).Assembly.Location;
                        args = $"\"{dllPath}\" {args}".Trim();
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = args,
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    Process.Start(psi);
                    Shutdown();
                    return;
                }
            }
            catch
            {
                // If user declines UAC, proceed in current non-elevated context
            }
        }

        base.OnStartup(e);
    }

    private static bool IsAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }
}


