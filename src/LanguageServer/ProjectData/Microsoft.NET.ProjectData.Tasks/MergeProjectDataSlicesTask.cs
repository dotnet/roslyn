// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Thin MSBuild wrapper around <see cref="ProjectDataMerger"/>: runs once on the
/// outer multi-TFM build (after <c>DispatchToInnerBuilds</c>), collects all slice
/// files matching <see cref="SliceGlob"/>, merges them into <see cref="OutputPath"/>
/// with structural deduplication, and swallows any exception so a merge fault
/// never fails the build.
/// </summary>
public sealed class MergeProjectDataSlicesTask : Microsoft.Build.Utilities.Task
{
	/// <summary>
	/// Absolute path of the merged cache file. When empty, the user-folder cache
	/// path is computed from <see cref="ProjectFilePath"/> via
	/// <see cref="UserFolderCachePath"/>.
	/// </summary>
	public string OutputPath { get; set; } = string.Empty;

	public string SliceGlob { get; set; } = string.Empty;

	public ITaskItem[]? SliceFiles { get; set; }

	public bool PreserveExistingSlices { get; set; }

	/// <summary>
	/// Semicolon-separated <c>TargetFrameworks</c> list from the outer build, used
	/// to select the primary slice for merged multi-TFM cache files.
	/// </summary>
	public string TargetFrameworks { get; set; } = string.Empty;

	/// <summary>
	/// Required when <see cref="OutputPath"/> is empty so the user-folder layout
	/// can be derived for this project.
	/// </summary>
	public string ProjectFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Project intermediate output directory (<c>$(IntermediateOutputPath)</c>). Transient
	/// <c>.tmp</c> side-files for the atomic write are placed here (when on the same volume as the
	/// output) so they never appear next to committed source. Optional; falls back to the output
	/// directory.
	/// </summary>
	public string IntermediateOutputPath { get; set; } = string.Empty;

	/// <summary>
	/// Optional override for the repo-scoped donor index path. When empty, the task resolves
	/// <c>&lt;git-common-dir&gt;\dotnet-projectdata\lscache-donor-index.json</c> from <see cref="ProjectFilePath"/>.
	/// </summary>
	public string DonorCacheIndexPath { get; set; } = string.Empty;

	/// <summary>
	/// Optional override for the logical workspace root recorded in the donor index.
	/// </summary>
	public string DonorCacheWorkspaceRoot { get; set; } = string.Empty;

	[Output]
	public bool Succeeded { get; set; }

	[Output]
	public bool FoundSlices { get; set; }

	[Output]
	public string ResolvedOutputPath { get; set; } = string.Empty;

