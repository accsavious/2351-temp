using System.Collections.Concurrent;

namespace CampusHub.AuthServer.Diagnostics;

/// <summary>
/// One line on the /Activity page.
/// </summary>
public sealed record ActivityEntry(DateTime Time, LogLevel Level, string Source, string Message);

/// <summary>
/// The most recent sign-in activity, kept in memory so the /Activity page can show it.
/// It holds the same messages the console prints, nothing more. Restarting the server clears it.
/// </summary>
public sealed class ActivityLog
{
    private const int Capacity = 200;
    private readonly ConcurrentQueue<ActivityEntry> _entries = new();

    public void Add(ActivityEntry entry)
    {
        _entries.Enqueue(entry);
        while (_entries.Count > Capacity && _entries.TryDequeue(out _)) { }
    }

    /// <summary>Newest first.</summary>
    public IReadOnlyList<ActivityEntry> Snapshot() => _entries.Reverse().ToList();

    public void Clear() => _entries.Clear();
}

/// <summary>
/// Copies log messages into <see cref="ActivityLog"/>. Only two sources are kept: OpenIddict,
/// which explains every step of the OIDC flow and every rejection, and this server's own code
/// (the login page). Everything else stays console-only.
/// </summary>
public sealed class ActivityLoggerProvider(ActivityLog log) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) =>
        categoryName.StartsWith("OpenIddict", StringComparison.Ordinal) ||
        categoryName.StartsWith("CampusHub", StringComparison.Ordinal)
            ? new ActivityLogger(log, ShortName(categoryName))
            : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;

    public void Dispose() { }

    // "OpenIddict.Server.OpenIddictServerDispatcher" → "OpenIddictServerDispatcher"
    private static string ShortName(string category) =>
        category[(category.LastIndexOf('.') + 1)..];

    private sealed class ActivityLogger(ActivityLog log, string source) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (exception is not null)
            {
                message += $" ({exception.GetType().Name}: {exception.Message})";
            }

            log.Add(new ActivityEntry(DateTime.Now, logLevel, source, message));
        }
    }
}
