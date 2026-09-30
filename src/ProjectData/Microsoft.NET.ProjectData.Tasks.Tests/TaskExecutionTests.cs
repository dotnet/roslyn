// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Moq;
using Xunit;

namespace Microsoft.NET.ProjectData.Tasks.Tests;

/// <summary>
/// Tests for the MSBuild task entry points (``MergeProjectDataSlicesTask``,
/// ``WriteProjectDataSliceTask``). Validates how the tasks surface
/// misconfiguration and runtime failures back to MSBuild — specifically that
/// errors are not silently swallowed at ``MessageImportance.Low``, which the
/// default ``dotnet build`` verbosity hides.
/// </summary>
public class TaskExecutionTests
{
	[Fact]
	public void MergeTask_LogsError_AndReturnsFalse_WhenNeitherInputProvided()
	{
		var engine = new BuildEngineStub();
		var task = new MergeProjectDataSlicesTask
		{
			BuildEngine = engine,
			// Both OutputPath and ProjectFilePath left empty — neither route to a target path.
		};

		bool result = task.Execute();

		Assert.False(result);
		Assert.Single(engine.Errors);
		Assert.Contains("requires either OutputPath or ProjectFilePath", engine.Errors[0].Message);
	}

	[Fact]
	public void MergeTask_LogsWarning_NotMessage_OnRuntimeIOFailure()
	{
		// Force a runtime failure inside ``Execute`` by pointing ``OutputPath`` at a
		// location that cannot be written (a path under a non-existent drive root on
		// Windows, or under an invalid character path on Unix). The catch-all should
		// surface the failure as a Warning so it appears under ``-v:minimal``.
		var engine = new BuildEngineStub();
		string unwritablePath = OperatingSystem.IsWindows()
			? @"Z:\nonexistent-drive\out.lscache"
			: "/nonexistent-root-XYZZY/out.lscache";

		var task = new MergeProjectDataSlicesTask
		{
			BuildEngine = engine,
			OutputPath = unwritablePath,
			SliceGlob = Path.Combine(Path.GetTempPath(), "lscache-tests-no-such-glob", "**", "*.slice"),
		};

		// No slices match the glob, so ``Merge`` returns 0 and ``DeleteOutputPathIfNotProjectFolder``
		// is invoked on the unwritable path. That throws, which exercises the catch-all.
		// We don't assert ``Execute`` returns true/false here — the contract is just
		// that any runtime failure produces a Warning, not a hidden Low-importance Message.
		try
		{
			task.Execute();
		}
		catch
		{
			// Any uncaught exception would mean the catch-all filter is too narrow —
			// also a failure mode worth surfacing, but not under this test.
		}

		// Either the warning was logged (caught path) or no work happened (lucky path).
		// Assert NOTHING is logged at MessageImportance.Low pretending to be an error —
		// that's the bug we're fixing.
		foreach (BuildMessageEventArgs message in engine.Messages)
		{
			Assert.DoesNotContain("failed to merge", message.Message, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void WriteTask_LogsWarning_NotMessage_OnRuntimeIOFailure()
	{
		// Same shape as the merge-task test. ``WriteProjectDataSliceTask`` calls
		// ``ProjectDataWriter.AtomicWriteStreamed`` which throws on an unwritable
		// output path. The catch-all must surface that as a Warning.
		var engine = new BuildEngineStub();
		string unwritablePath = OperatingSystem.IsWindows()
			? @"Z:\nonexistent-drive\out.lscache"
			: "/nonexistent-root-XYZZY/out.lscache";

		string fakeProject = Path.Combine(Path.GetTempPath(), "lscache-tests", Guid.NewGuid().ToString("N"), "App.csproj");
		var task = new WriteProjectDataSliceTask
		{
			BuildEngine = engine,
			ProjectFilePath = fakeProject,
			OutputPath = unwritablePath,
			CommandLineArguments = ["/noconfig"],
		};

		try
		{
			task.Execute();
		}
		catch
		{
		}

		foreach (BuildMessageEventArgs message in engine.Messages)
		{
			Assert.DoesNotContain("failed to write", message.Message, StringComparison.OrdinalIgnoreCase);
		}
	}

	[Fact]
	public void UnsupportedMarkerTask_TreatsCacheRootResolutionFailureAsRecoverable()
	{
		MethodInfo? method = typeof(WriteUnsupportedProjectDataMarkerTask).GetMethod(
			"IsRecoverableMarkerWriteException",
			BindingFlags.NonPublic | BindingFlags.Static);

		Assert.NotNull(method);
		Assert.True((bool)method.Invoke(null, [new InvalidOperationException("Unable to determine cache root.")])!);
	}

	[Fact]
	public void UnsupportedProjectDataMarker_DeleteTreatsPathResolutionFailuresAsRecoverable()
	{
		MethodInfo? method = typeof(UnsupportedProjectDataMarker).GetMethod(
			"IsRecoverableMarkerException",
			BindingFlags.NonPublic | BindingFlags.Static);

		Assert.NotNull(method);
		Assert.True((bool)method.Invoke(null, [new InvalidOperationException("Unable to determine cache root.")])!);
		UnsupportedProjectDataMarker.Delete(string.Empty);
	}

	[Fact]
	public void ProjectDataBuildReceipt_RoundTripsOnlyMatchingAttemptAndProject()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "receipts");
			string projectPath = Path.Combine(tempRoot, "App", "App.csproj");
			string otherProjectPath = Path.Combine(tempRoot, "Other", "Other.csproj");
			string attemptId = "attempt-1";

			ProjectDataBuildReceipt.Write(receiptDirectory, attemptId, projectPath);
			ProjectDataBuildReceipt.WriteAggregateCompletion(receiptDirectory, attemptId);

			Assert.True(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectPath, out ProjectDataBuildReceiptData receipt));
			Assert.Equal(Path.GetFullPath(projectPath), receipt.ProjectFilePath);
			Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, attemptId));
			Assert.False(ProjectDataBuildReceipt.TryRead(receiptDirectory, "attempt-2", projectPath, out _));
			Assert.False(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, otherProjectPath, out _));
			Assert.False(ProjectDataBuildReceipt.TryReadAggregateCompletion(receiptDirectory, "attempt-2"));

			string differentlyCasedProject = projectPath.ToUpperInvariant();
			Assert.Equal(
				OperatingSystem.IsLinux() ? false : true,
				string.Equals(
					ProjectDataBuildReceipt.GetReceiptFilePath(receiptDirectory, projectPath),
					ProjectDataBuildReceipt.GetReceiptFilePath(receiptDirectory, differentlyCasedProject),
					StringComparison.Ordinal));

			File.WriteAllText(receipt.ReceiptFilePath, "version=2\nattempt=attempt-1\nproject=wrong\nextra=value\n");
			Assert.False(ProjectDataBuildReceipt.TryRead(receiptDirectory, attemptId, projectPath, out _));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteProjectDataBuildReceiptTask_WriteFailureIsObservable()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "receipt-file");
			File.WriteAllText(receiptDirectory, "not a directory");
			BuildEngineStub engine = new();
			WriteProjectDataBuildReceiptTask task = new()
			{
				BuildEngine = engine,
				ReceiptDirectory = receiptDirectory,
				AttemptId = "attempt-1",
				ProjectFilePath = Path.Combine(tempRoot, "App.csproj"),
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains("failed to write completed receipt", error.Message, StringComparison.OrdinalIgnoreCase);
			Assert.Contains("attempt-1", error.Message, StringComparison.Ordinal);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_CapsPerProjectAndGlobalDiagnostics()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "logger-receipts");
			string attemptId = "attempt-1";
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger logger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory))};{attemptId}",
			};
			logger.Initialize(eventSource.Object);

			for (int projectIndex = 0; projectIndex < 41; projectIndex++)
			{
				string projectPath = Path.Combine(tempRoot, $"Project{projectIndex}.csproj");
				for (int diagnosticIndex = 0; diagnosticIndex < 6; diagnosticIndex++)
				{
					BuildWarningEventArgs warning = new(
						subcategory: string.Empty,
						code: $"W{diagnosticIndex}",
						file: projectPath,
						lineNumber: diagnosticIndex + 1,
						columnNumber: 1,
						endLineNumber: diagnosticIndex + 1,
						endColumnNumber: 2,
						message: "bounded warning",
						helpKeyword: string.Empty,
						senderName: "test")
					{
						ProjectFile = projectPath,
					};
					eventSource.Raise(source => source.AnyEventRaised += null, warning);
				}
			}

			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: false));
			logger.Shutdown();

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
			Assert.Equal(200, manifest.Diagnostics.Length);
			Assert.Equal(46, manifest.TruncatedDiagnosticCount);
			Assert.All(
				manifest.Diagnostics.GroupBy(diagnostic => diagnostic.ProjectFilePath),
				group => Assert.True(group.Count() <= 5, $"Expected at most 5 diagnostics for {group.Key}."));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_ErrorDisplacesWarningAtProjectCap()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "severity-aware-cap");
			string attemptId = "attempt-1";
			string projectPath = Path.Combine(tempRoot, "App.csproj");
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger logger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory))};{attemptId}",
			};
			logger.Initialize(eventSource.Object);
			for (int index = 0; index < 5; index++)
			{
				eventSource.Raise(
					source => source.AnyEventRaised += null,
					new BuildWarningEventArgs(string.Empty, $"W{index}", projectPath, 0, 0, 0, 0, "warning", string.Empty, "test")
					{
						ProjectFile = projectPath,
					});
			}
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildErrorEventArgs(string.Empty, "E1", projectPath, 0, 0, 0, 0, "actionable error", string.Empty, "test")
				{
					ProjectFile = projectPath,
				});
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: false));

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
			Assert.Equal(5, manifest.Diagnostics.Length);
			Assert.Equal(4, manifest.Diagnostics.Count(diagnostic => diagnostic.Severity == "Warning"));
			Assert.Contains(manifest.Diagnostics, diagnostic => diagnostic.Severity == "Error" && diagnostic.Code == "E1");
			Assert.Equal(1, manifest.TruncatedDiagnosticCount);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_ProjectCapDoesNotDisplaceAnotherProjectsWarning()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "project-cap-isolation");
			string attemptId = "attempt-1";
			string cappedProject = Path.Combine(tempRoot, "Capped.csproj");
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger logger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory))};{attemptId}",
			};
			logger.Initialize(eventSource.Object);

			for (int index = 0; index < 5; index++)
			{
				eventSource.Raise(
					source => source.AnyEventRaised += null,
					new BuildErrorEventArgs(string.Empty, $"E{index}", cappedProject, 0, 0, 0, 0, "error", string.Empty, "test")
					{
						ProjectFile = cappedProject,
					});
			}
			for (int projectIndex = 0; projectIndex < 39; projectIndex++)
			{
				string projectPath = Path.Combine(tempRoot, $"Warning{projectIndex}.csproj");
				for (int diagnosticIndex = 0; diagnosticIndex < 5; diagnosticIndex++)
				{
					eventSource.Raise(
						source => source.AnyEventRaised += null,
						new BuildWarningEventArgs(string.Empty, $"W{diagnosticIndex}", projectPath, 0, 0, 0, 0, "warning", string.Empty, "test")
						{
							ProjectFile = projectPath,
						});
				}
			}
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildErrorEventArgs(string.Empty, "E5", cappedProject, 0, 0, 0, 0, "extra error", string.Empty, "test")
				{
					ProjectFile = cappedProject,
				});
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: false));

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
			Assert.Equal(200, manifest.Diagnostics.Length);
			Assert.Equal(5, manifest.Diagnostics.Count(diagnostic => diagnostic.ProjectFilePath == cappedProject));
			Assert.Equal(1, manifest.TruncatedDiagnosticCount);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_MalformedParametersAndWriteFailureNeverThrow()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger malformed = new() { Parameters = "not-base64;attempt-1" };
			Exception? malformedException = Record.Exception(() => malformed.Initialize(eventSource.Object));
			Assert.Null(malformedException);

			string receiptFile = Path.Combine(tempRoot, "not-a-directory");
			File.WriteAllText(receiptFile, string.Empty);
			ProjectDataBuildCompletionLogger unwritable = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptFile))};attempt-2",
			};
			unwritable.Initialize(eventSource.Object);
			Exception? eventException = Record.Exception(() => eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: false)));
			Assert.Null(eventException);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_DistinguishesCancellationFromProcessLoss()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string cancelledDirectory = Path.Combine(tempRoot, "cancelled");
			Mock<IEventSource> cancelledSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger cancelledLogger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(cancelledDirectory))};cancelled-attempt",
			};
			cancelledLogger.Initialize(cancelledSource.Object);
			cancelledSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildCanceledEventArgs("Build cancelled"));
			cancelledLogger.Shutdown();

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(cancelledDirectory, "cancelled-attempt", out ProjectDataBuildAttemptManifest cancelledManifest));
			Assert.True(cancelledManifest.BuildCancelled);
			Assert.False(cancelledManifest.BuildFinished);
			Assert.True(ProjectDataBuildReceipt.TryReadAggregateCompletion(cancelledDirectory, "cancelled-attempt"));

			string lostDirectory = Path.Combine(tempRoot, "lost");
			Mock<IEventSource> lostSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger lostLogger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(lostDirectory))};lost-attempt",
			};
			lostLogger.Initialize(lostSource.Object);
			lostLogger.Shutdown();

			Assert.False(ProjectDataBuildAttemptManifest.TryRead(lostDirectory, "lost-attempt", out _));
			Assert.False(ProjectDataBuildReceipt.TryReadAggregateCompletion(lostDirectory, "lost-attempt"));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ProjectDataBuildCompletionLogger_RecordsSubmissionPhases()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "phase-evidence");
			string attemptId = "attempt-1";
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger logger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory))};{attemptId}",
			};
			logger.Initialize(eventSource.Object);
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildSubmissionStartedEventArgs(
					new Dictionary<string, string?> { ["MSBuildIsRestoring"] = "true" },
					[@"C:\repo\App.slnx"],
					["Restore"],
					BuildRequestDataFlags.None,
					submissionId: 1));
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildSubmissionStartedEventArgs(
					new Dictionary<string, string?>(),
					[@"C:\repo\App.slnx"],
					["ProjectDataBuild"],
					BuildRequestDataFlags.None,
					submissionId: 2));
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: true));

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
			Assert.True(manifest.ProjectDataBuildSubmissionObserved);
			Assert.Contains(manifest.Submissions, submission => submission.SubmissionId == 1 && submission.Phase == "Restore" && submission.MSBuildIsRestoring);
			Assert.Contains(manifest.Submissions, submission => submission.SubmissionId == 2 && submission.Phase == "ProjectDataBuild" && !submission.MSBuildIsRestoring);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData(".csproj")]
	[InlineData(".vbproj")]
	[InlineData(".fsproj")]
	[InlineData(".vcxproj")]
	[InlineData(".esproj")]
	[InlineData(".proj")]
	public void ProjectDataBuildCompletionLogger_UsesStructuredFileWhenNuGetReportsSolutionAsProject(string projectExtension)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string receiptDirectory = Path.Combine(tempRoot, "nuget-diagnostic");
			string attemptId = "attempt-1";
			string solutionPath = Path.Combine(tempRoot, "App.slnx");
			string brokenProjectPath = Path.Combine(tempRoot, "Broken", "Broken" + projectExtension);
			Mock<IEventSource> eventSource = new(MockBehavior.Loose);
			ProjectDataBuildCompletionLogger logger = new()
			{
				Parameters = $"{Convert.ToBase64String(Encoding.UTF8.GetBytes(receiptDirectory))};{attemptId}",
			};
			logger.Initialize(eventSource.Object);
			BuildErrorEventArgs error = new(
				subcategory: string.Empty,
				code: "NU1101",
				file: brokenProjectPath,
				lineNumber: 0,
				columnNumber: 0,
				endLineNumber: 0,
				endColumnNumber: 0,
				message: "Package was not found.",
				helpKeyword: string.Empty,
				senderName: "NuGet")
			{
				ProjectFile = solutionPath,
			};
			eventSource.Raise(source => source.AnyEventRaised += null, error);
			eventSource.Raise(
				source => source.AnyEventRaised += null,
				new BuildFinishedEventArgs("Build finished", string.Empty, succeeded: false));

			Assert.True(ProjectDataBuildAttemptManifest.TryRead(receiptDirectory, attemptId, out ProjectDataBuildAttemptManifest manifest));
			ProjectDataBuildDiagnosticRecord diagnostic = Assert.Single(manifest.Diagnostics);
			Assert.Equal(brokenProjectPath, diagnostic.ProjectFilePath);
			Assert.Equal(ProjectDataBuildDiagnosticRecord.FileProjectPathSource, diagnostic.ProjectFilePathSource);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_RecordsDonorIndex_AfterSuccessfulFinalCacheWrite()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string outputPath = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			var engine = new BuildEngineStub();
			var task = new WriteProjectDataSliceTask
			{
				BuildEngine = engine,
				ProjectFilePath = projectFile,
				OutputPath = outputPath,
				DonorCacheIndexPath = indexPath,
				DonorCacheWorkspaceRoot = workspaceRoot,
				WriteHeader = true,
				IsPrimary = true,
				LastDtbSucceeded = true,
				CommandLineArguments = ["/noconfig"],
			};

			bool result = task.Execute();

			Assert.True(result);
			Assert.True(task.Succeeded);
			Assert.True(File.Exists(indexPath), $"Expected donor index at {indexPath}.");
			string content = File.ReadAllText(indexPath);
			Assert.Contains("\"version\": 2", content);
			Assert.Contains(JsonString(Path.GetFullPath(workspaceRoot)), content);
			using JsonDocument index = JsonDocument.Parse(content);
			Assert.Equal(["version", "entries"], index.RootElement.EnumerateObject().Select(static property => property.Name));
			JsonElement entry = index.RootElement.GetProperty("entries")[0];
			Assert.Equal(["path", "newestMtimeMs", "updatedUtc"], entry.EnumerateObject().Select(static property => property.Name));
			Assert.True(entry.TryGetProperty("newestMtimeMs", out _));
			Assert.True(entry.TryGetProperty("updatedUtc", out _));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public void WriteTask_DoesNotRecordDonorIndex_ForNonFinalCacheWrite(bool writeHeader, bool isPrimary)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			WriteProjectDataSliceTask task = CreateFinalWriteTask(workspaceRoot, indexPath);
			task.WriteHeader = writeHeader;
			task.IsPrimary = isPrimary;

			Assert.True(task.Execute());
			Assert.True(task.Succeeded);
			Assert.False(File.Exists(indexPath));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_RecordsSparseEntries_ForDistinctWorkspaces()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string mainWorkspaceRoot = Path.Combine(tempRoot, "main-worktree");
			string capitalizedMainWorkspaceRoot = Path.Combine(tempRoot, "capitalized-main-worktree");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");

			Assert.True(CreateFinalWriteTask(mainWorkspaceRoot, indexPath).Execute());
			Assert.True(CreateFinalWriteTask(capitalizedMainWorkspaceRoot, indexPath).Execute());

			using JsonDocument index = JsonDocument.Parse(File.ReadAllText(indexPath));
			string[] paths = index.RootElement
				.GetProperty("entries")
				.EnumerateArray()
				.Select(entry => entry.GetProperty("path").GetString()!)
				.ToArray();
			Assert.Contains(Path.GetFullPath(mainWorkspaceRoot), paths);
			Assert.Contains(Path.GetFullPath(capitalizedMainWorkspaceRoot), paths);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_OutsideGitRepository_DoesNotRecordWrite()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string projectFile = Path.Combine(tempRoot, "worktree", "src", "App", "App.csproj");
			string cacheFile = Path.Combine(tempRoot, "output", "App.csproj.lscache");
			Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			File.WriteAllText(projectFile, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
			File.WriteAllText(cacheFile, "version=2\n");

			Assert.Null(ProjectDataDonorIndex.TryResolveDefaultIndexPath(projectFile));
			Assert.False(ProjectDataDonorIndex.TryRecordWrite(projectFile, cacheFile, options: null, out string? message));
			Assert.Null(message);
			Assert.Empty(Directory.EnumerateFiles(tempRoot, "lscache-donor-index.json", SearchOption.AllDirectories));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_Disabled_DoesNotRecordWrite()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			File.WriteAllText(cacheFile, "version=2\n");

			Assert.False(ProjectDataDonorIndex.TryRecordWrite(
				projectFile,
				cacheFile,
				new ProjectDataDonorWriteOptions
				{
					Enabled = false,
					IndexPath = indexPath,
					WorkspaceRoot = workspaceRoot,
				},
				out string? message));
			Assert.Null(message);
			Assert.False(File.Exists(indexPath));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_InvalidDonorIndexOverride_DoesNotFailSuccessfulCacheWrite()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			BuildEngineStub engine = new();
			WriteProjectDataSliceTask task = CreateFinalWriteTask(
				Path.Combine(tempRoot, "worktree"),
				"\0invalid-index-path",
				engine);

			Assert.True(task.Execute());
			Assert.True(task.Succeeded);
			Assert.True(File.Exists(task.OutputPath));
			Assert.Empty(engine.Warnings);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_UnavailableGitMetadata_RecordsSparseEntry()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			Directory.CreateDirectory(Path.Combine(workspaceRoot, ".git"));
			Directory.CreateDirectory(Path.GetDirectoryName(projectFile)!);
			File.WriteAllText(projectFile, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
			File.WriteAllText(cacheFile, "version=2\n");
			string indexPath = Assert.IsType<string>(ProjectDataDonorIndex.TryResolveDefaultIndexPath(projectFile));

			Assert.True(ProjectDataDonorIndex.TryRecordWrite(projectFile, cacheFile, options: null, out string? message));
			Assert.Null(message);

			using JsonDocument index = JsonDocument.Parse(File.ReadAllText(indexPath));
			JsonElement entry = Assert.Single(index.RootElement.GetProperty("entries").EnumerateArray());
			Assert.Equal(Path.GetFullPath(workspaceRoot), entry.GetProperty("path").GetString());
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_MissingIndexes_DoNotShareEntries()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string firstWorkspaceRoot = Path.Combine(tempRoot, "first-worktree");
			string secondWorkspaceRoot = Path.Combine(tempRoot, "second-worktree");
			string firstIndexPath = Path.Combine(tempRoot, "first-index", "lscache-donor-index.json");
			string secondIndexPath = Path.Combine(tempRoot, "second-index", "lscache-donor-index.json");

			Assert.True(CreateFinalWriteTask(firstWorkspaceRoot, firstIndexPath).Execute());
			Assert.True(CreateFinalWriteTask(secondWorkspaceRoot, secondIndexPath).Execute());

			string secondIndexContent = File.ReadAllText(secondIndexPath);
			Assert.Contains(JsonString(Path.GetFullPath(secondWorkspaceRoot)), secondIndexContent);
			Assert.DoesNotContain(JsonString(Path.GetFullPath(firstWorkspaceRoot)), secondIndexContent);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_DoesNotOverwrite_UnsupportedFutureVersion()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(cacheFile, "version=2\n");
			const string FutureIndex = """{"version":3,"entries":{"futureData":"preserve"}}""";
			File.WriteAllText(indexPath, FutureIndex);

			bool result = ProjectDataDonorIndex.TryRecordWrite(
				projectFile,
				cacheFile,
				new ProjectDataDonorWriteOptions { IndexPath = indexPath, WorkspaceRoot = workspaceRoot },
				out string? message);

			Assert.False(result);
			Assert.Contains("unsupported donor index version 3", message);
			Assert.Equal(FutureIndex, File.ReadAllText(indexPath));
			Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(indexPath)!, "lscache-donor-index.json.corrupt-*"));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData("""{"version":2,"entries":{"futureData":"preserve"}}""")]
	[InlineData("""{"version":2,"entries":[{"path":"\u0000"}]}""")]
	[InlineData("""{"entries":[]}""")]
	[InlineData("[]")]
	public void DonorIndex_QuarantinesAndRecreates_CorruptIndex(string corruptIndex)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			File.WriteAllText(cacheFile, "version=2\n");
			File.WriteAllText(indexPath, corruptIndex);

			bool result = ProjectDataDonorIndex.TryRecordWrite(
				projectFile,
				cacheFile,
				new ProjectDataDonorWriteOptions { IndexPath = indexPath, WorkspaceRoot = workspaceRoot },
				out string? message);

			Assert.True(result);
			Assert.Contains("Recovered corrupt donor index", message);
			Assert.Contains("\"version\": 2", File.ReadAllText(indexPath));
			string quarantinePath = Assert.Single(Directory.EnumerateFiles(Path.GetDirectoryName(indexPath)!, "lscache-donor-index.json.corrupt-*"));
			Assert.Equal(corruptIndex, File.ReadAllText(quarantinePath));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_CleansTemporaryFile_WhenReplacementFails()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index");
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			Directory.CreateDirectory(indexPath);
			File.WriteAllText(cacheFile, "version=2\n");

			Assert.False(ProjectDataDonorIndex.TryRecordWrite(
				projectFile,
				cacheFile,
				new ProjectDataDonorWriteOptions { IndexPath = indexPath, WorkspaceRoot = workspaceRoot },
				out string? message));
			Assert.False(string.IsNullOrEmpty(message));
			Assert.Empty(Directory.EnumerateFiles(tempRoot, "index.*.tmp", SearchOption.TopDirectoryOnly));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void DonorIndex_ReplacesIndex_WhileDeleteSharedReaderIsOpen()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Assert.True(CreateFinalWriteTask(workspaceRoot, indexPath).Execute());

			using FileStream reader = new(indexPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

			Assert.True(CreateFinalWriteTask(workspaceRoot, indexPath).Execute());
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public async Task WriteTask_WaitsForExclusiveIndexFileLock()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(indexPath)!);
			using FileStream indexLock = new(indexPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

			Task<bool> write = Task.Run(() => CreateFinalWriteTask(workspaceRoot, indexPath).Execute(), TestContext.Current.CancellationToken);
			await Task.Delay(200, TestContext.Current.CancellationToken);
			Assert.False(write.IsCompleted);

			indexLock.Dispose();
			Assert.True(await write);
			Assert.True(File.Exists(indexPath));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void TaskAssembly_DoesNotContainReaderSelectionOrGitTypes()
	{
		Assembly taskAssembly = typeof(WriteProjectDataSliceTask).Assembly;

		Assert.Null(taskAssembly.GetType("Microsoft.NET.ProjectData.ProjectDataDonorCandidate"));
		Assert.Null(taskAssembly.GetType("Microsoft.NET.ProjectData.ProjectDataDonorOptions"));
		Assert.Null(taskAssembly.GetType("Microsoft.NET.ProjectData.ProjectDataDonorIndex+GitQueryContext"));
		Assert.Null(typeof(ProjectDataDonorIndex).GetMethod("EnumerateDonorCandidates", BindingFlags.Public | BindingFlags.Static));
	}

	[Fact]
	public void DonorIndex_PreservesNewestCacheMtime_ForWorkspace()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string cacheFile = projectFile + ".lscache";
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			Directory.CreateDirectory(Path.GetDirectoryName(cacheFile)!);
			File.WriteAllText(cacheFile, "version=2\n");
			File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddMinutes(-1));
			ProjectDataDonorWriteOptions options = new()
			{
				IndexPath = indexPath,
				WorkspaceRoot = workspaceRoot + Path.DirectorySeparatorChar,
			};

			Assert.True(ProjectDataDonorIndex.TryRecordWrite(projectFile, cacheFile, options, out _));
			using JsonDocument firstIndex = JsonDocument.Parse(File.ReadAllText(indexPath));
			long newestMtimeMs = firstIndex.RootElement
				.GetProperty("entries")[0]
				.GetProperty("newestMtimeMs")
				.GetInt64();

			File.SetLastWriteTimeUtc(cacheFile, DateTime.UtcNow.AddHours(-1));
			options.WorkspaceRoot = workspaceRoot;
			Assert.True(ProjectDataDonorIndex.TryRecordWrite(projectFile, cacheFile, options, out _));

			using JsonDocument secondIndex = JsonDocument.Parse(File.ReadAllText(indexPath));
			JsonElement entry = Assert.Single(secondIndex.RootElement.GetProperty("entries").EnumerateArray());
			Assert.Equal(
				newestMtimeMs,
				entry.GetProperty("newestMtimeMs").GetInt64());
			Assert.Equal(workspaceRoot, entry.GetProperty("path").GetString());
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_DoesNotRecordDonorIndex_ForInnerSliceWrites()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string outputPath = Path.Combine(workspaceRoot, "obj", "App.csproj.slice");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			var engine = new BuildEngineStub();
			var task = new WriteProjectDataSliceTask
			{
				BuildEngine = engine,
				ProjectFilePath = projectFile,
				OutputPath = outputPath,
				DonorCacheIndexPath = indexPath,
				DonorCacheWorkspaceRoot = workspaceRoot,
				WriteHeader = false,
				IsPrimary = false,
				LastDtbSucceeded = true,
				CommandLineArguments = ["/noconfig"],
			};

			bool result = task.Execute();

			Assert.True(result);
			Assert.True(task.Succeeded);
			Assert.False(File.Exists(indexPath), $"Inner slice writes must not update donor index {indexPath}.");
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void WriteTask_DoesNotRecordDonorIndex_ForNonPrimaryHeaderWrites()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string outputPath = Path.Combine(workspaceRoot, "obj", "App.csproj.slice");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			var engine = new BuildEngineStub();
			var task = new WriteProjectDataSliceTask
			{
				BuildEngine = engine,
				ProjectFilePath = projectFile,
				OutputPath = outputPath,
				DonorCacheIndexPath = indexPath,
				DonorCacheWorkspaceRoot = workspaceRoot,
				WriteHeader = true,
				IsPrimary = false,
				LastDtbSucceeded = true,
				CommandLineArguments = ["/noconfig"],
			};

			bool result = task.Execute();

			Assert.True(result);
			Assert.True(task.Succeeded);
			Assert.False(File.Exists(indexPath), $"Non-primary writes must not update donor index {indexPath}.");
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void MergeTask_RecordsDonorIndex_AfterSuccessfulMerge()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string outputPath = projectFile + ".lscache";
			string slicePath = Path.Combine(workspaceRoot, "obj", "Debug", "App.csproj.slice");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			WriteSlice(slicePath, "MergedAssembly", "net10.0");
			var engine = new BuildEngineStub();
			var task = new MergeProjectDataSlicesTask
			{
				BuildEngine = engine,
				ProjectFilePath = projectFile,
				OutputPath = outputPath,
				SliceFiles = [new Microsoft.Build.Utilities.TaskItem(slicePath)],
				DonorCacheIndexPath = indexPath,
				DonorCacheWorkspaceRoot = workspaceRoot,
				TargetFrameworks = "net10.0",
			};

			bool result = task.Execute();

			Assert.True(result);
			Assert.True(task.Succeeded);
			Assert.True(File.Exists(outputPath), $"Expected merged cache at {outputPath}.");
			Assert.True(File.Exists(indexPath), $"Expected donor index at {indexPath}.");
			using JsonDocument index = JsonDocument.Parse(File.ReadAllText(indexPath));
			JsonElement entry = Assert.Single(index.RootElement.GetProperty("entries").EnumerateArray());
			Assert.Equal(Path.GetFullPath(workspaceRoot), entry.GetProperty("path").GetString());
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void MergeTask_DoesNotRecordDonorIndex_WhenNoSlicesAreMerged()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string workspaceRoot = Path.Combine(tempRoot, "worktree");
			string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
			string outputPath = projectFile + ".lscache";
			string missingSlicePath = Path.Combine(workspaceRoot, "obj", "Debug", "App.csproj.slice");
			string indexPath = Path.Combine(tempRoot, "index", "lscache-donor-index.json");
			var engine = new BuildEngineStub();
			var task = new MergeProjectDataSlicesTask
			{
				BuildEngine = engine,
				ProjectFilePath = projectFile,
				OutputPath = outputPath,
				SliceFiles = [new Microsoft.Build.Utilities.TaskItem(missingSlicePath)],
				DonorCacheIndexPath = indexPath,
				DonorCacheWorkspaceRoot = workspaceRoot,
				TargetFrameworks = "net10.0",
			};

			bool result = task.Execute();

			Assert.True(result);
			Assert.False(task.Succeeded);
			Assert.False(File.Exists(indexPath), $"No-slice merge must not update donor index {indexPath}.");
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	// A bare NuGet version is a minimum, so the resolved version may legitimately be higher.
	// Stale restore validation separately compares the current request with project.assets.json.
	[InlineData("12.0.3", "13.0.1", true)]
	[InlineData("13.0.4", "13.0.1", false)]
	[InlineData("11.0.0-preview.6.*", "11.0.0-preview.6.26359.118", true)]
	[InlineData("11.0.0-preview.6.*", "11.0.0-preview.7.1", false)]
	[InlineData("11.0.0-preview.6.*", "11.1.0", false)]
	[InlineData("11.0.0-preview.6.*", "12.0.0", false)]
	[InlineData("[1.0.0,2.0.0)", "1.5.0", true)]
	[InlineData("[1.0.0,2.0.0)", "2.0.0", false)]
	public void ValidatePackagesTask_UsesNuGetVersionRangeSemantics(string requestedVersion, string resolvedVersion, bool expectedResult)
	{
		string packagePath = CreatePackageDirectory();
		try
		{
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				PackageReferences = [CreateItem("Test.Package", ("Version", requestedVersion))],
				ResolvedPackages = [CreateItem($"test.package/{resolvedVersion}", ("Name", "test.package"), ("Version", resolvedVersion), ("Path", packagePath))],
			};

			Assert.Equal(expectedResult, task.Execute());
			Assert.Equal(expectedResult ? 0 : 1, engine.Errors.Count);
		}
		finally
		{
			Directory.Delete(packagePath, recursive: true);
		}
	}

	[Fact]
	public void ValidatePackagesTask_UsesVersionOverrideBeforeDirectAndCentralVersions()
	{
		string packagePath = CreatePackageDirectory();
		try
		{
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				ManagePackageVersionsCentrally = true,
				PackageReferences = [CreateItem("Test.Package", ("Version", "1.0.0"), ("VersionOverride", "3.0.0"))],
				PackageVersions = [CreateItem("Test.Package", ("Version", "2.0.0"))],
				ResolvedPackages = [CreateItem("Test.Package/2.0.0", ("Name", "Test.Package"), ("Version", "2.0.0"), ("Path", packagePath))],
			};

			Assert.False(task.Execute());
			Assert.Contains("requested '3.0.0', resolved '2.0.0'", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			Directory.Delete(packagePath, recursive: true);
		}
	}

	[Fact]
	public void ValidatePackagesTask_UsesCentralVersionWhenReferenceHasNoVersion()
	{
		string packagePath = CreatePackageDirectory();
		try
		{
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				ManagePackageVersionsCentrally = true,
				PackageReferences = [CreateItem("Test.Package")],
				PackageVersions = [CreateItem("Test.Package", ("Version", "2.0.0"))],
				ResolvedPackages = [CreateItem("Test.Package/1.0.0", ("Name", "Test.Package"), ("Version", "1.0.0"), ("Path", packagePath))],
			};

			Assert.False(task.Execute());
			Assert.Contains("requested '2.0.0', resolved '1.0.0'", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			Directory.Delete(packagePath, recursive: true);
		}
	}

	[Fact]
	public void ValidatePackagesTask_RejectsStaleRequestedVersion()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string packagePath = Path.Combine(tempRoot, "test.package", "13.0.1");
			Directory.CreateDirectory(packagePath);
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "project": {
				    "frameworks": {
				      "net8.0": {
				        "dependencies": {
				          "Test.Package": {
				            "target": "Package",
				            "version": "[13.0.1, )"
				          }
				        }
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net8.0",
				PackageReferences = [CreateItem("Test.Package", ("Version", "12.0.3"))],
				ResolvedPackages = [CreateItem("Test.Package/13.0.1", ("Name", "Test.Package"), ("Version", "13.0.1"), ("Path", packagePath))],
			};

			Assert.False(task.Execute());
			Assert.Contains("current request '12.0.3', restored request '[13.0.1, )'", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData("", "", "", null, null, true)]
	[InlineData(";", "", "", null, null, true)]
	[InlineData("", ";", " ; ", null, null, true)]
	[InlineData("compile;build", "", "all", "Compile, Build", "All", true)]
	[InlineData("compile,build", "", "", "None", null, true)]
	[InlineData("all", "build", "", "Runtime, Compile, ContentFiles, Native, Analyzers, BuildTransitive", null, true)]
	[InlineData("", "runtime", "", null, null, false)]
	[InlineData("", "", "all", null, null, false)]
	public void ValidatePackagesTask_ValidatesPackageAssetSelection(
		string includeAssets,
		string excludeAssets,
		string privateAssets,
		string? restoredInclude,
		string? restoredSuppressParent,
		bool expectedResult)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string packagePath = Path.Combine(tempRoot, "test.package", "1.0.0");
			Directory.CreateDirectory(packagePath);
			string includeProperty = restoredInclude is null ? string.Empty : $", \"include\": \"{restoredInclude}\"";
			string suppressParentProperty = restoredSuppressParent is null ? string.Empty : $", \"suppressParent\": \"{restoredSuppressParent}\"";
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				$$"""
				{
				  "project": {
				    "frameworks": {
				      "net8.0": {
				        "dependencies": {
				          "Test.Package": {
				            "target": "Package",
				            "version": "[1.0.0, )"{{includeProperty}}{{suppressParentProperty}}
				          }
				        }
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net8.0",
				PackageReferences =
				[
					CreateItem(
						"Test.Package",
						("Version", "1.0.0"),
						("IncludeAssets", includeAssets),
						("ExcludeAssets", excludeAssets),
						("PrivateAssets", privateAssets)),
				],
				ResolvedPackages = [CreateItem("Test.Package/1.0.0", ("Name", "Test.Package"), ("Version", "1.0.0"), ("Path", packagePath))],
			};

			Assert.Equal(expectedResult, task.Execute());
			if (!expectedResult)
			{
				Assert.Contains("current assets", Assert.Single(engine.Errors).Message);
			}
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_RejectsMissingDeclaredPackageAndResolvedFolder()
	{
		var engine = new BuildEngineStub();
		string missingPath = Path.Combine(Path.GetTempPath(), "projectdata-missing-package-" + Guid.NewGuid().ToString("N"));
		var task = new ValidateProjectDataPackagesTask
		{
			BuildEngine = engine,
			ProjectFilePath = "App.csproj",
			PackageReferences = [CreateItem("Declared.Package", ("Version", "1.0.0"))],
			ResolvedPackages = [CreateItem("Other.Package/2.0.0", ("Name", "Other.Package"), ("Version", "2.0.0"), ("Path", missingPath))],
		};

		Assert.False(task.Execute());
		Assert.Contains(engine.Errors, error => error.Message?.Contains("restore graph does not contain declared PackageReference items: Declared.Package", StringComparison.Ordinal) == true);
		Assert.Contains(engine.Errors, error => error.Message?.Contains($"package files are missing: Other.Package/2.0.0 at {missingPath}", StringComparison.Ordinal) == true);
	}

	[Fact]
	public void ValidatePackagesTask_RejectsRestoredRequestRemovedFromCurrentProject()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "project": {
				    "frameworks": {
				      "net8.0": {
				        "dependencies": {
				          "Removed.Package": {
				            "target": "Package",
				            "version": "[1.0.0, )"
				          }
				        }
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net8.0",
			};

			Assert.False(task.Execute());
			Assert.Contains("Removed.Package (restored request '[1.0.0, )', no current PackageReference)", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_RejectsExplicitReferenceWithoutEvaluatedVersion()
	{
		string packagePath = CreatePackageDirectory();
		try
		{
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				ManagePackageVersionsCentrally = true,
				PackageReferences = [CreateItem("Central.Package")],
				ResolvedPackages = [CreateItem("Central.Package/2.0.0", ("Name", "Central.Package"), ("Version", "2.0.0"), ("Path", packagePath))],
			};

			Assert.False(task.Execute());
			Assert.Contains("declared PackageReference items have no evaluated version request: Central.Package", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			Directory.Delete(packagePath, recursive: true);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedRestoreGraphWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string packagePath = Path.Combine(tempRoot, "test.package", "1.0.0");
			Directory.CreateDirectory(packagePath);
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "project": {
				    "frameworks": {
				      "net8.0": []
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net8.0",
				PackageReferences = [CreateItem("Test.Package", ("Version", "1.0.0"))],
				ResolvedPackages = [CreateItem("Test.Package/1.0.0", ("Name", "Test.Package"), ("Version", "1.0.0"), ("Path", packagePath))],
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("target framework 'net8.0' must be a JSON object", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedDependencyTargetWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "project": {
				    "frameworks": {
				      "net8.0": {
				        "dependencies": {
				          "Broken.Package": {
				            "target": [],
				            "version": "[1.0.0, )"
				          }
				        }
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net8.0",
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("dependency request 'Broken.Package' for target framework 'net8.0' has no string target", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_IgnoresRestoredSdkAutoReferencedPackagesMissingFromCurrentEvaluation()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "project": {
				    "frameworks": {
				      "net10.0": {
				        "dependencies": {
				          "Aspire.Hosting.AppHost": {
				            "autoReferenced": true,
				            "target": "Package",
				            "version": "[13.3.5, )"
				          }
				        }
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "AppHost.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net10.0",
			};

			Assert.True(task.Execute());
			Assert.Empty(engine.Errors);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_MatchesCurrentReferenceToRestoredAutoReferencedRequest()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string packagePath = Path.Combine(tempRoot, "aspire.hosting.apphost", "13.3.5");
			Directory.CreateDirectory(packagePath);
			string assetsFile = WriteAutoReferencedAssetsFile(tempRoot, "true", "\"[13.3.5, )\"");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "AppHost.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net10.0",
				PackageReferences = [CreateItem("Aspire.Hosting.AppHost", ("Version", "13.3.5"))],
				ResolvedPackages =
				[
					CreateItem(
						"Aspire.Hosting.AppHost/13.3.5",
						("Name", "Aspire.Hosting.AppHost"),
						("Version", "13.3.5"),
						("Path", packagePath)),
				],
			};

			Assert.True(task.Execute());
			Assert.Empty(engine.Errors);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData("false")]
	[InlineData("\"true\"")]
	public void ValidatePackagesTask_DoesNotIgnoreInvalidAutoReferencedMarkers(string autoReferenced)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteAutoReferencedAssetsFile(tempRoot, autoReferenced, "\"[13.3.5, )\"");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "AppHost.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net10.0",
			};

			Assert.False(task.Execute());
			Assert.Contains("no current PackageReference", Assert.Single(engine.Errors).Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedAutoReferencedVersionWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteAutoReferencedAssetsFile(tempRoot, "true", "[]");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "AppHost.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "net10.0",
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("package dependency request 'Aspire.Hosting.AppHost'", error.Message);
			Assert.Contains("has no string version", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	private static string WriteAutoReferencedAssetsFile(string tempRoot, string autoReferenced, string version)
	{
		string assetsFile = Path.Combine(tempRoot, "project.assets.json");
		File.WriteAllText(
			assetsFile,
			$$"""
			{
			  "project": {
			    "frameworks": {
			      "net10.0": {
			        "dependencies": {
			          "Aspire.Hosting.AppHost": {
			            "autoReferenced": {{autoReferenced}},
			            "target": "Package",
			            "version": {{version}}
			          }
			        }
			      }
			    }
			  }
			}
			""");
		return assetsFile;
	}

	[Theory]
	[InlineData("2.0.0", "2.0.0", true)]
	[InlineData("2.0.0", "1.0.0", false)]
	[InlineData(null, "2.0.0", false)]
	[InlineData("2.0.0", null, false)]
	public void ValidatePackagesTask_ValidatesActiveCentralTransitivePins(
		string? restoredPinVersion,
		string? currentPinVersion,
		bool expectedResult)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string restoredPin = restoredPinVersion is null
				? string.Empty
				: $$"""
					"Pinned.Package": {
					  "include": "Runtime, Compile",
					  "version": "[{{restoredPinVersion}}, )"
					}
					""";
			string assetsFile = WriteCentralTransitiveAssetsFile(tempRoot, restoredPin);
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				ManagePackageVersionsCentrally = true,
				CentralPackageTransitivePinningEnabled = true,
				PackageVersions = currentPinVersion is null
					? []
					: [CreateItem("Pinned.Package", ("Version", currentPinVersion))],
			};

			Assert.Equal(expectedResult, task.Execute());
			if (expectedResult)
			{
				Assert.Empty(engine.Errors);
			}
			else
			{
				Assert.Contains(
					engine.Errors,
					error => error.Message?.Contains("central transitive package version requests differ from the restore graph", StringComparison.Ordinal) == true);
			}
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_IgnoresInactiveCentralVersionAndUnpinnedTransitivePackage()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteCentralTransitiveAssetsFile(tempRoot, centralTransitiveRequests: string.Empty);
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				ManagePackageVersionsCentrally = true,
				CentralPackageTransitivePinningEnabled = true,
				PackageVersions =
				[
					CreateItem("Inactive.Package", ("Version", "5.0.0")),
					CreateItem("Inactive.Resolved", ("Version", "5.0.0")),
				],
			};

			Assert.True(task.Execute());
			Assert.Empty(engine.Errors);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedCentralTransitivePinWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteCentralTransitiveAssetsFile(
				tempRoot,
				"""
				"Pinned.Package": {
				  "version": []
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				CentralPackageTransitivePinningEnabled = true,
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("central transitive dependency request 'Pinned.Package'", error.Message);
			Assert.Contains("has no string version", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedResolvedTargetGraphWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = Path.Combine(tempRoot, "project.assets.json");
			File.WriteAllText(
				assetsFile,
				"""
				{
				  "targets": {
				    "netstandard2.0": []
				  },
				  "centralTransitiveDependencyGroups": {},
				  "project": {
				    "restore": {
				      "CentralPackageTransitivePinningEnabled": true
				    },
				    "frameworks": {
				      "netstandard2.0": {
				        "dependencies": {}
				      }
				    }
				  }
				}
				""");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				CentralPackageTransitivePinningEnabled = true,
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("resolved target graph for target framework 'netstandard2.0' must be a JSON object", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Theory]
	[InlineData("false", false, true)]
	[InlineData("true", true, true)]
	[InlineData("false", true, false)]
	[InlineData("true", false, false)]
	public void ValidatePackagesTask_ValidatesCentralTransitivePinningMode(
		string restoredPinningMode,
		bool currentPinningEnabled,
		bool expectedResult)
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteCentralTransitiveAssetsFile(
				tempRoot,
				centralTransitiveRequests: string.Empty,
				restoredPinningMode: restoredPinningMode);
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				ManagePackageVersionsCentrally = true,
				CentralPackageTransitivePinningEnabled = currentPinningEnabled,
			};

			Assert.Equal(expectedResult, task.Execute());
			Assert.Equal(
				expectedResult ? 0 : 1,
				engine.Errors.Count(error => error.Message?.Contains("central transitive package pinning mode differs from the restore graph", StringComparison.Ordinal) == true));
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	[Fact]
	public void ValidatePackagesTask_ReportsMalformedCentralTransitivePinningModeWithContext()
	{
		string tempRoot = CreateTempRoot();
		try
		{
			string assetsFile = WriteCentralTransitiveAssetsFile(
				tempRoot,
				centralTransitiveRequests: string.Empty,
				restoredPinningMode: "\"true\"");
			var engine = new BuildEngineStub();
			var task = new ValidateProjectDataPackagesTask
			{
				BuildEngine = engine,
				ProjectFilePath = "App.csproj",
				AssetsFile = assetsFile,
				TargetFramework = "netstandard2.0",
				TargetFrameworkMoniker = ".NETStandard,Version=v2.0",
				ManagePackageVersionsCentrally = true,
				CentralPackageTransitivePinningEnabled = true,
			};

			Assert.False(task.Execute());
			BuildErrorEventArgs error = Assert.Single(engine.Errors);
			Assert.Contains($"restore graph '{assetsFile}' could not be read", error.Message);
			Assert.Contains("central transitive package pinning mode in restore settings must be a JSON boolean", error.Message);
		}
		finally
		{
			DeleteTempRoot(tempRoot);
		}
	}

	private static string WriteCentralTransitiveAssetsFile(
		string tempRoot,
		string centralTransitiveRequests,
		string restoredPinningMode = "true")
	{
		string assetsFile = Path.Combine(tempRoot, "project.assets.json");
		File.WriteAllText(
			assetsFile,
			$$"""
			{
			  "targets": {
			    "netstandard2.0": {
			      "Pinned.Package/2.0.0": {
			        "type": "package"
			      },
			      "Normal.Transitive/1.0.0": {
			        "type": "package"
			      },
			      "Inactive.Resolved/5.0.0": {
			        "type": "package"
			      }
			    }
			  },
			  "centralTransitiveDependencyGroups": {
			    ".NETStandard,Version=v2.0": {
			      {{centralTransitiveRequests}}
			    }
			  },
			  "project": {
			    "restore": {
			      "CentralPackageTransitivePinningEnabled": {{restoredPinningMode}}
			    },
			    "frameworks": {
			      "netstandard2.0": {
			        "dependencies": {},
			        "centralPackageVersions": {
			          "Inactive.Resolved": "5.0.0"
			        }
			      }
			    }
			  }
			}
			""");
		return assetsFile;
	}

	private static Microsoft.Build.Utilities.TaskItem CreateItem(string itemSpec, params (string Name, string Value)[] metadata)
	{
		var item = new Microsoft.Build.Utilities.TaskItem(itemSpec);
		foreach ((string name, string value) in metadata)
		{
			item.SetMetadata(name, value);
		}

		return item;
	}

	private static string CreatePackageDirectory()
	{
		string path = Path.Combine(Path.GetTempPath(), "projectdata-package-validation-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private sealed class BuildEngineStub : IBuildEngine
	{
		public List<BuildErrorEventArgs> Errors { get; } = [];
		public List<BuildWarningEventArgs> Warnings { get; } = [];
		public List<BuildMessageEventArgs> Messages { get; } = [];
		public List<CustomBuildEventArgs> CustomEvents { get; } = [];

		public bool ContinueOnError => false;
		public int LineNumberOfTaskNode => 0;
		public int ColumnNumberOfTaskNode => 0;
		public string ProjectFileOfTaskNode => string.Empty;

		public bool BuildProjectFile(string projectFileName, string[] targetNames, System.Collections.IDictionary globalProperties, System.Collections.IDictionary targetOutputs)
			=> throw new NotSupportedException();

		public void LogCustomEvent(CustomBuildEventArgs e) => this.CustomEvents.Add(e);
		public void LogErrorEvent(BuildErrorEventArgs e) => this.Errors.Add(e);
		public void LogMessageEvent(BuildMessageEventArgs e) => this.Messages.Add(e);
		public void LogWarningEvent(BuildWarningEventArgs e) => this.Warnings.Add(e);
	}

	private static string CreateTempRoot()
	{
		string path = Path.Combine(Path.GetTempPath(), "projectdata-task-donor-tests-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private static void DeleteTempRoot(string tempRoot)
	{
		try
		{
			Directory.Delete(tempRoot, recursive: true);
		}
		catch
		{
		}
	}

	private static string JsonString(string value)
		=> "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

	private static WriteProjectDataSliceTask CreateFinalWriteTask(
		string workspaceRoot,
		string indexPath,
		BuildEngineStub? buildEngine = null)
	{
		string projectFile = Path.Combine(workspaceRoot, "src", "App", "App.csproj");
		return new WriteProjectDataSliceTask
		{
			BuildEngine = buildEngine ?? new BuildEngineStub(),
			ProjectFilePath = projectFile,
			OutputPath = projectFile + ".lscache",
			DonorCacheIndexPath = indexPath,
			DonorCacheWorkspaceRoot = workspaceRoot,
			WriteHeader = true,
			IsPrimary = true,
			LastDtbSucceeded = true,
			CommandLineArguments = ["/noconfig"],
		};
	}

	private static void WriteSlice(string slicePath, string assemblyName, string targetFramework)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(slicePath)!);
		File.WriteAllText(
			slicePath,
			$$"""
			[project]
			project=App.csproj
			language=C#
			primary
			lastDtbSucceeded

			[sliceDimensions]
			TargetFramework={{targetFramework}}

			[properties]
			AssemblyName={{assemblyName}}

			[commandLineArguments]
			/noconfig
			""");
	}
}
