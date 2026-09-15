// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Thin MSBuild wrapper around <see cref="ProjectDataWriter"/>: declares the MSBuild
/// inputs as properties, calls the writer, and swallows any exception so a writer
/// fault never fails the build.
/// </summary>
public sealed class WriteProjectDataSliceTask : Microsoft.Build.Utilities.Task
{
	[Required]
	public string ProjectFilePath { get; set; } = string.Empty;

	/// <summary>
	/// Absolute path of the cache file. When empty, the user-folder cache path is
	/// computed from <see cref="ProjectFilePath"/> via <see cref="UserFolderCachePath"/>.
	/// </summary>
	public string OutputPath { get; set; } = string.Empty;

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

	public bool WriteHeader { get; set; }
	public bool IsPrimary { get; set; }
	public bool LastDtbSucceeded { get; set; }

	/// <summary>
	/// True when the invoking build forced <c>CoreCompile</c> to run so that the compiler command
	/// line is always captured. The authoritative <c>ProjectDataBuild</c> graph sets
	/// <c>NonExistentFile</c> in <c>_PrepareProjectDataBuild</c> for exactly this reason, so an
	/// empty <see cref="CommandLineArguments"/> there genuinely means the project produces no C#
	/// compilation and should be marked unsupported.
	/// <para/>
	/// When false — for example the opportunistic <c>EnableProjectDataOnBuild</c> hook running on an
	/// ordinary incremental build — an empty <see cref="CommandLineArguments"/> instead means
	/// <c>CoreCompile</c> was skipped as up-to-date (its <c>AfterTargets</c> hook still fires). That
	/// is a perfectly good project, so the task must leave any existing project-data file and
	/// unsupported marker untouched rather than poison the shared cache with a spurious
	/// <c>CompilerCommandLineArgumentsEmpty</c> marker.
	/// </summary>
	public bool CoreCompileForced { get; set; }

	public ITaskItem[]? SliceDimensions { get; set; }
	public ITaskItem[]? Properties { get; set; }
	public string[]? CommandLineArguments { get; set; }
	public ITaskItem[]? SourceFiles { get; set; }
	public ITaskItem[]? MetadataReferences { get; set; }
	public ITaskItem[]? AnalyzerReferences { get; set; }
	public string[]? AnalyzerConfigFiles { get; set; }
	public string[]? AdditionalFiles { get; set; }
	public ITaskItem[]? EmbeddedResources { get; set; }
	public ITaskItem[]? ProjectReferences { get; set; }
	public string[]? Capabilities { get; set; }
	public ITaskItem[]? SdkKnownAnalyzerPacks { get; set; }
	public ITaskItem[]? SdkAnalyzerConfigPolicy { get; set; }

	[Output]
	public bool Succeeded { get; set; }

	[Output]
	public string ResolvedOutputPath { get; set; } = string.Empty;

