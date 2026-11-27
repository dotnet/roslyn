// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Structured, bounded evidence emitted by the central ProjectDataBuild MSBuild logger.
/// </summary>
public sealed class ProjectDataBuildAttemptManifest
{
	public const int SchemaVersion = 1;
	public const string FileName = "attempt.manifest.json";

	public int Version { get; set; } = SchemaVersion;
	public string AttemptId { get; set; } = string.Empty;
	public bool BuildFinished { get; set; }
	public bool BuildSucceeded { get; set; }
	public bool BuildCancelled { get; set; }
	public bool ProjectDataBuildSubmissionObserved { get; set; }
	public string CompletedUtc { get; set; } = string.Empty;
	public int TruncatedDiagnosticCount { get; set; }
	public int TruncatedSubmissionCount { get; set; }
	public int TruncatedContextCount { get; set; }
	public ProjectDataBuildSubmissionRecord[] Submissions { get; set; } = [];
	public ProjectDataBuildContextRecord[] Contexts { get; set; } = [];
	public ProjectDataBuildDiagnosticRecord[] Diagnostics { get; set; } = [];

	public static string GetManifestFilePath(string receiptDirectory)
	{
		ThrowIfNullOrWhiteSpace(receiptDirectory, nameof(receiptDirectory));
		return Path.Combine(receiptDirectory, FileName);
	}

	public static bool TryRead(string receiptDirectory, string attemptId, out ProjectDataBuildAttemptManifest manifest)
	{
		manifest = new ProjectDataBuildAttemptManifest();
		try
		{
			ThrowIfNullOrWhiteSpace(receiptDirectory, nameof(receiptDirectory));
			ThrowIfNullOrWhiteSpace(attemptId, nameof(attemptId));

			string path = GetManifestFilePath(receiptDirectory);
			if (!File.Exists(path))
			{
				return false;
			}

			using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
			JsonElement root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object ||
				!TryGetInt32(root, "version", out int version) ||
				version != SchemaVersion ||
				!TryGetString(root, "attemptId", out string manifestAttemptId) ||
				!string.Equals(manifestAttemptId, attemptId, StringComparison.Ordinal))
			{
				return false;
			}

			manifest = new ProjectDataBuildAttemptManifest
			{
				Version = version,
				AttemptId = manifestAttemptId,
				BuildFinished = GetBoolean(root, "buildFinished"),
				BuildSucceeded = GetBoolean(root, "buildSucceeded"),
				BuildCancelled = GetBoolean(root, "buildCancelled"),
				ProjectDataBuildSubmissionObserved = GetBoolean(root, "projectDataBuildSubmissionObserved"),
				CompletedUtc = GetString(root, "completedUtc"),
				TruncatedDiagnosticCount = GetInt32(root, "truncatedDiagnosticCount"),
				TruncatedSubmissionCount = GetInt32(root, "truncatedSubmissionCount"),
				TruncatedContextCount = GetInt32(root, "truncatedContextCount"),
				Submissions = ReadSubmissions(root),
				Contexts = ReadContexts(root),
				Diagnostics = ReadDiagnostics(root),
			};
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or ArgumentException or NotSupportedException)
		{
			System.Diagnostics.Trace.TraceWarning(
				"[ProjectDataBuildAttemptManifest] Failed to read manifest for attempt {0}: {1}",
				attemptId,
				ex.Message);
			return false;
		}
	}

	private static ProjectDataBuildSubmissionRecord[] ReadSubmissions(JsonElement root)
	{
		if (!root.TryGetProperty("submissions", out JsonElement submissions) || submissions.ValueKind != JsonValueKind.Array)
		{
			return [];
		}

		List<ProjectDataBuildSubmissionRecord> result = [];
		foreach (JsonElement element in submissions.EnumerateArray())
		{
			if (element.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			result.Add(new ProjectDataBuildSubmissionRecord
			{
				SubmissionId = GetInt32(element, "submissionId"),
				Phase = GetString(element, "phase"),
				MSBuildIsRestoring = GetBoolean(element, "msBuildIsRestoring"),
				EntryProjects = ReadStringArray(element, "entryProjects"),
				TargetNames = ReadStringArray(element, "targetNames"),
				Context = ReadContext(element, "context"),
			});
		}

		return [.. result];
	}

	private static ProjectDataBuildContextRecord[] ReadContexts(JsonElement root)
	{
		if (!root.TryGetProperty("contexts", out JsonElement contexts) || contexts.ValueKind != JsonValueKind.Array)
		{
			return [];
		}

		List<ProjectDataBuildContextRecord> result = [];
		foreach (JsonElement element in contexts.EnumerateArray())
		{
			if (element.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			result.Add(new ProjectDataBuildContextRecord
			{
				Kind = GetString(element, "kind"),
				ProjectFilePath = GetString(element, "projectFilePath"),
				Context = ReadContext(element, "context"),
				ParentContext = ReadContext(element, "parentContext"),
			});
		}

		return [.. result];
	}

	private static ProjectDataBuildDiagnosticRecord[] ReadDiagnostics(JsonElement root)
	{
		if (!root.TryGetProperty("diagnostics", out JsonElement diagnostics) || diagnostics.ValueKind != JsonValueKind.Array)
		{
			return [];
		}

		List<ProjectDataBuildDiagnosticRecord> result = [];
		foreach (JsonElement element in diagnostics.EnumerateArray())
		{
			if (element.ValueKind != JsonValueKind.Object)
			{
				continue;
			}

			result.Add(new ProjectDataBuildDiagnosticRecord
			{
				Severity = GetString(element, "severity"),
				Phase = GetString(element, "phase"),
				ProjectFilePath = GetString(element, "projectFilePath"),
				ProjectFilePathSource = GetString(element, "projectFilePathSource"),
				FilePath = GetString(element, "filePath"),
				Code = GetString(element, "code"),
				Message = GetString(element, "message"),
				Line = GetInt32(element, "line"),
				Column = GetInt32(element, "column"),
				Context = ReadContext(element, "context"),
			});
		}

		return [.. result];
	}

	private static ProjectDataBuildEventContextRecord? ReadContext(JsonElement owner, string propertyName)
	{
		if (!owner.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.Object)
		{
			return null;
		}

		return new ProjectDataBuildEventContextRecord
		{
			NodeId = GetInt32(element, "nodeId"),
			ProjectContextId = GetInt32(element, "projectContextId"),
			ProjectInstanceId = GetInt32(element, "projectInstanceId"),
			TargetId = GetInt32(element, "targetId"),
			TaskId = GetInt32(element, "taskId"),
			SubmissionId = GetInt32(element, "submissionId"),
			EvaluationId = GetInt32(element, "evaluationId"),
			BuildRequestId = GetInt64(element, "buildRequestId"),
		};
	}

	private static string[] ReadStringArray(JsonElement owner, string propertyName)
	{
		if (!owner.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.Array)
		{
			return [];
		}

		return [.. element.EnumerateArray()
			.Where(static value => value.ValueKind == JsonValueKind.String)
			.Select(static value => value.GetString() ?? string.Empty)];
	}

	private static bool TryGetString(JsonElement owner, string propertyName, out string value)
	{
		value = string.Empty;
		if (!owner.TryGetProperty(propertyName, out JsonElement element) || element.ValueKind != JsonValueKind.String)
		{
			return false;
		}

		value = element.GetString() ?? string.Empty;
		return true;
	}

	private static bool TryGetInt32(JsonElement owner, string propertyName, out int value)
	{
		value = 0;
		return owner.TryGetProperty(propertyName, out JsonElement element) &&
			element.ValueKind == JsonValueKind.Number &&
			element.TryGetInt32(out value);
	}

	private static string GetString(JsonElement owner, string propertyName)
		=> TryGetString(owner, propertyName, out string value) ? value : string.Empty;

	private static int GetInt32(JsonElement owner, string propertyName)
		=> TryGetInt32(owner, propertyName, out int value) ? value : 0;

	private static long GetInt64(JsonElement owner, string propertyName)
		=> owner.TryGetProperty(propertyName, out JsonElement element) &&
			element.ValueKind == JsonValueKind.Number &&
			element.TryGetInt64(out long value)
				? value
				: 0;

	private static bool GetBoolean(JsonElement owner, string propertyName)
		=> owner.TryGetProperty(propertyName, out JsonElement element) &&
			element.ValueKind is JsonValueKind.True or JsonValueKind.False &&
			element.GetBoolean();

	private static void ThrowIfNullOrWhiteSpace(string value, string parameterName)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
		}
	}
}