	public override bool Execute()
	{
		if (string.IsNullOrEmpty(this.OutputPath) && string.IsNullOrEmpty(this.ProjectFilePath))
		{
			// Misconfigured task invocation — neither input is supplied so we cannot
			// determine where to write. Fail loud (Error) rather than swallow into the
			// catch-all below, which is reserved for runtime IO/parse failures.
			this.Log.LogError(
				"MergeProjectDataSlicesTask requires either OutputPath or ProjectFilePath to be specified.");
			return false;
		}

		try
		{
			this.Succeeded = false;
			this.FoundSlices = false;
			this.ResolvedOutputPath = this.ResolveOutputPath();

			int count;
			if (this.SliceFiles is { Length: > 0 })
			{
				string[] sliceFiles = this.SliceFiles
					.Select(item => item.ItemSpec)
					.Where(path => !string.IsNullOrWhiteSpace(path))
					.ToArray();
				string[] existing = sliceFiles.Where(File.Exists).ToArray();
				string[] missing = sliceFiles.Except(existing, StringComparer.OrdinalIgnoreCase).ToArray();
				if (missing.Length > 0)
				{
					this.Log.LogMessage(MessageImportance.Low,
						"ProjectData: skipping missing slices for {0}: {1}",
						this.ProjectFilePath,
						string.Join(";", missing));
				}

				if (existing.Length == 0)
				{
					this.DeleteOutputPathIfNotProjectFolder();
					return true;
				}

				this.FoundSlices = true;
				count = ProjectDataMerger.Merge(this.ResolvedOutputPath, existing, this.TargetFrameworks, this.PreserveExistingSlices, this.IntermediateOutputPath);
			}
			else
			{
				this.FoundSlices = true;
				string[] sliceFiles = ProjectDataMerger.FindSlices(this.SliceGlob).ToArray();
				if (sliceFiles.Length == 0)
				{
					this.FoundSlices = false;
					count = 0;
				}
				else
				{
					count = ProjectDataMerger.Merge(this.ResolvedOutputPath, sliceFiles, this.TargetFrameworks, this.PreserveExistingSlices, this.IntermediateOutputPath);
				}
			}

			if (count == 0)
			{
				this.DeleteOutputPathIfNotProjectFolder();
				this.Log.LogMessage(MessageImportance.Low, "ProjectData: no slice files found matching {0}; skipping merge.", this.SliceGlob);
			}
			else
			{
				this.Succeeded = true;
				UnsupportedProjectDataMarker.Delete(this.ProjectFilePath);
				this.RecordDonorIndexEntry();
				this.Log.LogMessage(MessageImportance.Low, "ProjectData: merged {0} slices into {1}.", count, this.ResolvedOutputPath);
			}
		}
		catch (Exception ex)
		{
			// Cache-write failures should not break the user's build, but they must be
			// visible at default verbosity so the user knows the cache might be stale.
			// ``LogMessage(Low)`` was invisible under ``-v:minimal`` (the default for
			// ``dotnet build``) and hid real diagnostics; ``LogWarning`` matches the
			// .NET SDK convention for non-fatal task failures. Catch all exception
			// types — narrowing previously caused legitimate DTB scenarios (e.g. a
			// ``KeyNotFoundException`` from a missing slice property) to fail the
			// build instead of degrading to a stale-cache warning.
			this.Log.LogWarning(
				"ProjectData: failed to merge slices for {0}: {1}",
				string.IsNullOrEmpty(this.OutputPath) ? this.ProjectFilePath : this.OutputPath,
				ex.Message);
		}
		return true;
	}

	private string ResolveOutputPath()
	{
		if (!string.IsNullOrEmpty(this.OutputPath))
		{
			return this.OutputPath;
		}
		if (string.IsNullOrEmpty(this.ProjectFilePath))
		{
			throw new InvalidOperationException(
				"MergeProjectDataSlicesTask: either OutputPath or ProjectFilePath must be supplied.");
		}
		return UserFolderCachePath.Compute(this.ProjectFilePath);
	}

	private void RecordDonorIndexEntry()
	{
		ProjectDataDonorWriteOptions options = new()
		{
			IndexPath = string.IsNullOrEmpty(this.DonorCacheIndexPath) ? null : this.DonorCacheIndexPath,
			WorkspaceRoot = string.IsNullOrEmpty(this.DonorCacheWorkspaceRoot) ? null : this.DonorCacheWorkspaceRoot,
		};

		bool recorded = ProjectDataDonorIndex.TryRecordWrite(this.ProjectFilePath, this.ResolvedOutputPath, options, out string? message);
		if (recorded && !string.IsNullOrEmpty(message))
		{
			this.Log.LogMessage(MessageImportance.Low, "ProjectData: {0}", message);
		}
		else if (!recorded && !string.IsNullOrEmpty(message))
		{
			this.Log.LogMessage(MessageImportance.Low, "ProjectData: failed to update donor index for {0}: {1}", this.ProjectFilePath, message);
		}
	}

	private void DeleteOutputPathIfNotProjectFolder()
	{
		if (string.IsNullOrEmpty(this.ResolvedOutputPath) ||
			PathsEqual(this.ResolvedOutputPath, Path.GetFullPath(this.ProjectFilePath) + ".lscache"))
		{
			return;
		}

		try
		{
			File.Delete(this.ResolvedOutputPath);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			this.Log.LogWarning("ProjectData: failed to delete stale output {0}: {1}", this.ResolvedOutputPath, ex.Message);
		}
	}

	private static bool PathsEqual(string left, string right)
		=> string.Equals(
			Path.GetFullPath(left),
			Path.GetFullPath(right),
			StringComparisons.Paths);
}
