using VisitorGuide.Api.Services;

namespace VisitorGuide.Application.Tests;

public sealed class OnlineVisitorTrackerTests
{
    [Fact]
    public void TabsInTheSameBrowserCountOnceAndLeaveOnlyWhenTheLastTabLeaves()
    {
        var tracker = new OnlineVisitorTracker(new TestTimeProvider());
        var browserId = Guid.NewGuid();
        var firstTab = Guid.NewGuid();
        var secondTab = Guid.NewGuid();

        Assert.Equal(1, tracker.Heartbeat(browserId, firstTab));
        Assert.Equal(1, tracker.Heartbeat(browserId, secondTab));
        Assert.Equal(1, tracker.Leave(browserId, firstTab));
        Assert.Equal(1, tracker.GetOnlineCount());
        Assert.Equal(0, tracker.Leave(browserId, secondTab));
    }

    [Fact]
    public void DifferentBrowsersCountSeparately()
    {
        var tracker = new OnlineVisitorTracker(new TestTimeProvider());

        Assert.Equal(1, tracker.Heartbeat(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Equal(2, tracker.Heartbeat(Guid.NewGuid(), Guid.NewGuid()));
    }

    [Fact]
    public void GetOnlineCountExpiresVisitorsAfterTheirHeartbeatTimesOut()
    {
        var timeProvider = new TestTimeProvider();
        var tracker = new OnlineVisitorTracker(timeProvider);
        var visitorId = Guid.NewGuid();

        var browserId = Guid.NewGuid();
        tracker.Heartbeat(browserId, visitorId);
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        tracker.Heartbeat(browserId, visitorId);
        timeProvider.Advance(TimeSpan.FromSeconds(30));
        Assert.Equal(1, tracker.GetOnlineCount());
        timeProvider.Advance(TimeSpan.FromSeconds(16));

        Assert.Equal(0, tracker.GetOnlineCount());
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