public sealed class ProjectDataBuildSubmissionRecord
{
	public int SubmissionId { get; set; }
	public string Phase { get; set; } = string.Empty;
	public bool MSBuildIsRestoring { get; set; }
	public string[] EntryProjects { get; set; } = [];
	public string[] TargetNames { get; set; } = [];
	public ProjectDataBuildEventContextRecord? Context { get; set; }
}

public sealed class ProjectDataBuildContextRecord
{
	public string Kind { get; set; } = string.Empty;
	public string ProjectFilePath { get; set; } = string.Empty;
	public ProjectDataBuildEventContextRecord? Context { get; set; }
	public ProjectDataBuildEventContextRecord? ParentContext { get; set; }
}

public sealed class ProjectDataBuildDiagnosticRecord
{
	public const string FileProjectPathSource = "File";
	public const string ProjectFileProjectPathSource = "ProjectFile";
	public const string ContextProjectPathSource = "Context";
	public const string UnknownProjectPathSource = "Unknown";

	public string Severity { get; set; } = string.Empty;
	public string Phase { get; set; } = string.Empty;
	public string ProjectFilePath { get; set; } = string.Empty;
	public string ProjectFilePathSource { get; set; } = UnknownProjectPathSource;
	public string FilePath { get; set; } = string.Empty;
	public string Code { get; set; } = string.Empty;
	public string Message { get; set; } = string.Empty;
	public int Line { get; set; }
	public int Column { get; set; }
	public ProjectDataBuildEventContextRecord? Context { get; set; }
}

public sealed class ProjectDataBuildEventContextRecord
{
	public int NodeId { get; set; }
	public int ProjectContextId { get; set; }
	public int ProjectInstanceId { get; set; }
	public int TargetId { get; set; }
	public int TaskId { get; set; }
	public int SubmissionId { get; set; }
	public int EvaluationId { get; set; }
	public long BuildRequestId { get; set; }
}
