using AutoPvpSeriesGrind.Core.Tasks;
using Xunit;

namespace AutoPvpSeriesGrind.Tests;

public sealed class RotationHandoffTests
{
    [Fact]
    public void StartsAreSpacedOutWhileTheRotationStaysOff()
    {
        var handoff = new RotationHandoff();

        Assert.True(handoff.ShouldStart(active: false, nowMs: 0));
        Assert.False(handoff.ShouldStart(active: false, nowMs: RotationHandoff.RetryMs - 1));
        Assert.True(handoff.ShouldStart(active: false, nowMs: RotationHandoff.RetryMs));
    }

    [Fact]
    public void GivesUpAfterTheLastUnansweredStart()
    {
        var handoff = new RotationHandoff();
        var now = 0L;
        for (var start = 0; start < RotationHandoff.MaxStarts; start++, now += RotationHandoff.RetryMs)
        {
            Assert.False(handoff.GaveUp);
            Assert.True(handoff.ShouldStart(active: false, now));
        }

        Assert.True(handoff.GaveUp);
        Assert.False(handoff.ShouldStart(active: false, now + RotationHandoff.RetryMs * 100));
    }

    [Fact]
    public void AnAnsweredStartEarnsAFreshSetOfTries()
    {
        var handoff = new RotationHandoff();
        var now = 0L;
        for (var start = 0; start < RotationHandoff.MaxStarts - 1; start++, now += RotationHandoff.RetryMs)
        {
            handoff.ShouldStart(active: false, now);
        }

        Assert.False(handoff.ShouldStart(active: true, now));

        for (var start = 0; start < RotationHandoff.MaxStarts; start++, now += RotationHandoff.RetryMs)
        {
            Assert.True(handoff.ShouldStart(active: false, now));
        }
    }

    [Fact]
    public void OnlyARotationThisRunStartedIsStopped()
    {
        var handoff = new RotationHandoff();
        Assert.False(handoff.ShouldStart(active: true, nowMs: 0));
        Assert.False(handoff.TakeStop());

        Assert.True(handoff.ShouldStart(active: false, nowMs: 0));
        Assert.True(handoff.TakeStop());
        Assert.False(handoff.TakeStop());
    }
}
