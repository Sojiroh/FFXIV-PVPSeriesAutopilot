using Dalamud.Plugin.Ipc;
using ECommons.DalamudServices;

namespace AutoPvpSeriesGrind.Core.Ipc;

// Rotation Solver Reborn ignores its mode-changing IPC inside PvP territories, so its state is read over IPC
// and changed with its own /rotation commands.
internal sealed class RotationSolverIpc
{
    private static RotationSolverIpc? instance;
    public static RotationSolverIpc Instance => instance ??= new RotationSolverIpc();

    private readonly ICallGateSubscriber<bool> autorotationActive;

    private RotationSolverIpc()
    {
        autorotationActive = Svc.PluginInterface.GetIpcSubscriber<bool>(ApsgConstants.IpcGates.RotationSolverActive);
    }

    public bool IsAvailable => autorotationActive.HasFunction;

    public bool IsActive()
        => IpcGate.Invoke(autorotationActive.HasFunction, autorotationActive.InvokeFunc, false, "AutorotationActive failed");

    public void Start(bool pressOnly)
        => Svc.Commands.ProcessCommand(pressOnly
            ? ApsgConstants.RotationSolverCommands.PressOnly
            : ApsgConstants.RotationSolverCommands.PressAndTarget);

    public void Stop() => Svc.Commands.ProcessCommand(ApsgConstants.RotationSolverCommands.Off);
}
