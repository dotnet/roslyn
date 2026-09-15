// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.Json;
using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Central MSBuild logger that records structured aggregate ProjectDataBuild evidence.
/// </summary>
public sealed class ProjectDataBuildCompletionLogger : ILogger
{
	private const int MaxDiagnosticsPerProject = 5;
	private const int MaxDiagnostics = 200;
	private const int MaxSubmissions = 1024;
	private const int MaxContexts = 20_000;

	private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	private static readonly JsonSerializerOptions SerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
	};

	private readonly object gate = new();
	private readonly List<ProjectDataBuildSubmissionRecord> submissions = [];
	private readonly List<ProjectDataBuildContextRecord> contexts = [];
	private readonly List<ProjectDataBuildDiagnosticRecord> diagnostics = [];
	private readonly Dictionary<string, string> projectByContext = new(StringComparer.Ordinal);
	private readonly Dictionary<int, string> phaseBySubmission = [];
	private readonly Dictionary<string, int> diagnosticCountByProject = new(StringComparer.OrdinalIgnoreCase);

	private string receiptDirectory = string.Empty;
	private string attemptId = string.Empty;
	private string latestPhase = "Unknown";
	private bool initialized;
	private bool buildFinished;
	private bool buildSucceeded;
	private bool buildCancelled;
	private bool projectDataBuildSubmissionObserved;
	private int truncatedDiagnosticCount;
	private int truncatedSubmissionCount;
	private int truncatedContextCount;
	private string completedUtc = string.Empty;
	private IEventSource? eventSource;

	public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;

	public string? Parameters { get; set; }

	public void Initialize(IEventSource eventSource)
	{
		try
		{
			if (eventSource is null)
			{
				throw new ArgumentNullException(nameof(eventSource));
			}
			if (!TryParseParameters(this.Parameters, out this.receiptDirectory, out this.attemptId))
			{
				TryWriteLoggerError("ProjectDataBuild completion logger parameters were invalid.");
				return;
			}

			this.initialized = true;
			this.eventSource = eventSource;
			eventSource.AnyEventRaised += this.OnAnyEventRaised;
		}
		catch (Exception ex)
		{
			TryWriteLoggerError($"ProjectDataBuild completion logger failed to initialize: {ex.Message}");
		}
	}

	public void Shutdown()
	{
		try
		{
			if (this.initialized && this.buildCancelled && !this.buildFinished)
			{
				this.WriteEvidence();
			}
			if (this.eventSource is not null)
			{
				this.eventSource.AnyEventRaised -= this.OnAnyEventRaised;
				this.eventSource = null;
			}
		}
		catch (Exception ex)
		{
			TryWriteLoggerError($"ProjectDataBuild completion logger failed during shutdown: {ex.Message}");
		}
	}

	private void OnAnyEventRaised(object sender, BuildEventArgs e)
	{
		_ = sender;
		try
		{
			lock (this.gate)
			{
				switch (e)
				{
					case BuildSubmissionStartedEventArgs submission:
						this.RecordSubmission(submission);
						break;
					case ProjectStartedEventArgs projectStarted:
						this.RecordProjectContext(projectStarted);
						break;
					case ProjectEvaluationStartedEventArgs evaluationStarted:
						this.RecordEvaluationContext(evaluationStarted);
						break;
					case BuildErrorEventArgs error:
						this.RecordDiagnostic(
							severity: "Error",
							error.ProjectFile,
							error.File,
							error.Code,
							error.Message,
							error.LineNumber,
							error.ColumnNumber,
							error.BuildEventContext,
							emitFrame: true);
						break;
					case BuildWarningEventArgs warning:
						this.RecordDiagnostic(
							severity: "Warning",
							warning.ProjectFile,
							warning.File,
							warning.Code,
							warning.Message,
							warning.LineNumber,
							warning.ColumnNumber,
							warning.BuildEventContext,
							emitFrame: false);
						break;
					case BuildCanceledEventArgs:
						this.buildCancelled = true;
						this.completedUtc = DateTimeOffset.UtcNow.ToString("O");
						this.WriteEvidence();
						break;
					case BuildFinishedEventArgs finished:
						this.buildFinished = true;
						this.buildSucceeded = finished.Succeeded;
						this.completedUtc = finished.Timestamp.ToUniversalTime().ToString("O");
						this.WriteEvidence();
						break;
				}
			}
		}
		catch (Exception ex)
		{
			TryWriteLoggerError($"ProjectDataBuild completion logger ignored an event failure: {ex.Message}");
		}
	}

	private void RecordSubmission(BuildSubmissionStartedEventArgs submission)
	{
		string[] targetNames = submission.TargetNames?.Where(static target => !string.IsNullOrWhiteSpace(target)).ToArray() ?? [];
		bool isRestoring = TryGetBooleanGlobalProperty(submission.GlobalProperties, "MSBuildIsRestoring");
		string phase = ClassifyPhase(isRestoring, targetNames);
		this.latestPhase = phase;
		this.projectDataBuildSubmissionObserved |= string.Equals(phase, "ProjectDataBuild", StringComparison.Ordinal);
		if (this.submissions.Count >= MaxSubmissions)
		{
			this.truncatedSubmissionCount++;
			return;
		}

		this.phaseBySubmission[submission.SubmissionId] = phase;
		this.submissions.Add(new ProjectDataBuildSubmissionRecord
		{
			SubmissionId = submission.SubmissionId,
			Phase = phase,
			MSBuildIsRestoring = isRestoring,
			EntryProjects = submission.EntryProjectsFullPath?.Where(static path => !string.IsNullOrWhiteSpace(path)).ToArray() ?? [],
			TargetNames = targetNames,
			Context = ConvertContext(submission.BuildEventContext),
		});
	}

	private void RecordProjectContext(ProjectStartedEventArgs projectStarted)
	{
		if (this.contexts.Count >= MaxContexts)
		{
			this.truncatedContextCount++;
			return;
		}

		string projectFile = projectStarted.ProjectFile ?? string.Empty;
		string contextKey = GetContextKey(projectStarted.BuildEventContext);
		if (contextKey.Length > 0 && projectFile.Length > 0)
		{
			this.projectByContext[contextKey] = projectFile;
		}

		this.contexts.Add(new ProjectDataBuildContextRecord
		{
			Kind = "Project",
			ProjectFilePath = projectFile,
			Context = ConvertContext(projectStarted.BuildEventContext),
			ParentContext = ConvertContext(projectStarted.ParentProjectBuildEventContext),
		});
	}

	private void RecordEvaluationContext(ProjectEvaluationStartedEventArgs evaluationStarted)
	{
		if (this.contexts.Count >= MaxContexts)
		{
			this.truncatedContextCount++;
			return;
		}

		string projectFile = evaluationStarted.ProjectFile ?? string.Empty;
		string contextKey = GetContextKey(evaluationStarted.BuildEventContext);
		if (contextKey.Length > 0 && projectFile.Length > 0)
		{
			this.projectByContext[contextKey] = projectFile;
		}

		this.contexts.Add(new ProjectDataBuildContextRecord
		{
			Kind = "Evaluation",
			ProjectFilePath = projectFile,
			Context = ConvertContext(evaluationStarted.BuildEventContext),
		});
	}

	private void RecordDiagnostic(
		string severity,
		string? projectFile,
		string? file,
		string? code,
		string? message,
		int line,
		int column,
		BuildEventContext? context,
		bool emitFrame)
	{
		string resolvedProjectFile;
		string projectFilePathSource;
		if (IsProjectFilePath(file))
		{
			resolvedProjectFile = file!;
			projectFilePathSource = ProjectDataBuildDiagnosticRecord.FileProjectPathSource;
		}
		else if (IsProjectFilePath(projectFile))
		{
			resolvedProjectFile = projectFile!;
			projectFilePathSource = ProjectDataBuildDiagnosticRecord.ProjectFileProjectPathSource;
		}
		else if (this.projectByContext.TryGetValue(GetContextKey(context), out string? contextProjectFile))
		{
			resolvedProjectFile = contextProjectFile;
			projectFilePathSource = ProjectDataBuildDiagnosticRecord.ContextProjectPathSource;
		}
		else
		{
			resolvedProjectFile = projectFile ?? string.Empty;
			projectFilePathSource = ProjectDataBuildDiagnosticRecord.UnknownProjectPathSource;
		}

		string diagnosticKey = resolvedProjectFile.Length == 0 ? "<global>" : resolvedProjectFile;
		this.diagnosticCountByProject.TryGetValue(diagnosticKey, out int projectDiagnosticCount);
		bool globalCapReached = this.diagnostics.Count >= MaxDiagnostics;
		bool projectCapReached = projectDiagnosticCount >= MaxDiagnosticsPerProject;
		int replacementIndex = -1;
		if (globalCapReached || projectCapReached)
		{
			if (string.Equals(severity, "Error", StringComparison.OrdinalIgnoreCase))
			{
				if (projectCapReached)
				{
					replacementIndex = this.diagnostics.FindLastIndex(existing =>
						string.Equals(existing.Severity, "Warning", StringComparison.OrdinalIgnoreCase) &&
						string.Equals(
							string.IsNullOrEmpty(existing.ProjectFilePath) ? "<global>" : existing.ProjectFilePath,
							diagnosticKey,
							StringComparison.OrdinalIgnoreCase));
				}

				if (replacementIndex < 0 && globalCapReached && !projectCapReached)
				{
					replacementIndex = this.diagnostics.FindLastIndex(static existing =>
						string.Equals(existing.Severity, "Warning", StringComparison.OrdinalIgnoreCase));
				}
			}

			this.truncatedDiagnosticCount++;
			if (replacementIndex < 0)
			{
				return;
			}
		}

		ProjectDataBuildDiagnosticRecord diagnostic = new()
		{
			Severity = severity,
			Phase = this.GetPhase(context),
			ProjectFilePath = resolvedProjectFile,
			ProjectFilePathSource = projectFilePathSource,
			FilePath = file ?? string.Empty,
			Code = code ?? string.Empty,
			Message = message ?? string.Empty,
			Line = line,
			Column = column,
			Context = ConvertContext(context),
		};

		if (replacementIndex >= 0)
		{
			ProjectDataBuildDiagnosticRecord replaced = this.diagnostics[replacementIndex];
			string replacedKey = string.IsNullOrEmpty(replaced.ProjectFilePath) ? "<global>" : replaced.ProjectFilePath;
			this.DecrementDiagnosticCount(replacedKey);
			this.diagnostics[replacementIndex] = diagnostic;
			this.diagnosticCountByProject.TryGetValue(diagnosticKey, out int replacementProjectDiagnosticCount);
			this.diagnosticCountByProject[diagnosticKey] = replacementProjectDiagnosticCount + 1;
		}
		else
		{
			this.diagnosticCountByProject[diagnosticKey] = projectDiagnosticCount + 1;
			this.diagnostics.Add(diagnostic);
		}

		if (emitFrame)
		{
			this.EmitProvisionalDiagnostic(diagnostic);
		}
	}

	private void EmitProvisionalDiagnostic(ProjectDataBuildDiagnosticRecord diagnostic)
	{
		try
		{
			Console.Error.WriteLine(ProjectDataBuildDiagnosticProtocol.Encode(this.attemptId, diagnostic));
		}
		catch (Exception ex)
		{
			TryWriteLoggerError($"ProjectDataBuild completion logger failed to emit a provisional diagnostic: {ex.Message}");
		}
	}

	private void DecrementDiagnosticCount(string diagnosticKey)
	{
		if (!this.diagnosticCountByProject.TryGetValue(diagnosticKey, out int count) || count <= 1)
		{
			this.diagnosticCountByProject.Remove(diagnosticKey);
			return;
		}

		this.diagnosticCountByProject[diagnosticKey] = count - 1;
	}

	private string GetPhase(BuildEventContext? context)
	{
		if (context is not null && this.phaseBySubmission.TryGetValue(context.SubmissionId, out string? phase))
		{
			return phase;
		}

		return this.latestPhase;
	}

	private void WriteEvidence()
	{
		if (!this.initialized)
		{
			return;
		}

		try
		{
			Directory.CreateDirectory(this.receiptDirectory);
			ProjectDataBuildAttemptManifest manifest = new()
			{
				AttemptId = this.attemptId,
				BuildFinished = this.buildFinished,
				BuildSucceeded = this.buildSucceeded,
				BuildCancelled = this.buildCancelled,
				ProjectDataBuildSubmissionObserved = this.projectDataBuildSubmissionObserved,
				CompletedUtc = this.completedUtc,
				TruncatedDiagnosticCount = this.truncatedDiagnosticCount,
				TruncatedSubmissionCount = this.truncatedSubmissionCount,
				TruncatedContextCount = this.truncatedContextCount,
				Submissions = [.. this.submissions],
				Contexts = [.. this.contexts],
				Diagnostics = [.. this.diagnostics],
			};
			string manifestPath = ProjectDataBuildAttemptManifest.GetManifestFilePath(this.receiptDirectory);
			WriteJsonAtomically(manifestPath, JsonSerializer.Serialize(manifest, SerializerOptions));
			ProjectDataBuildReceipt.WriteAggregateCompletion(this.receiptDirectory, this.attemptId);
		}
		catch (Exception ex)
		{
			TryWriteLoggerError($"ProjectDataBuild completion logger failed to write evidence: {ex.Message}");
		}
	}

	private static bool TryParseParameters(string? parameters, out string receiptDirectory, out string attemptId)
	{
		receiptDirectory = string.Empty;
		attemptId = string.Empty;
		if (string.IsNullOrWhiteSpace(parameters))
		{
			return false;
		}

		string[] parts = parameters!.Split(';');
		if (parts.Length != 2)
		{
			return false;
		}

		try
		{
			receiptDirectory = Utf8NoBom.GetString(Convert.FromBase64String(parts[0]));
			attemptId = parts[1];
			return receiptDirectory.Length > 0 && attemptId.Length > 0;
		}
		catch (FormatException)
		{
			return false;
		}
	}

	private static string ClassifyPhase(bool isRestoring, IReadOnlyList<string> targetNames)
	{
		if (isRestoring || targetNames.Any(static target => string.Equals(target, "Restore", StringComparison.OrdinalIgnoreCase)))
		{
			return "Restore";
		}

		if (targetNames.Any(static target => string.Equals(target, "ProjectDataBuild", StringComparison.OrdinalIgnoreCase)))
		{
			return "ProjectDataBuild";
		}

		return "Unknown";
	}

	private static bool TryGetBooleanGlobalProperty(IReadOnlyDictionary<string, string?>? properties, string name)
	{
		if (properties is null)
		{
			return false;
		}

		if (!properties.TryGetValue(name, out string? value))
		{
			value = properties
				.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
				.Value;
		}

		return bool.TryParse(value, out bool result) && result;
	}

	private static ProjectDataBuildEventContextRecord? ConvertContext(BuildEventContext? context)
		=> context is null
			? null
			: new ProjectDataBuildEventContextRecord
			{
				NodeId = context.NodeId,
				ProjectContextId = context.ProjectContextId,
				ProjectInstanceId = context.ProjectInstanceId,
				TargetId = context.TargetId,
				TaskId = context.TaskId,
				SubmissionId = context.SubmissionId,
				EvaluationId = context.EvaluationId,
				BuildRequestId = context.BuildRequestId,
			};

	private static string GetContextKey(BuildEventContext? context)
		=> context is null
			? string.Empty
			: $"{context.NodeId}:{context.ProjectContextId}:{context.ProjectInstanceId}:{context.SubmissionId}:{context.EvaluationId}:{context.BuildRequestId}";

	private static bool IsProjectFilePath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}

		string extension = Path.GetExtension(path);
		return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) ||
			extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase) ||
			extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase) ||
			extension.Equals(".vcxproj", StringComparison.OrdinalIgnoreCase) ||
			extension.Equals(".esproj", StringComparison.OrdinalIgnoreCase) ||
			extension.Equals(".proj", StringComparison.OrdinalIgnoreCase);
	}

	private static void WriteJsonAtomically(string path, string content)
	{
		string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
		try
		{
			File.WriteAllText(tempPath, content, Utf8NoBom);
			if (File.Exists(path))
			{
				File.Replace(tempPath, path, destinationBackupFileName: null);
			}
			else
			{
				File.Move(tempPath, path);
			}
		}
		finally
		{
			try
			{
				File.Delete(tempPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
			}
		}
	}

	private static void TryWriteLoggerError(string message)
	{
		try
		{
			Console.Error.WriteLine(message);
		}
		catch
		{
		}
	}
}
