namespace GitGhStandardCli.Models;

public enum SynchronizationValidationOutcome
{
	Ok,
	CanonicalSourceUnavailable,
	VersionDrift,
	EnforcementFailure,
	Blocked
}

public sealed class SynchronizationValidationEvidence
{
	public bool CanonicalSourceExists { get; set; }

	public bool CanonicalWorkflowExists { get; set; }

	public bool CanonicalVersionResolved { get; set; }

	public string CanonicalVersion { get; set; } = string.Empty;

	public string LocalVersion { get; set; } = string.Empty;

	public bool VersionDriftDetected { get; set; }

	public bool EnforcementFailuresDetected { get; set; }

	public bool ObserveMode { get; set; }

	public bool AncestryProofValid { get; set; } = true;

	public string AncestryProofFailureReason { get; set; } = string.Empty;

	public bool ScopeProofValid { get; set; } = true;

	public string ScopeProofFailureReason { get; set; } = string.Empty;

	public bool OwnershipPathSafetyValid { get; set; } = true;

	public string OwnershipPathSafetyFailureReason { get; set; } = string.Empty;

	public int ChurnVolume { get; set; }

	public int ChurnWarningThreshold { get; set; }

	public int ChurnBlockThreshold { get; set; }

	public bool OverrideActive { get; set; }

	public string OverrideReason { get; set; } = string.Empty;

	public int OverrideApprovalsRequired { get; set; }

	public int OverrideApprovalsReceived { get; set; }

	public string OverrideExpiryUtc { get; set; } = string.Empty;

	public bool OverrideAutoMergeAllowed { get; set; }

	public string DivergenceSeverity { get; set; } = "none";

	public IReadOnlyList<string> FailureMessages { get; set; } = Array.Empty<string>();

	public Dictionary<string, object?> Fields { get; set; } = new();
}

public sealed record SynchronizationValidationDecision(
	SynchronizationValidationOutcome Outcome,
	int ExitCode,
	SynchronizationValidationEvidence Evidence);

public static class SynchronizationValidationContract
{
	public static SynchronizationValidationDecision Evaluate(SynchronizationValidationEvidence evidence)
	{
		var warningCategories = BuildWarningCategories(evidence);
		var telemetryVolume = warningCategories.Count + evidence.FailureMessages.Count;
		var telemetryChurn = warningCategories.Count;
		var telemetryWarningCategories = string.Join(",", warningCategories);
		var telemetryFailureCategories = telemetryWarningCategories;
		var divergenceSeverity = BuildDivergenceSeverity(evidence);

		var normalizedEvidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = evidence.CanonicalSourceExists,
			CanonicalWorkflowExists = evidence.CanonicalWorkflowExists,
			CanonicalVersionResolved = evidence.CanonicalVersionResolved,
			CanonicalVersion = evidence.CanonicalVersion,
			LocalVersion = evidence.LocalVersion,
			VersionDriftDetected = evidence.VersionDriftDetected,
			EnforcementFailuresDetected = evidence.EnforcementFailuresDetected,
			ObserveMode = evidence.ObserveMode,
			AncestryProofValid = evidence.AncestryProofValid,
			AncestryProofFailureReason = evidence.AncestryProofFailureReason,
			ScopeProofValid = evidence.ScopeProofValid,
			ScopeProofFailureReason = evidence.ScopeProofFailureReason,
			OwnershipPathSafetyValid = evidence.OwnershipPathSafetyValid,
			OwnershipPathSafetyFailureReason = evidence.OwnershipPathSafetyFailureReason,
			ChurnVolume = evidence.ChurnVolume,
			ChurnWarningThreshold = evidence.ChurnWarningThreshold,
			ChurnBlockThreshold = evidence.ChurnBlockThreshold,
			OverrideActive = evidence.OverrideActive,
			OverrideReason = evidence.OverrideReason,
			OverrideApprovalsRequired = evidence.OverrideApprovalsRequired,
			OverrideApprovalsReceived = evidence.OverrideApprovalsReceived,
			OverrideExpiryUtc = evidence.OverrideExpiryUtc,
			OverrideAutoMergeAllowed = evidence.OverrideAutoMergeAllowed,
			DivergenceSeverity = divergenceSeverity,
			FailureMessages = evidence.FailureMessages,
			Fields = new Dictionary<string, object?>(evidence.Fields)
			{
				["canonicalSourceExists"] = evidence.CanonicalSourceExists,
				["canonicalWorkflowExists"] = evidence.CanonicalWorkflowExists,
				["canonicalVersionResolved"] = evidence.CanonicalVersionResolved,
				["canonicalVersion"] = evidence.CanonicalVersion,
				["localVersion"] = evidence.LocalVersion,
				["versionDriftDetected"] = evidence.VersionDriftDetected,
				["enforcementFailuresDetected"] = evidence.EnforcementFailuresDetected,
				["observeMode"] = evidence.ObserveMode,
				["ancestryProofValid"] = evidence.AncestryProofValid,
				["ancestryProofFailureReason"] = evidence.AncestryProofFailureReason,
				["scopeProofValid"] = evidence.ScopeProofValid,
				["scopeProofFailureReason"] = evidence.ScopeProofFailureReason,
				["ownershipPathSafetyValid"] = evidence.OwnershipPathSafetyValid,
				["ownershipPathSafetyFailureReason"] = evidence.OwnershipPathSafetyFailureReason,
				["churnVolume"] = evidence.ChurnVolume,
				["churnWarningThreshold"] = evidence.ChurnWarningThreshold,
				["churnBlockThreshold"] = evidence.ChurnBlockThreshold,
				["overrideActive"] = evidence.OverrideActive,
				["overrideReason"] = evidence.OverrideReason,
				["overrideApprovalsRequired"] = evidence.OverrideApprovalsRequired,
				["overrideApprovalsReceived"] = evidence.OverrideApprovalsReceived,
				["overrideExpiryUtc"] = evidence.OverrideExpiryUtc,
				["overrideAutoMergeAllowed"] = evidence.OverrideAutoMergeAllowed,
				["divergenceSeverity"] = divergenceSeverity,
				["telemetryVolume"] = telemetryVolume,
				["telemetryChurn"] = telemetryChurn,
				["telemetryWarningCategories"] = telemetryWarningCategories,
				["telemetryFailureCategories"] = telemetryFailureCategories
			}
		};

