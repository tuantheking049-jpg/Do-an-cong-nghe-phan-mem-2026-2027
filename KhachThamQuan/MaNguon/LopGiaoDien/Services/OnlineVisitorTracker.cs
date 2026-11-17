namespace VisitorGuide.Api.Services;

public interface IOnlineVisitorTracker
{
    int Heartbeat(Guid browserId, Guid visitorId);
    int Leave(Guid browserId, Guid visitorId);
    int GetOnlineCount();
}

public sealed class OnlineVisitorTracker(TimeProvider timeProvider) : IOnlineVisitorTracker
{
    private static readonly TimeSpan InactiveTimeout = TimeSpan.FromSeconds(45);
    private readonly object _sync = new();
    private readonly Dictionary<Guid, VisitorSession> _sessionsByVisitor = new();

    public int Heartbeat(Guid browserId, Guid visitorId)
    {
        lock (_sync)
        {
            var now = timeProvider.GetUtcNow();
            RemoveInactiveVisitors(now);
            _sessionsByVisitor[visitorId] = new VisitorSession(browserId, now);
            return GetOnlineBrowserCount();
        }
    }

    public int Leave(Guid browserId, Guid visitorId)
    {
        lock (_sync)
        {
            RemoveInactiveVisitors(timeProvider.GetUtcNow());
            if (_sessionsByVisitor.TryGetValue(visitorId, out var session) && session.BrowserId == browserId)
                _sessionsByVisitor.Remove(visitorId);
            return GetOnlineBrowserCount();
        }
    }

    public int GetOnlineCount()
    {
        lock (_sync)
        {
            RemoveInactiveVisitors(timeProvider.GetUtcNow());
            return GetOnlineBrowserCount();
        }
    }

    private int GetOnlineBrowserCount() =>
        _sessionsByVisitor.Values.Select(session => session.BrowserId).Distinct().Count();

    private void RemoveInactiveVisitors(DateTimeOffset now)
    {
        var inactiveVisitors = _sessionsByVisitor
            .Where(visitor => now - visitor.Value.LastSeen > InactiveTimeout)
            .Select(visitor => visitor.Key)
            .ToArray();

        foreach (var visitorId in inactiveVisitors)
        {
            _sessionsByVisitor.Remove(visitorId);
        }
    }

    private sealed record VisitorSession(Guid BrowserId, DateTimeOffset LastSeen);
}
