using FluentAssertions;
using GitGhStandardCli.Models;

namespace SquadWorkflowStandard.Tests;

public sealed class SynchronizationValidationContractTests
{
	[Fact]
	public void Evaluate_ShouldPreferVersionDrift_WhenDriftAndEnforcementFailuresArePresent()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			VersionDriftDetected = true,
			EnforcementFailuresDetected = true,
			FailureMessages = ["content drift in .squad/routing.md"]
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.VersionDrift);
		decision.ExitCode.Should().Be(3);
		decision.Evidence.Fields["canonicalVersion"].Should().Be("2.0.0");
		decision.Evidence.Fields["localVersion"].Should().Be("1.9.0");
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("content drift in .squad/routing.md");
	}

	[Fact]
	public void Evaluate_ShouldEmitObserveModeTelemetry_WithoutBlockingWhenObserveModeIsEnabled()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			VersionDriftDetected = true,
			EnforcementFailuresDetected = true,
			ObserveMode = true,
			FailureMessages = ["content drift in .squad/routing.md"]
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Ok);
		decision.ExitCode.Should().Be(0);
		decision.Evidence.DivergenceSeverity.Should().Be("warning");
		decision.Evidence.Fields["observeMode"].Should().Be(true);
		decision.Evidence.Fields["divergenceSeverity"].Should().Be("warning");
		decision.Evidence.Fields["telemetryVolume"].Should().Be(4);
		decision.Evidence.Fields["telemetryChurn"].Should().Be(3);
		decision.Evidence.Fields["telemetryWarningCategories"].Should()
			.Be("content-drift,enforcement-failure,version-drift");
		decision.Evidence.Fields["telemetryFailureCategories"].Should()
			.Be("content-drift,enforcement-failure,version-drift");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenAncestryProofIsMissing()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			AncestryProofValid = false,
			AncestryProofFailureReason = "missing merge-base evidence"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["ancestryProofValid"].Should().Be(false);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Ancestry proof failed: missing merge-base evidence");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenScopeProofIsMissing()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			ScopeProofValid = false,
			ScopeProofFailureReason = "changed files outside sync classes"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["scopeProofValid"].Should().Be(false);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Scope proof failed: changed files outside sync classes");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOwnershipPathSafetyIsViolated()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OwnershipPathSafetyValid = false,
			OwnershipPathSafetyFailureReason = "protected path class leakage"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["ownershipPathSafetyValid"].Should().Be(false);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Ownership path safety failed: protected path class leakage");
	}

	[Fact]
	public void Evaluate_ShouldWarn_WhenChurnExceedsWarningThreshold_InObserveMode()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			ObserveMode = true,
			ChurnVolume = 20,
			ChurnWarningThreshold = 10,
			ChurnBlockThreshold = 25
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Ok);
		decision.ExitCode.Should().Be(0);
		decision.Evidence.DivergenceSeverity.Should().Be("warning");
		decision.Evidence.Fields["telemetryWarningCategories"].Should().Be("churn-budget");
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Churn budget warning: 20 exceeded warning threshold 10");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenChurnExceedsBlockThreshold()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			ChurnVolume = 30,
			ChurnWarningThreshold = 10,
			ChurnBlockThreshold = 25
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.DivergenceSeverity.Should().Be("critical");
		decision.Evidence.Fields["churnVolume"].Should().Be(30);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Churn budget exceeded: 30 exceeded block threshold 25");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOverrideApprovalsAreInsufficient()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OverrideActive = true,
			OverrideReason = "incident recovery",
			OverrideApprovalsRequired = 2,
			OverrideApprovalsReceived = 1,
			OverrideExpiryUtc = "2099-01-01T00:00:00Z"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["overrideActive"].Should().Be(true);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Override governance failed: received 1 approvals but 2 are required");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOverrideAutoMergeIsEnabled()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OverrideActive = true,
			OverrideReason = "incident recovery",
			OverrideApprovalsRequired = 2,
			OverrideApprovalsReceived = 2,
			OverrideExpiryUtc = "2099-01-01T00:00:00Z",
			OverrideAutoMergeAllowed = true
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["overrideAutoMergeAllowed"].Should().Be(true);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Override governance failed: override PRs must not auto-merge");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOverrideReasonIsMissing()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OverrideActive = true,
			OverrideApprovalsRequired = 2,
			OverrideApprovalsReceived = 2,
			OverrideExpiryUtc = "2099-01-01T00:00:00Z"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["overrideActive"].Should().Be(true);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Override governance failed: override reason is required");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOverrideExpiryIsMissing()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OverrideActive = true,
			OverrideReason = "incident recovery",
			OverrideApprovalsRequired = 2,
			OverrideApprovalsReceived = 2
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["overrideActive"].Should().Be(true);
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Override governance failed: override expiry is required");
	}

	[Fact]
	public void Evaluate_ShouldBlock_WhenOverrideHasExpired()
	{
		var evidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = true,
			CanonicalWorkflowExists = true,
			CanonicalVersionResolved = true,
			CanonicalVersion = "2.0.0",
			LocalVersion = "1.9.0",
			OverrideActive = true,
			OverrideReason = "incident recovery",
			OverrideApprovalsRequired = 2,
			OverrideApprovalsReceived = 2,
			OverrideExpiryUtc = "2000-01-01T00:00:00Z"
		};

		var decision = SynchronizationValidationContract.Evaluate(evidence);

		decision.Outcome.Should().Be(SynchronizationValidationOutcome.Blocked);
		decision.ExitCode.Should().Be(4);
		decision.Evidence.Fields["overrideExpiryUtc"].Should().Be("2000-01-01T00:00:00Z");
		decision.Evidence.FailureMessages.Should().ContainSingle().Which.Should()
			.Be("Override governance failed: override window has expired");
	}
}
