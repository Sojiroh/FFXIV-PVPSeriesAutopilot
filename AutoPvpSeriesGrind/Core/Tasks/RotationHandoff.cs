namespace AutoPvpSeriesGrind.Core.Tasks;

// Decides when to ask an external rotation plugin to start pressing skills. That plugin switches itself off
// (on death, between areas) and can refuse a start outright, answering each refusal with a chat error, so
// starts are spaced out and stop after a few unanswered ones.
internal sealed class RotationHandoff
{
    public const int MaxStarts = 3;
    public const long RetryMs = 3000;

    private int unansweredStarts;
    private long nextStartAtMs;
    private bool started;

    public bool GaveUp => unansweredStarts >= MaxStarts;

    public bool ShouldStart(bool active, long nowMs)
    {
        if (active)
        {
            unansweredStarts = 0;
            return false;
        }

        if (GaveUp || nowMs < nextStartAtMs)
        {
            return false;
        }

        unansweredStarts++;
        nextStartAtMs = nowMs + RetryMs;
        started = true;
        return true;
    }

    // True once after a start was asked for, so only a rotation this run switched on is switched off again.
    public bool TakeStop()
    {
        var wasStarted = started;
        started = false;
        return wasStarted;
    }
}