	public override bool Execute()
	{
		try
		{
			this.Succeeded = false;
			this.ResolvedOutputPath = this.ResolveOutputPath();

			if (!ProjectDataWriter.TryValidateNetFrameworkReferences(
				this.ProjectFilePath,
				this.SliceDimensions,
				this.Properties,
				this.MetadataReferences,
				out string unsupportedReason))
			{
				if (this.ShouldDeleteOutputOnValidationFailure())
				{
					this.DeleteFileIfExists(this.ResolvedOutputPath);
				}

				if (this.IsPrimary)
				{
					UnsupportedProjectDataMarker.Write(this.ProjectFilePath, unsupportedReason);
				}

				this.Log.LogMessage(MessageImportance.Low,
					"ProjectData: skipped writing project-data file for {0}: {1}.", this.ProjectFilePath, unsupportedReason);
				return true;
			}

			if (this.CommandLineArguments == null || this.CommandLineArguments.Length == 0)
			{
				// An empty compiler command line is only authoritative when the build forced
				// CoreCompile to run (the ProjectDataBuild graph). On an ordinary incremental build,
				// the EnableProjectDataOnBuild hook fires AfterTargets="CoreCompile" even when
				// CoreCompile was skipped as up-to-date, leaving @(CscCommandLineArgs) empty for a
				// perfectly good project. Poisoning the shared cache with an unsupported marker (or
				// deleting the good project-data file) in that case makes projects silently vanish on
				// the next non-forced workspace refresh. Leave existing state untouched instead.
				if (!this.CoreCompileForced)
				{
					this.Log.LogMessage(MessageImportance.Low,
						"ProjectData: leaving project-data for {0} untouched; CscCommandLineArgs was empty but CoreCompile was not forced (likely skipped as up-to-date).",
						this.ProjectFilePath);
					return true;
				}

				if (this.ShouldDeleteOutputOnValidationFailure())
				{
					this.DeleteFileIfExists(this.ResolvedOutputPath);
				}

				if (this.IsPrimary)
				{
					UnsupportedProjectDataMarker.Write(this.ProjectFilePath, "CompilerCommandLineArgumentsEmpty");
				}

				this.Log.LogMessage(MessageImportance.Low,
					"ProjectData: skipped writing project-data file for {0} because CscCommandLineArgs was empty.",
					this.ProjectFilePath);
				return true;
			}

			ProjectDataWriter.AtomicWriteStreamed(
				this.ResolvedOutputPath,
				writer => ProjectDataWriter.WriteContent(
					writer,
					this.ProjectFilePath,
					this.WriteHeader,
					this.IsPrimary,
					this.LastDtbSucceeded,
					this.SliceDimensions,
					this.Properties,
					this.CommandLineArguments,
					this.SourceFiles,
					this.MetadataReferences,
					this.AnalyzerReferences,
					this.AnalyzerConfigFiles,
					this.AdditionalFiles,
					this.EmbeddedResources,
					this.ProjectReferences,
					this.Capabilities,
					this.SdkKnownAnalyzerPacks,
					this.SdkAnalyzerConfigPolicy,
					this.ReportDuplicateItem),
				this.IntermediateOutputPath);
			this.Succeeded = true;
			UnsupportedProjectDataMarker.Delete(this.ProjectFilePath);
			this.RecordDonorIndexEntryIfFinalCache();

			this.Log.LogMessage(MessageImportance.Low,
				"ProjectData: wrote {0}.", this.ResolvedOutputPath);
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
				"ProjectData: failed to write project-data file for {0}: {1}",
				this.ProjectFilePath,
				ex.Message);
		}
		return true;
	}

	private string ResolveOutputPath()
		=> string.IsNullOrEmpty(this.OutputPath)
			? UserFolderCachePath.Compute(this.ProjectFilePath)
			: this.OutputPath;

	private void RecordDonorIndexEntryIfFinalCache()
	{
		if (!this.WriteHeader || !this.IsPrimary)
		{
			return;
		}

		this.RecordDonorIndexEntry();
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

	private void ReportDuplicateItem(ProjectDataDuplicateItemDiagnostic diagnostic)
	{
		this.Log.LogWarning(
			"ProjectData: duplicate {0} item in {1}: {2}. The duplicate entry was omitted from {3}.",
			diagnostic.Section,
			diagnostic.ProjectFilePath,
			diagnostic.ItemSpec,
			this.ResolvedOutputPath);
	}

	private bool ShouldDeleteOutputOnValidationFailure()
	{
		if (!this.IsPrimary)
		{
			return true;
		}

		string projectFolderOutputPath = Path.GetFullPath(this.ProjectFilePath) + ".lscache";
		return !PathsEqual(this.ResolvedOutputPath, projectFolderOutputPath);
	}

	private static bool PathsEqual(string left, string right)
		=> string.Equals(
			Path.GetFullPath(left),
			Path.GetFullPath(right),
			StringComparisons.Paths);

	private void DeleteFileIfExists(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		try
		{
			File.Delete(path);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			this.Log.LogWarning("ProjectData: failed to delete stale output {0}: {1}", path, ex.Message);
		}
	}
}
