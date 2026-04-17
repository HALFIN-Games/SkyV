using System.Linq;
using System.Windows;

namespace SkyV.Installer;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var config = InstallerConfig.FromArgs(e.Args);
        var w = new MainWindow(config);
        MainWindow = w;
        w.Show();
    }
}

