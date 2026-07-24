using TaskManager.Core.Abstractions;

namespace TaskManager.Core.Monitoring;

/// <summary>
/// The <b>End task</b> flow (spec §8) — the only mutating action in v1, and the one place
/// its ordering is stated: confirm unconditionally, terminate, and offer <b>Restart as
/// administrator</b> only when the attempt came back Access Denied. It takes primitives
/// rather than a row, and never reads or clears selection: which process is selected stays
/// the view model's business.
/// </summary>
/// <remarks>
/// The caller's half of the flow, which cannot be pinned here without a WinUI test host:
/// the selection is cleared exactly when <see cref="EndAsync"/> returns
/// <see cref="TerminationOutcome.Success"/> or <see cref="TerminationOutcome.NotFound"/> —
/// the process is gone either way — and retained on <see langword="null"/>,
/// <see cref="TerminationOutcome.AccessDenied"/> and <see cref="TerminationOutcome.Failed"/>,
/// where the row is still there and the user may want to try again.
/// </remarks>
public sealed class EndTaskFlow
{
    private readonly IProcessTerminator _terminator;
    private readonly IElevationService _elevation;
    private readonly IEndTaskInteraction _interaction;

    public EndTaskFlow(
        IProcessTerminator terminator,
        IElevationService elevation,
        IEndTaskInteraction interaction)
    {
        _terminator = terminator;
        _elevation = elevation;
        _interaction = interaction;
    }

    /// <summary>
    /// Runs the spec §8 End task flow for one process.
    /// </summary>
    /// <returns>
    /// <see langword="null"/> if the user declined the confirm dialog — nothing was
    /// attempted. Otherwise the terminator's outcome.
    /// </returns>
    public async Task<TerminationOutcome?> EndAsync(int processId, string processName)
    {
        // Unconditional confirm first (spec §8): apps, background and service-hosting
        // processes alike, with no rights probed up front.
        if (!await _interaction.ConfirmEndTaskAsync(processName))
        {
            return null;
        }

        TerminationOutcome outcome = _terminator.Terminate(processId);
        switch (outcome)
        {
            case TerminationOutcome.Success:
            case TerminationOutcome.NotFound:
                // A process that was already gone is not a failure the user needs told
                // about — the row disappears on the next tick either way.
                break;

            case TerminationOutcome.AccessDenied:
                // The elevate affordance is offered here and only here — never as a
                // pre-disabled End task (spec §8).
                if (await _interaction.ShowAccessDeniedAsync(processName))
                {
                    _elevation.RestartElevated();
                }

                break;

            default:
                await _interaction.ShowFailedAsync(processName);
                break;
        }

        return outcome;
    }
}