		if (!evidence.CanonicalSourceExists || !evidence.CanonicalWorkflowExists)
		{
			return BuildDecision(evidence, normalizedEvidence,
				SynchronizationValidationOutcome.CanonicalSourceUnavailable, 2);
		}

		if (!evidence.CanonicalVersionResolved)
		{
			return BuildDecision(evidence, normalizedEvidence,
				SynchronizationValidationOutcome.CanonicalSourceUnavailable, 2);
		}

		if (!evidence.AncestryProofValid)
		{
			var failureMessage = string.IsNullOrWhiteSpace(evidence.AncestryProofFailureReason)
				? "Ancestry proof failed"
				: $"Ancestry proof failed: {evidence.AncestryProofFailureReason}";
			var failureMessages = evidence.FailureMessages.ToList();
			if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
			{
				failureMessages.Add(failureMessage);
			}

			normalizedEvidence.FailureMessages = failureMessages;
			normalizedEvidence.DivergenceSeverity = "critical";
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
		}

		if (!evidence.ScopeProofValid)
		{
			var failureMessage = string.IsNullOrWhiteSpace(evidence.ScopeProofFailureReason)
				? "Scope proof failed"
				: $"Scope proof failed: {evidence.ScopeProofFailureReason}";
			var failureMessages = evidence.FailureMessages.ToList();
			if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
			{
				failureMessages.Add(failureMessage);
			}

			normalizedEvidence.FailureMessages = failureMessages;
			normalizedEvidence.DivergenceSeverity = "critical";
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
		}

		if (!evidence.OwnershipPathSafetyValid)
		{
			var failureMessage = string.IsNullOrWhiteSpace(evidence.OwnershipPathSafetyFailureReason)
				? "Ownership path safety failed"
				: $"Ownership path safety failed: {evidence.OwnershipPathSafetyFailureReason}";
			var failureMessages = evidence.FailureMessages.ToList();
			if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
			{
				failureMessages.Add(failureMessage);
			}

			normalizedEvidence.FailureMessages = failureMessages;
			normalizedEvidence.DivergenceSeverity = "critical";
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
		}

		if (evidence.ChurnBlockThreshold > 0 && evidence.ChurnVolume > evidence.ChurnBlockThreshold)
		{
			var failureMessage =
				$"Churn budget exceeded: {evidence.ChurnVolume} exceeded block threshold {evidence.ChurnBlockThreshold}";
			var failureMessages = evidence.FailureMessages.ToList();
			if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
			{
				failureMessages.Add(failureMessage);
			}

			normalizedEvidence.FailureMessages = failureMessages;
			normalizedEvidence.DivergenceSeverity = "critical";
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
		}

