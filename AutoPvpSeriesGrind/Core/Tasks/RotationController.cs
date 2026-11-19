using AutoPvpSeriesGrind.Core.Combat;
using AutoPvpSeriesGrind.Core.Game;
using AutoPvpSeriesGrind.Core.Ipc;
using AutoPvpSeriesGrind.Core.Rotation;
using System.Numerics;

namespace AutoPvpSeriesGrind.Core.Tasks;

internal sealed class RotationController(PvpBrain brain)
{
    private readonly PvpBrain brain = brain;
    private readonly PvpRotationDriver driver = new();
    private readonly RotationHandoff handoff = new();

    private RotationProvider provider = RotationProvider.Internal;
    private bool wasDead;

    public void Reset()
    {
        wasDead = false;
        ReleaseSolver();
    }

    public void Configure(RotationProvider rotationProvider, in RotationSettings rotationSettings)
    {
        provider = rotationProvider;
        driver.Configure(in rotationSettings);
    }

    public void OnMatchStart() => driver.OnAlive();

    public void TickDeathAndRespawn()
    {
        if (MatchState.LocalIsDead())
        {
            OnDeathDetected();
            return;
        }

        OnRespawnDetected();
    }

    private void OnDeathDetected()
    {
        if (wasDead)
        {
            return;
        }

        wasDead = true;
        brain.Reset();
        RunLog.Info("death detected -> waiting for respawn");
    }

    private void OnRespawnDetected()
    {
        if (!wasDead)
        {
            return;
        }

        wasDead = false;
        driver.OnAlive();
        RunLog.Info("respawn detected");
    }

    // pluginTargets: the caller sets the hard target itself, so Rotation Solver Reborn only has to press skills.
    public RotationOutcome Drive(PvpSnapshot snapshot, ulong targetId, bool pluginTargets, Posture posture, Vector3 moveDestination, bool underBurst, Action holdStill)
    {
        if (provider == RotationProvider.Manual
            || (provider == RotationProvider.RotationSolver && SolverPresses(pluginTargets))
            || Plugin.Instance.LimitBreak.Firing)
        {
            return RotationOutcome.None;
        }

        return driver.Tick(snapshot, targetId, underBurst, posture, moveDestination, holdStill);
    }

    public void ReleaseSolver()
    {
        if (!handoff.TakeStop())
        {
            return;
        }

        var solver = RotationSolverIpc.Instance;
        if (solver.IsActive())
        {
            solver.Stop();
            RunLog.Info("rotation -> Rotation Solver Reborn switched off");
        }
    }

    // The built-in rotation presses on every tick Rotation Solver Reborn does not report itself active, so a
    // disabled, outdated or unwilling RSR never leaves the character without a rotation.
    private bool SolverPresses(bool pluginTargets)
    {
        var solver = RotationSolverIpc.Instance;
        if (!solver.IsAvailable)
        {
            return false;
        }

        var active = solver.IsActive();
        if (!handoff.ShouldStart(active, Environment.TickCount64))
        {
            return active;
        }

        solver.Start(pressOnly: pluginTargets);
        if (solver.IsActive())
        {
            RunLog.Info($"rotation -> Rotation Solver Reborn ({(pluginTargets ? "presses skills on the plugin's target" : "picks its own targets")})");
            return true;
        }

        if (handoff.GaveUp)
        {
            RunLog.Warning($"Rotation Solver Reborn did not start after {RotationHandoff.MaxStarts} tries -> the built-in rotation takes over");
        }

        return false;
    }
}
