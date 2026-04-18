using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SkyV.Installer;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly InstallerConfig config;
    private readonly StringBuilder log = new();
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(10) };
    private CancellationTokenSource? cts;

    public MainWindow(InstallerConfig config)
    {
        this.config = config;
        InitializeComponent();
        DataContext = this;

        TitleText = "Ready";
        BodyText = "This installs Vokun WL for testers. It will install the signing certificate and then install/update the launcher.";
        StatusText = "Click Install / Update to begin.";
        PrimaryButtonText = "Install / Update";
        IsPrimaryEnabled = true;

        DownloadSummary =
            $"MSIX: {config.MsixUrl}\n" +
            $"CERT: {config.CerUrl}";

        Loaded += (_, _) =>
        {
            if (config.AutoStart) Dispatcher.BeginInvoke(StartAsync, DispatcherPriority.Background);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private string titleText = "";
    public string TitleText
    {
        get => titleText;
        set { titleText = value; OnPropertyChanged(nameof(TitleText)); }
    }

    private string bodyText = "";
    public string BodyText
    {
        get => bodyText;
        set { bodyText = value; OnPropertyChanged(nameof(BodyText)); }
    }

    private string downloadSummary = "";
    public string DownloadSummary
    {
        get => downloadSummary;
        set { downloadSummary = value; OnPropertyChanged(nameof(DownloadSummary)); }
    }

    private string statusText = "";
    public string StatusText
    {
        get => statusText;
        set { statusText = value; OnPropertyChanged(nameof(StatusText)); }
    }

    private string primaryButtonText = "";
    public string PrimaryButtonText
    {
        get => primaryButtonText;
        set { primaryButtonText = value; OnPropertyChanged(nameof(PrimaryButtonText)); }
    }

    private bool isPrimaryEnabled;
    public bool IsPrimaryEnabled
    {
        get => isPrimaryEnabled;
        set
        {
            isPrimaryEnabled = value;
            OnPropertyChanged(nameof(IsPrimaryEnabled));
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    private bool isIndeterminate;
    public bool IsIndeterminate
    {
        get => isIndeterminate;
        set { isIndeterminate = value; OnPropertyChanged(nameof(IsIndeterminate)); }
    }

    private double progressValue;
    public double ProgressValue
    {
        get => progressValue;
        set { progressValue = value; OnPropertyChanged(nameof(ProgressValue)); }
    }

    private string logText = "";
    public string LogText
    {
        get => logText;
        set { logText = value; OnPropertyChanged(nameof(LogText)); }
    }

    public bool IsNotBusy => IsPrimaryEnabled;

    private void AppendLog(string line)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => AppendLog(line));
            return;
        }

        log.AppendLine(line);
        LogText = log.ToString();
    }

    private static bool IsAdmin()
    {
        var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void RestartElevatedAndExit(bool autoStart)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(exePath)) throw new Exception("Could not locate installer executable path.");

        var args = config.RebuildArgs(autoStart: autoStart);
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = true,
            Verb = "runas",
        };
        Process.Start(psi);
        Close();
    }

    private async void OnPrimaryClicked(object sender, RoutedEventArgs e)
    {
        await StartAsync();
    }

    private async Task StartAsync()
    {
        if (!IsPrimaryEnabled) return;

        if (!IsAdmin())
        {
            try
            {
                RestartElevatedAndExit(autoStart: true);
                return;
            }
            catch (Exception ex)
            {
                TitleText = "Administrator required";
                StatusText = "Windows needs admin permission to install the certificate and machine-wide package.";
                PrimaryButtonText = "Install / Update";
                BodyText = ex.Message;
                return;
            }
        }

        PrimaryButtonText = "Installing...";
        IsPrimaryEnabled = false;
        IsIndeterminate = false;
        ProgressValue = 0;

        cts?.Cancel();
        cts = new CancellationTokenSource();
        var ct = cts.Token;

        try
        {
            TitleText = "Installing";
            BodyText = "Downloading the latest files, installing the certificate, then installing/updating Vokun WL.";

            var workDir = PrepareWorkDir();
            AppendLog($"Work dir: {workDir}");

            var cerPath = Path.Combine(workDir, "VokunWL_TestCert.cer");
            var msixPath = Path.Combine(workDir, "VokunWL.msix");

            StatusText = "Downloading certificate...";
            await DownloadAsync(config.CerUrl, cerPath, 0, 10, ct);

            StatusText = "Downloading launcher package...";
            await DownloadAsync(config.MsixUrl, msixPath, 10, 70, ct);

            StatusText = "Installing certificate...";
            InstallCert(cerPath);
            ProgressValue = 80;

            StatusText = "Installing package...";
            IsIndeterminate = true;
            await InstallMsixAsync(msixPath, ct);
            IsIndeterminate = false;
            ProgressValue = 100;

            TitleText = "Installed";
            StatusText = "Done. You can close this window.";
            PrimaryButtonText = "Re-run (update)";
            BodyText = "If you release a new version later, re-run this installer to update.";
        }
        catch (Exception ex)
        {
            TitleText = "Failed";
            StatusText = ex.Message;
            PrimaryButtonText = "Try again";
            AppendLog("");
            AppendLog("ERROR:");
            AppendLog(ex.ToString());
        }
        finally
        {
            IsPrimaryEnabled = true;
        }
    }

    private static string PrepareWorkDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VokunWLInstaller");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private async Task DownloadAsync(string url, string outPath, double startPct, double endPct, CancellationToken ct)
    {
        AppendLog($"GET {url}");

        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength;

        await using var input = await resp.Content.ReadAsStreamAsync(ct);
        await using var output = new FileStream(outPath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 128, useAsync: true);

        var buffer = new byte[1024 * 128];
        long readTotal = 0;
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, read), ct);
            readTotal += read;

            if (total.HasValue && total.Value > 0)
            {
                var frac = Math.Clamp((double)readTotal / total.Value, 0, 1);
                ProgressValue = startPct + ((endPct - startPct) * frac);
            }
        }

        AppendLog($"Saved: {outPath} ({readTotal} bytes)");
    }

    private void InstallCert(string cerPath)
    {
        if (!File.Exists(cerPath)) throw new FileNotFoundException("Certificate file missing", cerPath);

        var cert = new X509Certificate2(cerPath);
        AppendLog($"Cert subject: {cert.Subject}");
        AppendLog($"Cert thumbprint: {cert.Thumbprint}");

        using var store = new X509Store(StoreName.TrustedPeople, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        foreach (var existing in store.Certificates)
        {
            if (string.Equals(existing.Thumbprint, cert.Thumbprint, StringComparison.OrdinalIgnoreCase))
            {
                AppendLog("Certificate already installed.");
                return;
            }
        }

        store.Add(cert);
        AppendLog("Certificate installed to LocalMachine\\TrustedPeople.");
    }

    private async Task InstallMsixAsync(string msixPath, CancellationToken ct)
    {
        if (!File.Exists(msixPath)) throw new FileNotFoundException("MSIX file missing", msixPath);

        var ps1 = Path.Combine(PrepareWorkDir(), "install_msix.ps1");
        var script =
            "$ErrorActionPreference = 'Stop'\n" +
            "$msix = $args[0]\n" +
            "if (-not (Test-Path $msix)) { throw \"MSIX not found: $msix\" }\n" +
            "$existingUser = Get-AppxPackage -Name 'HALFIN.VokunWL' -ErrorAction SilentlyContinue\n" +
            "if ($existingUser) { Remove-AppxPackage -Package $existingUser.PackageFullName -ErrorAction SilentlyContinue | Out-Null }\n" +
            "$existingProv = Get-AppxProvisionedPackage -Online | Where-Object { $_.PackageName -like 'HALFIN.VokunWL_*' }\n" +
            "foreach ($p in $existingProv) { try { Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName -ErrorAction Stop | Out-Null } catch { } }\n" +
            "$provisionOk = $true\n" +
            "try { Add-AppxProvisionedPackage -Online -PackagePath $msix -SkipLicense -ErrorAction Stop | Out-Null } catch { $provisionOk = $false; Write-Output (\"Provisioning failed: \" + $_.Exception.Message) }\n" +
            "if (-not $provisionOk) { Write-Output \"Continuing with per-user install.\" }\n" +
            "Add-AppxPackage -Path $msix -ForceApplicationShutdown | Out-Null\n";
        File.WriteAllText(ps1, script, new UTF8Encoding(false));

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{ps1}\" \"{msixPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var p = Process.Start(psi);
        if (p is null) throw new Exception("Failed to start PowerShell for MSIX install.");

        var stdOut = Task.Run(async () =>
        {
            while (!p.StandardOutput.EndOfStream)
            {
                var line = await p.StandardOutput.ReadLineAsync(ct);
                if (!string.IsNullOrWhiteSpace(line)) AppendLog(line);
            }
        }, ct);

        var stdErr = Task.Run(async () =>
        {
            while (!p.StandardError.EndOfStream)
            {
                var line = await p.StandardError.ReadLineAsync(ct);
                if (!string.IsNullOrWhiteSpace(line)) AppendLog(line);
            }
        }, ct);

        await Task.WhenAll(stdOut, stdErr, p.WaitForExitAsync(ct));

        if (p.ExitCode != 0) throw new Exception($"MSIX install failed (exit code {p.ExitCode}).");
        AppendLog("MSIX install completed.");
    }

    private void OnOpenReleaseClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl(config.ReleasePageUrl);
    }

    private void OnCopyLogsClicked(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(LogText ?? "");
        MessageBox.Show(this, "Copied installer logs.", "Vokun WL", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