		if (evidence.ChurnWarningThreshold > 0 && evidence.ChurnVolume > evidence.ChurnWarningThreshold)
		{
			var failureMessage =
				$"Churn budget warning: {evidence.ChurnVolume} exceeded warning threshold {evidence.ChurnWarningThreshold}";
			var failureMessages = evidence.FailureMessages.ToList();
			if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
			{
				failureMessages.Add(failureMessage);
			}

			normalizedEvidence.FailureMessages = failureMessages;
			normalizedEvidence.DivergenceSeverity = "warning";
			normalizedEvidence.Fields["divergenceSeverity"] = "warning";
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Ok, 0);
		}

		if (evidence.OverrideActive)
		{
			if (string.IsNullOrWhiteSpace(evidence.OverrideReason))
			{
				var failureMessage = "Override governance failed: override reason is required";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}

			if (evidence.OverrideApprovalsRequired > evidence.OverrideApprovalsReceived)
			{
				var failureMessage =
					$"Override governance failed: received {evidence.OverrideApprovalsReceived} approvals but {evidence.OverrideApprovalsRequired} are required";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}

			if (evidence.OverrideAutoMergeAllowed)
			{
				var failureMessage = "Override governance failed: override PRs must not auto-merge";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}

			if (string.IsNullOrWhiteSpace(evidence.OverrideExpiryUtc))
			{
				var failureMessage = "Override governance failed: override expiry is required";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}

			if (!DateTimeOffset.TryParse(evidence.OverrideExpiryUtc, out var expiryUtc))
			{
				var failureMessage = "Override governance failed: override expiry is invalid";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}

			if (expiryUtc < DateTimeOffset.UtcNow)
			{
				var failureMessage = "Override governance failed: override window has expired";
				var failureMessages = evidence.FailureMessages.ToList();
				if (!failureMessages.Contains(failureMessage, StringComparer.Ordinal))
				{
					failureMessages.Add(failureMessage);
				}

				normalizedEvidence.FailureMessages = failureMessages;
				normalizedEvidence.DivergenceSeverity = "critical";
				return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Blocked, 4);
			}
		}

		if (evidence.ObserveMode && (evidence.VersionDriftDetected || evidence.EnforcementFailuresDetected ||
		                             evidence.FailureMessages.Count > 0))
		{
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Ok, 0);
		}

		if (evidence.VersionDriftDetected)
		{
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.VersionDrift, 3);
		}

		if (evidence.EnforcementFailuresDetected)
		{
			return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.EnforcementFailure, 4);
		}

		return BuildDecision(evidence, normalizedEvidence, SynchronizationValidationOutcome.Ok, 0);
	}

	private static SynchronizationValidationDecision BuildDecision(
		SynchronizationValidationEvidence evidence,
		SynchronizationValidationEvidence normalizedEvidence,
		SynchronizationValidationOutcome outcome,
		int exitCode)
	{
		ApplyTelemetry(evidence, normalizedEvidence);
		return new SynchronizationValidationDecision(outcome, exitCode, normalizedEvidence);
	}

	private static void ApplyTelemetry(
		SynchronizationValidationEvidence evidence,
		SynchronizationValidationEvidence normalizedEvidence)
	{
		var telemetryEvidence = new SynchronizationValidationEvidence
		{
			CanonicalSourceExists = evidence.CanonicalSourceExists,
			CanonicalWorkflowExists = evidence.CanonicalWorkflowExists,
			CanonicalVersionResolved = evidence.CanonicalVersionResolved,
			CanonicalVersion = evidence.CanonicalVersion,
			LocalVersion = evidence.LocalVersion,
			VersionDriftDetected = evidence.VersionDriftDetected,
			EnforcementFailuresDetected = evidence.EnforcementFailuresDetected,
			ObserveMode = evidence.ObserveMode,
			AncestryProofValid = evidence.AncestryProofValid,
			AncestryProofFailureReason = evidence.AncestryProofFailureReason,
			ScopeProofValid = evidence.ScopeProofValid,
			ScopeProofFailureReason = evidence.ScopeProofFailureReason,
			OwnershipPathSafetyValid = evidence.OwnershipPathSafetyValid,
			OwnershipPathSafetyFailureReason = evidence.OwnershipPathSafetyFailureReason,
			ChurnVolume = evidence.ChurnVolume,
			ChurnWarningThreshold = evidence.ChurnWarningThreshold,
			ChurnBlockThreshold = evidence.ChurnBlockThreshold,
			OverrideActive = evidence.OverrideActive,
			OverrideReason = evidence.OverrideReason,
			OverrideApprovalsRequired = evidence.OverrideApprovalsRequired,
			OverrideApprovalsReceived = evidence.OverrideApprovalsReceived,
			OverrideExpiryUtc = evidence.OverrideExpiryUtc,
			OverrideAutoMergeAllowed = evidence.OverrideAutoMergeAllowed,
			DivergenceSeverity = evidence.DivergenceSeverity,
			FailureMessages = normalizedEvidence.FailureMessages,
			Fields = new Dictionary<string, object?>(evidence.Fields)
		};

		var warningCategories = BuildWarningCategories(telemetryEvidence);
		var telemetryVolume = warningCategories.Count + normalizedEvidence.FailureMessages.Count;
		var telemetryChurn = warningCategories.Count;
		var telemetryWarningCategories = string.Join(",", warningCategories);
		normalizedEvidence.DivergenceSeverity = BuildDivergenceSeverity(telemetryEvidence);
		normalizedEvidence.Fields["divergenceSeverity"] = normalizedEvidence.DivergenceSeverity;
		normalizedEvidence.Fields["telemetryVolume"] = telemetryVolume;
		normalizedEvidence.Fields["telemetryChurn"] = telemetryChurn;
		normalizedEvidence.Fields["telemetryWarningCategories"] = telemetryWarningCategories;
		normalizedEvidence.Fields["telemetryFailureCategories"] = telemetryWarningCategories;
	}

	private static string BuildDivergenceSeverity(SynchronizationValidationEvidence evidence)
	{
		if (!evidence.AncestryProofValid || !evidence.ScopeProofValid || !evidence.OwnershipPathSafetyValid)
		{
			return "critical";
		}

		if (evidence.OverrideActive && (string.IsNullOrWhiteSpace(evidence.OverrideReason) ||
		                                evidence.OverrideApprovalsRequired > evidence.OverrideApprovalsReceived ||
		                                evidence.OverrideAutoMergeAllowed ||
		                                string.IsNullOrWhiteSpace(evidence.OverrideExpiryUtc) ||
		                                !DateTimeOffset.TryParse(evidence.OverrideExpiryUtc, out var expiryUtc) ||
		                                expiryUtc < DateTimeOffset.UtcNow))
		{
			return "critical";
		}

		if (evidence.ChurnBlockThreshold > 0 && evidence.ChurnVolume > evidence.ChurnBlockThreshold)
		{
			return "critical";
		}

		if (evidence.ChurnWarningThreshold > 0 && evidence.ChurnVolume > evidence.ChurnWarningThreshold)
		{
			return "warning";
		}

		if (!evidence.VersionDriftDetected && !evidence.EnforcementFailuresDetected &&
		    evidence.FailureMessages.Count == 0)
		{
			return "none";
		}

		return evidence.ObserveMode ? "warning" : "critical";
	}

	private static List<string> BuildWarningCategories(SynchronizationValidationEvidence evidence)
	{
		var categories = new List<string>();
		if (evidence.VersionDriftDetected)
		{
			categories.Add("version-drift");
		}

		if (evidence.EnforcementFailuresDetected)
		{
			categories.Add("enforcement-failure");
		}

		if (!evidence.AncestryProofValid)
		{
			categories.Add("ancestry-proof");
		}

		if (!evidence.ScopeProofValid)
		{
			categories.Add("scope-proof");
		}

		if (!evidence.OwnershipPathSafetyValid)
		{
			categories.Add("ownership-path-safety");
		}

		if (evidence.ChurnWarningThreshold > 0 && evidence.ChurnVolume > evidence.ChurnWarningThreshold)
		{
			categories.Add("churn-budget");
		}

		if (evidence.OverrideActive && (string.IsNullOrWhiteSpace(evidence.OverrideReason) ||
		                                evidence.OverrideApprovalsRequired > evidence.OverrideApprovalsReceived ||
		                                evidence.OverrideAutoMergeAllowed ||
		                                string.IsNullOrWhiteSpace(evidence.OverrideExpiryUtc) ||
		                                !DateTimeOffset.TryParse(evidence.OverrideExpiryUtc, out var expiryUtc) ||
		                                expiryUtc < DateTimeOffset.UtcNow))
		{
			categories.Add("override-governance");
		}

		if (evidence.FailureMessages.Any(message => message.Contains("drift", StringComparison.OrdinalIgnoreCase)))
		{
			categories.Add("content-drift");
		}

		if (evidence.FailureMessages.Any(message =>
			    message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
			    message.Contains("not found", StringComparison.OrdinalIgnoreCase)))
		{
			categories.Add("source-unavailable");
		}

		return categories
			.OrderBy(category => category, StringComparer.Ordinal)
			.ToList();
	}
}
