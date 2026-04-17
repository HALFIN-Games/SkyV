using System;
using System.Threading;
using System.Windows;

namespace SkyV.Launcher;

public partial class App : Application
{
    private const string InstanceId = "HALFIN.VokunWL";
    private Mutex? instanceMutex;
    private SingleInstancePipe? instancePipe;

    protected override void OnStartup(StartupEventArgs e)
    {
        instanceMutex = new Mutex(true, @"Local\" + InstanceId, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SingleInstancePipe.TrySend(InstanceId, e.Args);
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var intent = JoinIntent.TryParse(e.Args);
        var mainWindow = new MainWindow(intent);
        MainWindow = mainWindow;
        mainWindow.Show();

        instancePipe = new SingleInstancePipe(InstanceId, args =>
        {
            Dispatcher.Invoke(() =>
            {
                var parsed = JoinIntent.TryParse(args);
                if (parsed is not null) mainWindow.ApplyJoinIntent(parsed);
            });
        });
        instancePipe.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        instancePipe?.Dispose();
        instancePipe = null;

        if (instanceMutex is not null)
        {
            try { instanceMutex.ReleaseMutex(); } catch { }
            instanceMutex.Dispose();
            instanceMutex = null;
        }

        base.OnExit(e);
    }
}

