namespace AlphaTray.App;

internal sealed class TrayAppContext : ApplicationContext
{
    private readonly AppDatabase database;
    private readonly AppState appState;
    private readonly CancellationTokenSource shutdownTokenSource = new();
    private readonly DashboardServer dashboardServer;
    private readonly NotifyIcon notifyIcon;
    private readonly ToolStripMenuItem pauseAiItem;
    private readonly ToolStripMenuItem resumeAiItem;
    private readonly ToolStripMenuItem runScanItem;
    private Icon? activeIcon;
    private Icon? pausedIcon;
    private Icon? scanningIcon;

    public TrayAppContext()
    {
        database = new AppDatabase(GetDatabasePath());
        database.Initialize();
        appState = new AppState(database);
        dashboardServer = new DashboardServer(appState);

        activeIcon = LoadIcon("default_purple.ico") ?? SystemIcons.Application;
        pausedIcon = LoadIcon("paused_grey.ico") ?? activeIcon;
        scanningIcon = LoadIcon("researching_orange.ico") ?? activeIcon;

        var openDashboardItem = new ToolStripMenuItem("Open Dashboard", null, (_, _) => dashboardServer.OpenDashboard());
        runScanItem = new ToolStripMenuItem("Run Scan Now", null, async (_, _) => await RunScanFromTrayAsync());
        pauseAiItem = new ToolStripMenuItem("Pause AI", null, (_, _) => appState.PauseAi());
        resumeAiItem = new ToolStripMenuItem("Resume AI", null, (_, _) => appState.ResumeAi());
        var settingsItem = new ToolStripMenuItem("Settings", null, (_, _) => OpenSettings());
        var exitItem = new ToolStripMenuItem("Exit", null, async (_, _) => await ExitAsync());

        notifyIcon = new NotifyIcon
        {
            Icon = activeIcon,
            Text = "AlphaTray starting...",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };

        notifyIcon.ContextMenuStrip.Items.Add(openDashboardItem);
        notifyIcon.ContextMenuStrip.Items.Add(runScanItem);
        notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        notifyIcon.ContextMenuStrip.Items.Add(pauseAiItem);
        notifyIcon.ContextMenuStrip.Items.Add(resumeAiItem);
        notifyIcon.ContextMenuStrip.Items.Add(settingsItem);
        notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        notifyIcon.ContextMenuStrip.Items.Add(exitItem);
        notifyIcon.DoubleClick += (_, _) => dashboardServer.OpenDashboard();

        appState.Changed += (_, _) => UpdateTrayState();
        UpdateTrayState();
        _ = StartDashboardAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            activeIcon?.Dispose();
            pausedIcon?.Dispose();
            scanningIcon?.Dispose();
            shutdownTokenSource.Dispose();
        }

        base.Dispose(disposing);
    }

    private async Task StartDashboardAsync()
    {
        try
        {
            await dashboardServer.StartAsync(shutdownTokenSource.Token);
            notifyIcon.Text = "AlphaTray active";
        }
        catch (Exception ex)
        {
            notifyIcon.Text = "AlphaTray dashboard failed";
            MessageBox.Show(
                $"The local dashboard could not be started.{Environment.NewLine}{ex.Message}",
                "AlphaTray",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private async Task RunScanFromTrayAsync()
    {
        runScanItem.Enabled = false;
        try
        {
            await appState.RunScanAsync(shutdownTokenSource.Token);
        }
        finally
        {
            runScanItem.Enabled = true;
        }
    }

    private void OpenSettings()
    {
        if (dashboardServer.DashboardUri is null)
        {
            return;
        }

        var settingsUri = new Uri(dashboardServer.DashboardUri, "/settings");
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = settingsUri.ToString(),
            UseShellExecute = true
        });
    }

    private async Task ExitAsync()
    {
        shutdownTokenSource.Cancel();
        notifyIcon.Visible = false;
        await dashboardServer.DisposeAsync();
        ExitThread();
    }

    private void UpdateTrayState()
    {
        var state = appState.Snapshot();
        notifyIcon.Icon = state.IsScanRunning ? scanningIcon : state.IsAiPaused ? pausedIcon : activeIcon;
        notifyIcon.Text = state.IsScanRunning
            ? "AlphaTray running scan"
            : state.IsAiPaused
                ? "AlphaTray paused"
                : "AlphaTray active";

        pauseAiItem.Enabled = !state.IsAiPaused;
        resumeAiItem.Enabled = state.IsAiPaused;
        runScanItem.Enabled = !state.IsScanRunning;
    }

    private static Icon? LoadIcon(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
        return File.Exists(path) ? new Icon(path) : null;
    }

    private static string GetDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "AlphaTray", "alphatray.db");
    }
}
