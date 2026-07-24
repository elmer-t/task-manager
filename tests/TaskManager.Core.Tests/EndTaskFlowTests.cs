using TaskManager.Core.Abstractions;
using TaskManager.Core.Monitoring;
using Xunit;

namespace TaskManager.Core.Tests;

/// <summary>
/// Pins the spec §8 End task ordering: confirm unconditionally, terminate, and offer
/// "Restart as administrator" only on Access Denied. Every branch is checked for what it
/// does <em>and</em> for what it must not do — a dialog the flow shouldn't show is as much
/// a defect as one it fails to.
/// </summary>
public class EndTaskFlowTests
{
    private const int Pid = 4242;
    private const string Name = "chrome.exe";

    private sealed class FakeTerminator : IProcessTerminator
    {
        private readonly TerminationOutcome _outcome;

        public FakeTerminator(TerminationOutcome outcome) => _outcome = outcome;

        public int Calls { get; private set; }

        public int? LastProcessId { get; private set; }

        public TerminationOutcome Terminate(int processId)
        {
            Calls++;
            LastProcessId = processId;
            return _outcome;
        }
    }

    private sealed class FakeElevation : IElevationService
    {
        public int Calls { get; private set; }

        public void RestartElevated() => Calls++;
    }

    private sealed class FakeInteraction : IEndTaskInteraction
    {
        private readonly bool _confirms;
        private readonly bool _acceptsElevation;

        public FakeInteraction(bool confirms = true, bool acceptsElevation = false)
        {
            _confirms = confirms;
            _acceptsElevation = acceptsElevation;
        }

        public int Confirms { get; private set; }

        public int AccessDeniedDialogs { get; private set; }

        public int FailureDialogs { get; private set; }

        public Task<bool> ConfirmEndTaskAsync(string processName)
        {
            Confirms++;
            return Task.FromResult(_confirms);
        }

        public Task<bool> ShowAccessDeniedAsync(string processName)
        {
            AccessDeniedDialogs++;
            return Task.FromResult(_acceptsElevation);
        }

        public Task ShowFailedAsync(string processName)
        {
            FailureDialogs++;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Declining_the_confirm_attempts_nothing()
    {
        var terminator = new FakeTerminator(TerminationOutcome.Success);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction(confirms: false);
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Null(outcome);
        Assert.Equal(0, terminator.Calls);
        Assert.Equal(1, interaction.Confirms);
        Assert.Equal(0, interaction.AccessDeniedDialogs);
        Assert.Equal(0, interaction.FailureDialogs);
        Assert.Equal(0, elevation.Calls);
    }

    [Fact]
    public async Task A_successful_kill_shows_no_dialog_after_the_confirm()
    {
        var terminator = new FakeTerminator(TerminationOutcome.Success);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction();
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal(TerminationOutcome.Success, outcome);
        Assert.Equal(Pid, terminator.LastProcessId);
        Assert.Equal(0, interaction.AccessDeniedDialogs);
        Assert.Equal(0, interaction.FailureDialogs);
        Assert.Equal(0, elevation.Calls);
    }

    [Fact]
    public async Task A_process_that_was_already_gone_is_not_a_failure()
    {
        var terminator = new FakeTerminator(TerminationOutcome.NotFound);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction();
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal(TerminationOutcome.NotFound, outcome);
        Assert.Equal(0, interaction.FailureDialogs);
        Assert.Equal(0, interaction.AccessDeniedDialogs);
    }

    [Fact]
    public async Task Access_denied_with_an_accepted_prompt_relaunches_elevated_once()
    {
        var terminator = new FakeTerminator(TerminationOutcome.AccessDenied);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction(acceptsElevation: true);
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal(TerminationOutcome.AccessDenied, outcome);
        Assert.Equal(1, interaction.AccessDeniedDialogs);
        Assert.Equal(1, elevation.Calls);
    }

    [Fact]
    public async Task Access_denied_with_a_declined_prompt_stays_unelevated()
    {
        var terminator = new FakeTerminator(TerminationOutcome.AccessDenied);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction(acceptsElevation: false);
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal(TerminationOutcome.AccessDenied, outcome);
        Assert.Equal(1, interaction.AccessDeniedDialogs);
        Assert.Equal(0, elevation.Calls);
    }

    [Fact]
    public async Task A_failed_kill_reports_it_and_does_not_offer_elevation()
    {
        var terminator = new FakeTerminator(TerminationOutcome.Failed);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction();
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal(TerminationOutcome.Failed, outcome);
        Assert.Equal(1, interaction.FailureDialogs);
        Assert.Equal(0, interaction.AccessDeniedDialogs);
        Assert.Equal(0, elevation.Calls);
    }

    [Fact]
    public async Task An_unrecognised_outcome_takes_the_failure_branch()
    {
        var terminator = new FakeTerminator((TerminationOutcome)999);
        var elevation = new FakeElevation();
        var interaction = new FakeInteraction();
        var flow = new EndTaskFlow(terminator, elevation, interaction);

        TerminationOutcome? outcome = await flow.EndAsync(Pid, Name);

        Assert.Equal((TerminationOutcome)999, outcome);
        Assert.Equal(1, interaction.FailureDialogs);
        Assert.Equal(0, elevation.Calls);
    }
}
