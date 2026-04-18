using System;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Net;
using System.Net.Http;
using System.IO;

namespace SkyV.Launcher;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private JoinIntent? intent;
    private LauncherSettings settings;
    private WebsiteApiClient website;
    private QueueApiClient queueApi;
    private string? ticket;
    private bool inQueue;
    private bool admitted;
    private CancellationTokenSource? queueCts;
    private string? queueId;
    private string queueStatusNote = "";
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromMinutes(20) };

    public MainWindow(JoinIntent? intent)
    {
        this.intent = intent;
        settings = LauncherSettings.Load();
        website = new WebsiteApiClient(settings.WebsiteBaseUrl);
        queueApi = new QueueApiClient(settings.QueueBaseUrl);
        InitializeComponent();
        DataContext = this;
        RefreshText();
    }

    public void ApplyJoinIntent(JoinIntent? newIntent)
    {
        if (newIntent is null) return;

        intent = newIntent;
        ticket = null;
        inQueue = false;
        admitted = false;
        queueId = null;
        queueStatusNote = "";
        QueuePosition = 0;
        queueCts?.Cancel();
        queueCts = null;

        RefreshText();

        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Subtitle => "Windows launcher (v0)";

    private string titleText = "";
    public string TitleText
    {
        get => titleText;
        set { titleText = value; OnPropertyChanged(); }
    }

    private string bodyText = "";
    public string BodyText
    {
        get => bodyText;
        set { bodyText = value; OnPropertyChanged(); }
    }

    private string intentSummary = "";
    public string IntentSummary
    {
        get => intentSummary;
        set { intentSummary = value; OnPropertyChanged(); }
    }

    private string primaryButtonText = "";
    public string PrimaryButtonText
    {
        get => primaryButtonText;
        set { primaryButtonText = value; OnPropertyChanged(); }
    }

    private bool isPrimaryEnabled = true;
    public bool IsPrimaryEnabled
    {
        get => isPrimaryEnabled;
        set { isPrimaryEnabled = value; OnPropertyChanged(); }
    }

    private bool isLaunchEnabled;
    public bool IsLaunchEnabled
    {
        get => isLaunchEnabled;
        set { isLaunchEnabled = value; OnPropertyChanged(); }
    }

    private int queuePosition;
    public int QueuePosition
    {
        get => queuePosition;
        set { queuePosition = value; OnPropertyChanged(); }
    }

    private void RefreshText()
    {
        if (intent is null)
        {
            TitleText = "Ready to join";
            BodyText =
                "This launcher opens when you click Join Server on the website. " +
                "If you arrived here manually, click Open Website and then press Join Server.";
            IntentSummary = "No skyv:// join link received yet.";
            PrimaryButtonText = "Open website";
            IsLaunchEnabled = false;
            return;
        }

        if (!inQueue)
        {
            TitleText = "Join request received";
            BodyText =
                "The website sent a one-time join code to the launcher. " +
                "Next step is to exchange the code for a short-lived join ticket, then enter the queue.";
        }
        else if (!admitted)
        {
            TitleText = "In queue";
            BodyText = $"Waiting for admission. Current position: {QueuePosition}.";
        }
        else
        {
            TitleText = "Admitted";
            BodyText = "You are admitted. Next step is to verify required files and launch the game.";
        }

        IntentSummary =
            $"Server: {intent.ServerId}\n" +
            $"Code: {intent.Code}\n" +
            $"Nonce: {intent.Nonce ?? "(none)"}\n" +
            $"Ticket: {(ticket is null ? "(not fetched yet)" : "fetched")}\n" +
            $"Queue: {(inQueue ? (admitted ? "admitted" : "waiting") : "(not started)")}" +
            (string.IsNullOrWhiteSpace(queueStatusNote) ? "" : $"\nQueue note: {queueStatusNote}");

        if (ticket is null)
        {
            PrimaryButtonText = "Get join ticket";
        }
        else if (!inQueue)
        {
            PrimaryButtonText = "Enter queue";
        }
        else
        {
            PrimaryButtonText = admitted ? "Ready" : "Cancel queue";
        }

        IsLaunchEnabled = admitted;
    }

    private void OnOpenRulesClicked(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://www.vokunrp.com/rules");
    }

    private void OnOpenSettingsClicked(object sender, RoutedEventArgs e)
    {
        var w = new SettingsWindow(settings);
        w.Owner = this;
        var ok = w.ShowDialog();
        if (ok == true)
        {
            settings = LauncherSettings.Load();
            website = new WebsiteApiClient(settings.WebsiteBaseUrl);
            queueApi = new QueueApiClient(settings.QueueBaseUrl);
            queueStatusNote = "";
            RefreshText();
        }
    }

    private async void OnPrimaryActionClicked(object sender, RoutedEventArgs e)
    {
        if (intent is null)
        {
            OpenUrl("https://www.vokunrp.com/join");
            return;
        }

        if (ticket is null)
        {
            IsPrimaryEnabled = false;
            try
            {
                TitleText = "Exchanging join code...";
                BodyText = "Contacting vokunrp.com to exchange the one-time code for a join ticket.";

                var resp = await website.ExchangeCodeAsync(intent.ServerId, intent.Code, CancellationToken.None);
                ticket = resp.Ticket;

                TitleText = "Ticket received";
                BodyText =
                    "Join ticket was received. Next step is queue UI + admission gating, then launching the game only when admitted.";
                RefreshText();
            }
            catch (WebsiteApiException wex)
            {
                TitleText = "Join failed";
                BodyText = MapWebsiteExchangeError(wex);
                MessageBox.Show(this, $"{(int)wex.StatusCode} {wex.StatusCode}\n{wex.Message}", "SkyV", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                TitleText = "Join failed";
                BodyText = "The launcher could not exchange the join code. Use Copy diagnostics and check website status.";
                MessageBox.Show(this, ex.Message, "SkyV", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsPrimaryEnabled = true;
            }
            return;
        }

        if (!inQueue)
        {
            await StartQueueAsync();
            return;
        }

        if (!admitted)
        {
            CancelQueue();
            return;
        }

        MessageBox.Show(this, "You are admitted. Next step: pack verification + auto-detect Skyrim + SKSE verification.", "SkyV", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async Task StartQueueAsync()
    {
        if (ticket is null || intent is null) return;

        IsPrimaryEnabled = false;
        try
        {
            TitleText = "Enqueueing...";
            BodyText = "Contacting the queue service and entering the queue.";

            var resp = await queueApi.EnqueueAsync(ticket, intent.ServerId, CancellationToken.None);
            queueId = resp.QueueId;
            queueStatusNote = "";

            inQueue = true;
            admitted = string.Equals(resp.State, "admitted", StringComparison.OrdinalIgnoreCase);
            QueuePosition = resp.Position;

            queueCts?.Cancel();
            queueCts = new CancellationTokenSource();
            RefreshText();
            _ = PollQueueAsync(queueCts.Token);
        }
        catch (QueueApiException qex)
        {
            queueStatusNote = MapQueueError(qex);
            StartQueueStubFallback();
        }
        catch
        {
            queueStatusNote = "Queue service is unreachable. Using local fallback queue UI.";
            StartQueueStubFallback();
        }
        finally
        {
            IsPrimaryEnabled = true;
        }
    }

    private void CancelQueue()
    {
        _ = TryCancelRemoteQueueAsync();
        queueCts?.Cancel();
        queueCts = null;
        inQueue = false;
        admitted = false;
        queueId = null;
        QueuePosition = 0;
        RefreshText();
    }

    private void StartQueueStubFallback()
    {
        inQueue = true;
        admitted = false;
        queueId = null;
        QueuePosition = 10;
        queueCts?.Cancel();
        queueCts = new CancellationTokenSource();
        RefreshText();
        _ = RunQueueStubAsync(queueCts.Token);
    }

    private async Task PollQueueAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (ticket is null || queueId is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
                    continue;
                }

                var s = await queueApi.StatusAsync(ticket, queueId, ct);
                QueuePosition = s.Position;
                admitted = string.Equals(s.State, "admitted", StringComparison.OrdinalIgnoreCase);
                RefreshText();

                if (admitted) return;
                await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
            }
        }
        catch (QueueApiException qex)
        {
            queueStatusNote = MapQueueError(qex);
            StartQueueStubFallback();
        }
        catch
        {
            queueStatusNote = "Queue status refresh failed. Switched to local fallback queue UI.";
            StartQueueStubFallback();
        }
    }

    private async Task RunQueueStubAsync(CancellationToken ct)
    {
        try
        {
            while (QueuePosition > 1)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(TimeSpan.FromSeconds(1.2), ct);
                QueuePosition -= 1;
                RefreshText();
            }

            await Task.Delay(TimeSpan.FromSeconds(1.2), ct);
            admitted = true;
            QueuePosition = 0;
            RefreshText();
        }
        catch
        {
        }
    }

    private void OnLaunchSkseClicked(object sender, RoutedEventArgs e)
    {
        if (!admitted)
        {
            MessageBox.Show(this, "Launch is disabled until you are admitted.", "SkyV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            LaunchAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "SkyV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void LaunchAsync()
    {
        if (intent is null || ticket is null)
        {
            MessageBox.Show(this, "Missing join intent or ticket.", "SkyV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        IsPrimaryEnabled = false;
        try
        {
            var skyrimRoot = settings.SkyrimInstallPath;
            if (string.IsNullOrWhiteSpace(skyrimRoot))
            {
                skyrimRoot = SkyrimInstallLocator.TryFindSkyrimInstallPath();
            }

            if (string.IsNullOrWhiteSpace(skyrimRoot))
            {
                MessageBox.Show(this, "Skyrim install path was not found. Open Settings and set Skyrim Path.", "SkyV", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var skseLoader = System.IO.Path.Combine(skyrimRoot, "skse64_loader.exe");
            if (!File.Exists(skseLoader))
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Select skse64_loader.exe",
                    Filter = "SKSE Loader (skse64_loader.exe)|skse64_loader.exe|Executable (*.exe)|*.exe",
                    CheckFileExists = true,
                };

                var ok = dlg.ShowDialog(this);
                if (ok != true) return;
                skseLoader = dlg.FileName;
                skyrimRoot = Path.GetDirectoryName(skseLoader) ?? skyrimRoot;
            }

            TitleText = "Preparing files...";
            BodyText = "Installing required files and preparing your join ticket.";

            var pack = new VokunPackInstaller(http);
            await pack.EnsureInstalledAsync(skyrimRoot, settings.PackUrl, CancellationToken.None);

            JoinTicketHandoff.WriteTicket(skyrimRoot, ticket, intent.ServerId);

            TitleText = "Launching game...";
            BodyText = "Starting Skyrim via SKSE.";

            Process.Start(new ProcessStartInfo
            {
                FileName = skseLoader,
                WorkingDirectory = skyrimRoot,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            TitleText = "Launch failed";
            BodyText = "Could not prepare files or launch the game.";
            MessageBox.Show(this, ex.Message, "SkyV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsPrimaryEnabled = true;
        }
    }

    private void OnCopyDiagnosticsClicked(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SkyV Launcher diagnostics");
        sb.AppendLine($"Version: {typeof(MainWindow).Assembly.GetName().Version}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($".NET: {Environment.Version}");
        sb.AppendLine($"Args: {string.Join(" ", Environment.GetCommandLineArgs())}");
        if (intent is not null) sb.AppendLine($"Intent: {intent.RawUri}");

        Clipboard.SetText(sb.ToString());
        MessageBox.Show(this, "Copied diagnostics to clipboard.", "SkyV", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static string MapWebsiteExchangeError(WebsiteApiException ex)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Join request is invalid (bad server id or malformed payload). Please try joining again from the website.",
            HttpStatusCode.Unauthorized => "Join code is invalid. Please click Join Server again on the website.",
            HttpStatusCode.Forbidden => "Join is blocked (not whitelisted or code/server mismatch). Check your whitelist status on the website.",
            HttpStatusCode.Gone => "Join code expired or was already used. Click Join Server again to get a fresh code.",
            (HttpStatusCode)429 => "Too many attempts. Please wait a moment and try again.",
            HttpStatusCode.ServiceUnavailable => "Website could not validate roles right now. Please retry shortly.",
            HttpStatusCode.InternalServerError => "Website is currently misconfigured or failed to exchange code. Please retry shortly.",
            _ => "Join code exchange failed. Please retry from the website."
        };
    }

    private string MapQueueError(QueueApiException ex)
    {
        return ex.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Queue request is invalid (missing queue_id or invalid payload).",
            HttpStatusCode.Unauthorized => "Queue auth failed (ticket invalid/expired). Please rejoin from website.",
            HttpStatusCode.Forbidden => "Queue request is forbidden for this account/ticket.",
            HttpStatusCode.NotFound => "Queue entry no longer exists. Please rejoin from website.",
            HttpStatusCode.Conflict => "Queue state conflict (already used/invalid transition). Try again.",
            (HttpStatusCode)429 => "Queue service rate-limited this request. Please wait a moment.",
            HttpStatusCode.ServiceUnavailable => "Queue service temporarily unavailable.",
            HttpStatusCode.InternalServerError => "Queue service encountered an internal error.",
            _ => "Queue request failed. Using fallback queue UI."
        };
    }

    private async Task TryCancelRemoteQueueAsync()
    {
        try
        {
            if (ticket is null || queueId is null) return;
            await queueApi.CancelAsync(ticket, queueId, CancellationToken.None);
        }
        catch
        {
        }
    }
}
