namespace AlphaTray.App;

internal sealed class AppState
{
    private const string IsAiPausedSettingKey = "Ai.IsPaused";
    private readonly object syncRoot = new();
    private readonly AppDatabase database;

    public AppState(AppDatabase database)
    {
        this.database = database;
        IsAiPaused = database.GetBoolean(IsAiPausedSettingKey, defaultValue: false);
        StatusMessage = IsAiPaused
            ? "AI background activity is paused. Manual review remains available."
            : "AI background activity is active.";
    }

    public bool IsAiPaused { get; private set; }

    public bool IsScanRunning { get; private set; }

    public DateTimeOffset? LastScanStartedAtUtc { get; private set; }

    public DateTimeOffset? LastScanFinishedAtUtc { get; private set; }

    public string StatusMessage { get; private set; } = "AI background activity is active.";

    public event EventHandler? Changed;

    public AppStateSnapshot Snapshot()
    {
        lock (syncRoot)
        {
            return new AppStateSnapshot(
                IsAiPaused,
                IsScanRunning,
                LastScanStartedAtUtc,
                LastScanFinishedAtUtc,
                StatusMessage);
        }
    }

    public void PauseAi()
    {
        lock (syncRoot)
        {
            IsAiPaused = true;
            StatusMessage = "AI background activity is paused. Manual review remains available.";
        }

        database.SetBoolean(IsAiPausedSettingKey, true);
        OnChanged();
    }

    public void ResumeAi()
    {
        lock (syncRoot)
        {
            IsAiPaused = false;
            StatusMessage = "AI background activity is active.";
        }

        database.SetBoolean(IsAiPausedSettingKey, false);
        OnChanged();
    }

    public async Task RunScanAsync(CancellationToken cancellationToken)
    {
        lock (syncRoot)
        {
            if (IsScanRunning)
            {
                StatusMessage = "A scan is already running.";
                return;
            }

            IsScanRunning = true;
            LastScanStartedAtUtc = DateTimeOffset.UtcNow;
            StatusMessage = IsAiPaused
                ? "Manual scan started while scheduled AI activity is paused."
                : "Manual scan started.";
        }

        OnChanged();

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            lock (syncRoot)
            {
                LastScanFinishedAtUtc = DateTimeOffset.UtcNow;
                StatusMessage = "Manual scan completed. Market data and AI integrations are not wired yet.";
            }
        }
        catch (OperationCanceledException)
        {
            lock (syncRoot)
            {
                StatusMessage = "Manual scan was cancelled.";
            }
        }
        finally
        {
            lock (syncRoot)
            {
                IsScanRunning = false;
            }

            OnChanged();
        }
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}

internal sealed record AppStateSnapshot(
    bool IsAiPaused,
    bool IsScanRunning,
    DateTimeOffset? LastScanStartedAtUtc,
    DateTimeOffset? LastScanFinishedAtUtc,
    string StatusMessage);
