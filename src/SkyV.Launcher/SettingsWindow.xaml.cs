using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace SkyV.Launcher;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private readonly LauncherSettings settings;

    public SettingsWindow(LauncherSettings current)
    {
        InitializeComponent();

        settings = new LauncherSettings
        {
            WebsiteBaseUrl = current.WebsiteBaseUrl,
            QueueBaseUrl = current.QueueBaseUrl,
            PackUrl = current.PackUrl,
            SkyrimInstallPath = current.SkyrimInstallPath,
        };

        DataContext = this;
        StatusText = $"Settings file:\n{LauncherSettings.GetSettingsPath()}";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WebsiteBaseUrl
    {
        get => settings.WebsiteBaseUrl;
        set { settings.WebsiteBaseUrl = value; OnPropertyChanged(); }
    }

    public string QueueBaseUrl
    {
        get => settings.QueueBaseUrl;
        set { settings.QueueBaseUrl = value; OnPropertyChanged(); }
    }

    public string PackUrl
    {
        get => settings.PackUrl;
        set { settings.PackUrl = value; OnPropertyChanged(); }
    }

    public string? SkyrimInstallPath
    {
        get => settings.SkyrimInstallPath;
        set { settings.SkyrimInstallPath = value; OnPropertyChanged(); }
    }

    private string statusText = "";
    public string StatusText
    {
        get => statusText;
        set { statusText = value; OnPropertyChanged(); }
    }

    private async void OnTestWebsite(object sender, RoutedEventArgs e)
    {
        await TestUrlAsync(WebsiteBaseUrl, "/");
    }

    private async void OnTestQueue(object sender, RoutedEventArgs e)
    {
        await TestUrlAsync(QueueBaseUrl, "/healthz");
    }

    private async void OnTestPack(object sender, RoutedEventArgs e)
    {
        await TestUrlAsync(PackUrl, "");
    }

    private async Task TestUrlAsync(string baseUrl, string path)
    {
        try
        {
            StatusText = $"Testing: {baseUrl}{path}";
            var url = baseUrl.TrimEnd('/') + path;
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            using var req = new HttpRequestMessage(url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? HttpMethod.Head : HttpMethod.Get, url);
            req.Headers.TryAddWithoutValidation("User-Agent", "SkyV.Launcher");
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            StatusText = $"OK ({(int)resp.StatusCode})\n{url}";
        }
        catch (Exception ex)
        {
            StatusText = $"Failed\n{baseUrl}\n{ex.Message}";
        }
    }

    private void OnBrowseSkyrim(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select SkyrimSE.exe or skse64_loader.exe",
            Filter = "Skyrim/Skse executable|SkyrimSE.exe;skse64_loader.exe|Executable (*.exe)|*.exe",
            CheckFileExists = true,
        };

        var ok = dlg.ShowDialog(this);
        if (ok != true) return;

        var dir = Path.GetDirectoryName(dlg.FileName);
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir)) return;

        SkyrimInstallPath = dir;
        StatusText = $"Skyrim folder set:\n{dir}";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        settings.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
