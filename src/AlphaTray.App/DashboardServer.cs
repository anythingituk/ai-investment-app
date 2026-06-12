using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AlphaTray.App;

internal sealed class DashboardServer : IAsyncDisposable
{
    private const int PreferredPort = 48720;
    private readonly AppState appState;
    private WebApplication? app;

    public DashboardServer(AppState appState)
    {
        this.appState = appState;
    }

    public Uri? DashboardUri { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var port = FindAvailablePort(PreferredPort);
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(DashboardServer).Assembly.FullName,
            Args = Array.Empty<string>()
        });

        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

        app = builder.Build();
        app.MapGet("/", Dashboard);
        app.MapGet("/settings", Settings);
        app.MapGet("/api/status", () => Results.Json(appState.Snapshot()));
        app.MapPost("/api/pause", () =>
        {
            appState.PauseAi();
            return Results.Json(appState.Snapshot());
        });
        app.MapPost("/api/resume", () =>
        {
            appState.ResumeAi();
            return Results.Json(appState.Snapshot());
        });
        app.MapPost("/api/run-scan", (HttpContext context) =>
        {
            _ = appState.RunScanAsync(CancellationToken.None);
            return Results.Json(appState.Snapshot());
        });

        await app.StartAsync(cancellationToken);

        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()?
            .Addresses
            .FirstOrDefault();

        DashboardUri = new Uri(address ?? $"http://127.0.0.1:{port}");
    }

    public void OpenDashboard()
    {
        if (DashboardUri is null)
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = DashboardUri.ToString(),
            UseShellExecute = true
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (app is null)
        {
            return;
        }

        await app.StopAsync(TimeSpan.FromSeconds(5));
        await app.DisposeAsync();
    }

    private static IResult Dashboard()
    {
        return Results.Content(DashboardHtml("Dashboard"), "text/html", Encoding.UTF8);
    }

    private static IResult Settings()
    {
        return Results.Content(DashboardHtml("Settings"), "text/html", Encoding.UTF8);
    }

    private static int FindAvailablePort(int preferredPort)
    {
        for (var port = preferredPort; port < preferredPort + 20; port++)
        {
            if (IsPortAvailable(port))
            {
                return port;
            }
        }

        throw new InvalidOperationException("No local dashboard port is available.");
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string DashboardHtml(string activePage)
    {
        var isSettings = activePage == "Settings";
        return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>AlphaTray {{activePage}}</title>
  <style>
    :root {
      color-scheme: light;
      font-family: "Segoe UI", system-ui, sans-serif;
      --ink: #172033;
      --muted: #607089;
      --line: #d8dee9;
      --panel: #ffffff;
      --page: #f4f7fb;
      --accent: #2f6fed;
      --warn: #b54708;
      --danger: #b42318;
      --ok: #067647;
    }
    * { box-sizing: border-box; }
    body { margin: 0; background: var(--page); color: var(--ink); }
    header { display: flex; align-items: center; justify-content: space-between; padding: 16px 24px; border-bottom: 1px solid var(--line); background: var(--panel); }
    h1 { margin: 0; font-size: 20px; }
    nav { display: flex; gap: 8px; }
    nav a, button { border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--ink); padding: 8px 12px; text-decoration: none; font: inherit; cursor: pointer; }
    button.primary { background: var(--accent); color: #fff; border-color: var(--accent); }
    main { max-width: 1180px; margin: 0 auto; padding: 24px; display: grid; gap: 16px; }
    .grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }
    .panel { background: var(--panel); border: 1px solid var(--line); border-radius: 8px; padding: 16px; }
    .panel h2 { margin: 0 0 12px; font-size: 15px; }
    .value { font-size: 24px; font-weight: 650; margin-bottom: 6px; }
    .muted { color: var(--muted); font-size: 13px; }
    .warning { color: var(--warn); font-weight: 600; }
    .danger { color: var(--danger); font-weight: 600; }
    .ok { color: var(--ok); font-weight: 600; }
    table { width: 100%; border-collapse: collapse; }
    th, td { padding: 10px 8px; border-bottom: 1px solid var(--line); text-align: left; font-size: 14px; }
    th { color: var(--muted); font-weight: 600; }
    .actions { display: flex; gap: 8px; flex-wrap: wrap; }
    .wide { grid-column: 1 / -1; }
    @media (max-width: 800px) {
      header { align-items: flex-start; flex-direction: column; gap: 12px; }
      .grid { grid-template-columns: 1fr; }
    }
  </style>
</head>
<body>
  <header>
    <h1>AlphaTray</h1>
    <nav>
      <a href="/">Dashboard</a>
      <a href="/settings">Settings</a>
    </nav>
  </header>
  <main>
    {{(isSettings ? SettingsBody() : DashboardBody())}}
  </main>
  <script>
    async function post(path) {
      await fetch(path, { method: 'POST' });
      await refresh();
    }
    function fmt(value) {
      return value ? new Date(value).toLocaleString() : 'Not yet';
    }
    async function refresh() {
      const response = await fetch('/api/status');
      const state = await response.json();
      document.querySelectorAll('[data-ai-status]').forEach(el => {
        el.textContent = state.isScanRunning ? 'Running' : (state.isAiPaused ? 'Paused' : 'Active');
        el.className = state.isAiPaused ? 'value warning' : 'value ok';
      });
      document.querySelectorAll('[data-status-message]').forEach(el => el.textContent = state.statusMessage);
      document.querySelectorAll('[data-last-scan]').forEach(el => el.textContent = fmt(state.lastScanFinishedAtUtc));
      document.querySelectorAll('[data-scan-button]').forEach(el => el.disabled = state.isScanRunning);
    }
    setInterval(refresh, 3000);
    refresh();
  </script>
</body>
</html>
""";
    }

    private static string DashboardBody()
    {
        return """
<section class="grid">
  <article class="panel">
    <h2>AI Status</h2>
    <div class="value ok" data-ai-status>Active</div>
    <div class="muted" data-status-message>Loading status...</div>
  </article>
  <article class="panel">
    <h2>Portfolio/Cash</h2>
    <div class="value">GBP 10,000</div>
    <div class="muted">Planning placeholder, not connected to brokerage data.</div>
  </article>
  <article class="panel">
    <h2>Last Scan</h2>
    <div class="value" data-last-scan>Not yet</div>
    <div class="muted">Manual scan stub for shell validation.</div>
  </article>
  <article class="panel">
    <h2>AI Usage</h2>
    <div class="value">GBP 0.00</div>
    <div class="muted">Cost logging comes with provider integration.</div>
  </article>
</section>
<section class="panel wide">
  <h2>Controls</h2>
  <div class="actions">
    <button class="primary" data-scan-button onclick="post('/api/run-scan')">Run Scan Now</button>
    <button onclick="post('/api/pause')">Pause AI</button>
    <button onclick="post('/api/resume')">Resume AI</button>
  </div>
</section>
<section class="panel wide">
  <h2>GBP 1-10 Share Candidates</h2>
  <table>
    <thead><tr><th>Ticker</th><th>Market</th><th>Price</th><th>Score</th><th>Risk</th><th>Status</th></tr></thead>
    <tbody>
      <tr><td>Example PLC</td><td>UK</td><td>GBP 4.20</td><td>72</td><td>Medium</td><td>Needs human review</td></tr>
      <tr><td>Sample Inc</td><td>US</td><td>USD 7.80</td><td>68</td><td>High</td><td>Research queue</td></tr>
      <tr><td>Short Watch Co</td><td>US</td><td>USD 9.10</td><td>61</td><td><span class="danger">Short-watch warning</span></td><td>Risk rules required</td></tr>
    </tbody>
  </table>
</section>
<section class="grid">
  <article class="panel">
    <h2>Research Queue</h2>
    <div class="value">0</div>
    <div class="muted">No persisted queue yet.</div>
  </article>
  <article class="panel">
    <h2>Sector/Index Summary</h2>
    <div class="value">Pending</div>
    <div class="muted">Market data provider not configured.</div>
  </article>
  <article class="panel">
    <h2>Recent Alerts</h2>
    <div class="value">0</div>
    <div class="muted">Alert jobs are not scheduled yet.</div>
  </article>
  <article class="panel">
    <h2>Shorting Controls</h2>
    <div class="value warning">Separated</div>
    <div class="muted">Short-watch ideas require warning and exit rules.</div>
  </article>
</section>
""";
    }

    private static string SettingsBody()
    {
        return """
<section class="panel wide">
  <h2>Settings</h2>
  <table>
    <tbody>
      <tr><th>Dashboard bind address</th><td>127.0.0.1 only</td></tr>
      <tr><th>AI mode</th><td>Lean</td></tr>
      <tr><th>Monthly AI budget</th><td>Not configured</td></tr>
      <tr><th>Market data provider</th><td>Not configured</td></tr>
      <tr><th>Storage</th><td>SQLite planned for next data slice</td></tr>
    </tbody>
  </table>
</section>
<section class="panel wide">
  <h2>Background Activity</h2>
  <div class="actions">
    <button onclick="post('/api/pause')">Pause AI</button>
    <button onclick="post('/api/resume')">Resume AI</button>
  </div>
  <p class="muted" data-status-message>Loading status...</p>
</section>
""";
    }
}
