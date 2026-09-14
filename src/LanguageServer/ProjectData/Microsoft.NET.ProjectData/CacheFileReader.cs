// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Identifies the location selected while reading a ProjectData cache.
/// </summary>
public enum ProjectDataCacheSource
{
	None,
	ProjectFolder,
	UserFolder,
	UnsupportedMarker,
	Donor,
}

/// <summary>
/// Contains cached ProjectData slices and the location selected by the cache reader.
/// </summary>
public readonly struct ProjectDataCacheReadResult
{
	public ProjectDataCacheReadResult(ImmutableArray<CachedSliceData> slices, ProjectDataCacheSource source)
	{
		this.Slices = slices;
		this.Source = source;
	}

	public ImmutableArray<CachedSliceData> Slices { get; }

	public ProjectDataCacheSource Source { get; }
}

/// <summary>
/// Reads <c>.lscache</c> files and produces <see cref="CachedSliceData"/> for each project configuration slice.
/// </summary>
/// <remarks>Token-aware overloads propagate cancellation as <see cref="OperationCanceledException"/>.</remarks>
public static class CacheFileReader
{
	private const string CacheFileExtension = ".lscache";

	private const string VersionLinePrefix = "version=";
	private const int ProjectReferenceMetadataMinorVersion = 1;

	/// <summary>
	/// The wire-format version header is <c>version=&lt;major&gt;[.&lt;minor&gt;]</c>. The reader
	/// accepts any file whose MAJOR version matches the major it was built for, regardless of
	/// the minor: a newer minor only adds forward-compatible data (new sections, keys, or
	/// <c>@metadata</c>) that the parsing loop below ignores. A different major is rejected as
	/// a clean cache miss because the reader cannot safely interpret a format whose major it
	/// doesn't understand. Absence of a minor component (e.g. <c>version=2</c>) is treated as
	/// <c>.0</c>.
	/// </summary>
	internal static readonly int CurrentMajorVersion = ParseMajorVersionOrThrow(CacheFormat.VersionHeader);

	/// <summary>
	/// Gets the cache file path for a given project file path in project-folder mode.
	/// </summary>
	public static string GetProjectFolderCacheFilePath(string projectFilePath) => projectFilePath + CacheFileExtension;

	/// <summary>
	/// Gets the cache file path for a given project file path in user-folder mode.
	///
	/// <para>The path-computation algorithm lives in the shared
	/// <see cref="UserFolderCachePath"/> file (linked from the MSBuild task project) so the
	/// writer and reader cannot drift.</para>
	/// </summary>
	public static string GetUserFolderCacheFilePath(string projectFilePath) => UserFolderCachePath.Compute(projectFilePath);

	public static string GetCacheBaseDirectory() => UserFolderCachePath.GetCacheBaseDirectory();

	/// <summary>
	/// Reads cache data for a single project from its <c>.lscache</c> file.
	/// Checks the project folder, user folder, unsupported marker, and donor caches in precedence order.
	/// </summary>
	/// <param name="projectFilePath">The absolute path of the project file.</param>
	/// <param name="cacheInProject">When <see langword="true"/>, the cache file is stored beside the project file.</param>
	/// <param name="cancellationToken">A cancellation token.</param>
	/// <returns>The cached slices for the project, or an empty array if no cache exists or the file is invalid.</returns>
	/// <remarks>
	/// This overload constructs a <see cref="CachePathResolver"/> with no SDK
	/// binding. Cache files containing <c>&lt;NETSDK&gt;</c> entries cannot be
	/// resolved with this overload — use <see cref="ReadProjectCacheAsync(string, bool, CachePathResolver, CancellationToken)"/>
	/// and pass an SDK-bound resolver instead.
	/// </remarks>
	public static Task<ImmutableArray<CachedSliceData>> ReadProjectCacheAsync(
		string projectFilePath,
		bool cacheInProject,
		CancellationToken cancellationToken)
		=> ReadProjectCacheAsync(projectFilePath, cacheInProject, stringPool: null, cancellationToken);

	public static Task<ImmutableArray<CachedSliceData>> ReadProjectCacheAsync(
		string projectFilePath,
		bool cacheInProject,
		StringPool? stringPool,
		CancellationToken cancellationToken)
		=> ReadProjectCacheAsync(projectFilePath, cacheInProject, new CachePathResolver(), stringPool, cancellationToken);

	public static Task<ImmutableArray<CachedSliceData>> ReadProjectCacheAsync(
		string projectFilePath,
		bool cacheInProject,
		CachePathResolver resolver,
		CancellationToken cancellationToken)
		=> ReadProjectCacheAsync(projectFilePath, cacheInProject, resolver, stringPool: null, cancellationToken);

	/// <summary>
	/// Reads cache data for a single project and converts it to canonical immutable snapshots.
	/// </summary>
	public static Task<ImmutableArray<ProjectDataSnapshot>> ReadProjectDataSnapshotsAsync(
		string projectFilePath,
		bool cacheInProject,
		string? solutionPath,
		StringPool? stringPool,
		CancellationToken cancellationToken)
		=> ReadProjectDataSnapshotsAsync(projectFilePath, cacheInProject, new CachePathResolver(), solutionPath, stringPool, cancellationToken);

	/// <summary>
	/// Reads cache data for a single project with a caller-supplied resolver and converts it to canonical immutable snapshots.
	/// </summary>
	public static async Task<ImmutableArray<ProjectDataSnapshot>> ReadProjectDataSnapshotsAsync(
		string projectFilePath,
		bool cacheInProject,
		CachePathResolver resolver,
		string? solutionPath,
		StringPool? stringPool,
		CancellationToken cancellationToken)
	{
		ImmutableArray<CachedSliceData> slices = await ReadProjectCacheAsync(projectFilePath, cacheInProject, resolver, stringPool, cancellationToken).ConfigureAwait(false);
		ImmutableArray<ProjectDataSnapshot> snapshots = ProjectDataSnapshotFactory.CreateSnapshots(slices, solutionPath);
		cancellationToken.ThrowIfCancellationRequested();
		return snapshots;
	}

	/// <summary>
	/// Reads cache data for a single project from its <c>.lscache</c> file using
	/// a caller-supplied resolver. Use the SDK-bound overloads of
	/// <see cref="CachePathResolver"/> when reading caches that may contain
	/// <c>&lt;NETSDK&gt;</c> entries.
	/// </summary>
	public static Task<ImmutableArray<CachedSliceData>> ReadProjectCacheAsync(
		string projectFilePath,
		bool cacheInProject,
		CachePathResolver resolver,
		StringPool? stringPool,
		CancellationToken cancellationToken)
		=> ReadProjectCacheAsync(projectFilePath, cacheInProject, resolver, cancellationToken, stringPool);

	public static async Task<ImmutableArray<CachedSliceData>> ReadProjectCacheAsync(
		string projectFilePath,
		bool cacheInProject,
		CachePathResolver resolver,
		CancellationToken cancellationToken,
		StringPool? stringPool = null,
		ProjectDataDonorOptions? donorOptions = null)
	{
		ProjectDataCacheReadResult result = await ReadProjectCacheWithSourceAsync(
			projectFilePath,
			cacheInProject,
			resolver,
			stringPool,
			cancellationToken,
			donorOptions).ConfigureAwait(false);
		return result.Slices;
	}

	/// <summary>
	/// Reads cache data for a single project and reports the cache location selected by the reader.
	/// Existing slices-only overloads remain available for callers that do not need source attribution.
	/// </summary>
	public static async Task<ProjectDataCacheReadResult> ReadProjectCacheWithSourceAsync(
		string projectFilePath,
		bool cacheInProject,
		CachePathResolver resolver,
		StringPool? stringPool,
		CancellationToken cancellationToken,
		ProjectDataDonorOptions? donorOptions)
	{
		cancellationToken.ThrowIfCancellationRequested();

		try
		{
			// Always check the project folder first — a local cache takes precedence
			// regardless of the setting (handles migration and source-controlled caches).
			string projectFolderPath = GetProjectFolderCacheFilePath(projectFilePath);

			if (File.Exists(projectFolderPath))
			{
				ImmutableArray<CachedSliceData> slices = await ReadCacheFileAsync(
					projectFolderPath,
					projectFilePath,
					expectedProjectFilePath: null,
					resolver,
					stringPool,
					cancellationToken).ConfigureAwait(false);
				return new(slices, ProjectDataCacheSource.ProjectFolder);
			}

			bool userFolderAvailable = UserFolderCachePath.TryCompute(projectFilePath, out string userFolderPath);
			if (!userFolderAvailable)
			{
				System.Diagnostics.Trace.TraceWarning(
					"[lscache] User-folder cache root is unavailable for project {0}; continuing with marker and donor fallback.",
					projectFilePath);
			}
			else if (File.Exists(userFolderPath))
			{
				ImmutableArray<CachedSliceData> slices = await ReadCacheFileAsync(
					userFolderPath,
					projectFilePath,
					expectedProjectFilePath: projectFilePath,
					resolver,
					stringPool,
					cancellationToken).ConfigureAwait(false);
				return new(slices, ProjectDataCacheSource.UserFolder);
			}

			if (userFolderAvailable && UnsupportedProjectDataMarker.TryReadValid(projectFilePath, cancellationToken, out _))
			{
				return new([], ProjectDataCacheSource.UnsupportedMarker);
			}

			donorOptions ??= ProjectDataDonorOptions.Default;
			foreach (ProjectDataDonorCandidate donorCandidate in ProjectDataDonorIndex.EnumerateDonorCandidates(projectFilePath, donorOptions, cancellationToken))
			{
				cancellationToken.ThrowIfCancellationRequested();
				try
				{
					ImmutableArray<CachedSliceData> donorSlices = await ReadCacheFileAsync(donorCandidate.FilePath, projectFilePath, expectedProjectFilePath: projectFilePath, resolver, stringPool, cancellationToken).ConfigureAwait(false);
					if (!donorSlices.IsEmpty)
					{
						donorOptions.TraceDonorUsed(donorCandidate.WorkspaceRoot);
						return new(donorSlices, ProjectDataCacheSource.Donor);
					}
				}
				catch (Exception ex) when (IsRecoverableCacheReadException(ex))
				{
					donorOptions.TraceWarning(
						"[donor] Failed to read donor ProjectData file {0} for recipient project {1}: {2}",
						donorCandidate.FilePath,
						projectFilePath,
						ex.Message);
				}
			}

			return new([], ProjectDataCacheSource.None);
		}
		catch (Exception ex) when (IsRecoverableCacheReadException(ex))
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to read project cache for {0}: {1}",
				projectFilePath,
				ex.Message);
			return new([], ProjectDataCacheSource.None);
		}
	}

	private static bool IsRecoverableCacheReadException(Exception ex)
		=> ex is IOException or UnauthorizedAccessException or FormatException or ArgumentException or NotSupportedException or InvalidOperationException;

	/// <summary>
	/// Gets the path of the cache file that would be watched for a given project.
	/// Returns the project-folder path if it exists, otherwise the user-folder path.
	/// </summary>
	public static string GetCacheFilePathForWatching(string projectFilePath, bool cacheInProject)
	{
		string projectFolderPath = GetProjectFolderCacheFilePath(projectFilePath);

		if (File.Exists(projectFolderPath))
		{
			return projectFolderPath;
		}

		return GetUserFolderCacheFilePath(projectFilePath);
	}

	private static async Task<ImmutableArray<CachedSliceData>> ReadCacheFileAsync(
		string cacheFilePath,
		string projectFilePath,
		string? expectedProjectFilePath,
		CachePathResolver resolver,
		StringPool? stringPool,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		// DIAG: record the on-disk file size at the moment we open it so we can detect
		// "DTB succeeded but cache is empty/partially-written when reader opens it".
		long fileLength = -1;
		try { fileLength = new FileInfo(cacheFilePath).Length; } catch { }
		System.Diagnostics.Trace.TraceInformation(
			"[lscache-diag] Opening cache file {0} (size={1} bytes) for project {2} expected='{3}'",
			cacheFilePath,
			fileLength,
			projectFilePath,
			expectedProjectFilePath ?? "<null>");

		FileStream stream = OpenCacheFileForRead(cacheFilePath);
		await using (stream.ConfigureAwait(false))
		{
			using StreamReader reader = new(stream);
			string projectDirectory = Path.GetDirectoryName(projectFilePath)!;
			return await ReadFromAsync(reader, resolver, projectDirectory, projectFilePath, expectedProjectFilePath, stringPool, cancellationToken).ConfigureAwait(false);
		}
	}

	internal static FileStream OpenCacheFileForRead(string cacheFilePath)
		=> new(cacheFilePath, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read | FileShare.Delete,
			Options = FileOptions.Asynchronous,
		});

	private static int ParseMajorVersionOrThrow(string versionHeader)
		=> TryParseMajorVersion(versionHeader, out int major)
			? major
			: throw new InvalidOperationException(
				$"CacheFormat.VersionHeader '{versionHeader}' is not a valid 'version=<major>[.<minor>]' header.");

	/// <summary>
	/// Parses the MAJOR component from a <c>version=&lt;major&gt;[.&lt;minor&gt;]</c> header line.
	/// Returns <see langword="false"/> (and sets <paramref name="major"/> to <c>-1</c>) when the
	/// line is <see langword="null"/>, lacks the <c>version=</c> prefix, or has a non-numeric or
	/// non-positive major component.
	/// </summary>
	internal static bool TryParseMajorVersion(string? line, out int major)
	{
		major = -1;
		if (line is null || !line.StartsWith(VersionLinePrefix, StringComparison.Ordinal))
			return false;

		ReadOnlySpan<char> value = line.AsSpan(VersionLinePrefix.Length);
		int dot = value.IndexOf('.');
		ReadOnlySpan<char> majorSpan = dot >= 0 ? value[..dot] : value;
		if (!int.TryParse(majorSpan, out int parsed) || parsed <= 0)
			return false;

		major = parsed;
		return true;
	}

	private static bool HasAuthoritativeProjectReferenceDefaults(string? versionHeader)
	{
		if (versionHeader is null || !versionHeader.StartsWith(VersionLinePrefix, StringComparison.Ordinal))
		{
			return false;
		}

		ReadOnlySpan<char> value = versionHeader.AsSpan(VersionLinePrefix.Length);
		int dot = value.IndexOf('.');
		if (dot < 0 || !int.TryParse(value[(dot + 1)..], out int minor))
		{
			return false;
		}

		return minor >= ProjectReferenceMetadataMinorVersion;
	}

	/// <summary>
	/// Reads all project slices from the given reader, observing cancellation throughout parsing and materialization.
	/// </summary>
	/// <exception cref="OperationCanceledException">The cancellation token was canceled.</exception>
	public static async Task<ImmutableArray<CachedSliceData>> ReadFromAsync(
		TextReader reader,
		CachePathResolver resolver,
		string projectDirectory,
		string projectFilePath,
		string? expectedProjectFilePath,
		StringPool? stringPool,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(reader);
		ArgumentNullException.ThrowIfNull(resolver);
		ArgumentNullException.ThrowIfNull(projectDirectory);
		ArgumentNullException.ThrowIfNull(projectFilePath);
		cancellationToken.ThrowIfCancellationRequested();

		string? firstLineRaw = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		string? firstLine = firstLineRaw;

		bool hadHashHeader = false;
		if (firstLine is not null && firstLine.StartsWith(CacheFormat.HashHeaderPrefix, StringComparison.Ordinal))
		{
			hadHashHeader = true;
			firstLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
		}

		if (!TryParseMajorVersion(firstLine, out int fileMajorVersion) || fileMajorVersion != CurrentMajorVersion)
		{
			// A cache written by a different MAJOR version (or an unrecognized header) is a
			// clean cache miss. A newer MINOR (same major) is accepted: unknown sections,
			// keys, and metadata are ignored by the parsing loop below. Log enough context to
			// diagnose silent reader rejections in CI.
			System.Diagnostics.Trace.TraceWarning(
				"[lscache-diag] Unsupported cache version for {0}: first-raw='{1}' (len={2}) hadHashHeader={3} version-line='{4}' parsedMajor={5} expectedMajor={6}",
				projectFilePath,
				firstLineRaw ?? "<null>",
				firstLineRaw?.Length ?? -1,
				hadHashHeader,
				firstLine ?? "<null>",
				fileMajorVersion,
				CurrentMajorVersion);
			return [];
		}

		if (firstLine != CacheFormat.VersionHeader)
		{
			// Same MAJOR, different version-header text: a teammate on a different MINOR wrote this
			// file. It is safe to read — any sections, keys, or @metadata this reader does not
			// recognize are ignored by the loop below — so this is an informational signal about a
			// mixed-version team, not a warning. The guard keeps the common same-version read path
			// allocation-free (no params array / boxing unless the headers actually differ).
			System.Diagnostics.Trace.TraceInformation(
				"[lscache] Reading a cache written by a different minor version for {0}: file-version='{1}' reader-version='{2}'. Major matches; unrecognized additive data is ignored.",
				projectFilePath,
				firstLine,
				CacheFormat.VersionHeader);
		}

		bool hasAuthoritativeProjectReferenceDefaults = HasAuthoritativeProjectReferenceDefaults(firstLine);
		ImmutableArray<CachedSliceData>.Builder slices = ImmutableArray.CreateBuilder<CachedSliceData>();
		SliceBuilder? currentSlice = null;
		string? currentSection = null;
		string? declaredProjectFilePath = null;

		string? line;
		while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (line.Length == 0 || line[0] == CacheFormat.CommentChar)
			{
				continue;
			}

			if (line == CacheFormat.SliceSeparator)
			{
				if (currentSlice is not null)
				{
					slices.Add(currentSlice.Build(declaredProjectFilePath ?? projectFilePath));
				}
				currentSlice = null;
				currentSection = null;
				continue;
			}

			if (currentSection is null && line.StartsWith(CacheFormat.ProjectHeaderPrefix, StringComparison.Ordinal))
			{
				RecordDeclaredProjectFilePath(line[CacheFormat.ProjectHeaderPrefix.Length..], resolver, projectDirectory, ref declaredProjectFilePath);
				continue;
			}

			if (line[0] == '[' && line[^1] == ']')
			{
				currentSection = line[1..^1];

				if (currentSection == CacheFormat.Sections.Project)
				{
					currentSlice ??= new SliceBuilder(resolver, projectDirectory, stringPool, hasAuthoritativeProjectReferenceDefaults, cancellationToken);
				}

				continue;
			}

			currentSlice ??= new SliceBuilder(resolver, projectDirectory, stringPool, hasAuthoritativeProjectReferenceDefaults, cancellationToken);

			switch (currentSection)
			{
				case CacheFormat.Sections.Project:
					ParseProjectLine(currentSlice, line, resolver, projectDirectory, ref declaredProjectFilePath);
					break;
				case CacheFormat.Sections.SliceDimensions:
					currentSlice.AddSliceDimensionLine(line);
					break;
				case CacheFormat.Sections.Properties:
					currentSlice.AddPropertyLine(line);
					break;
				case CacheFormat.Sections.CommandLineArguments:
					currentSlice.AddCommandLineArgument(resolver.MakeAbsolute(line, projectDirectory));
					break;
				case CacheFormat.Sections.SourceFiles:
					currentSlice.SourceFileLines.Add(line);
					break;
				case CacheFormat.Sections.MetadataReferences:
					currentSlice.MetadataReferenceLines.Add(line);
					break;
				case CacheFormat.Sections.AnalyzerReferences:
					currentSlice.AnalyzerReferenceLines.Add(line);
					break;
				case CacheFormat.Sections.FrameworkPacks:
					currentSlice.FrameworkPacks.Add(line);
					break;
				case CacheFormat.Sections.SdkAnalyzerPacks:
					currentSlice.SdkAnalyzerPacks.Add(line);
					break;
				case CacheFormat.Sections.SdkAnalyzerConfigPolicy:
					currentSlice.SdkAnalyzerConfigPolicy.Add(line);
					break;
				case CacheFormat.Sections.AnalyzerConfigFiles:
					currentSlice.AnalyzerConfigFileLines.Add(line);
					break;
				case CacheFormat.Sections.AdditionalFiles:
					currentSlice.AdditionalFileLines.Add(line);
					break;
				case CacheFormat.Sections.EmbeddedResources:
					currentSlice.EmbeddedResourceLines.Add(line);
					break;
				case CacheFormat.Sections.ProjectReferences:
					currentSlice.ProjectReferenceLines.Add(line);
					break;
				case CacheFormat.Sections.Capabilities:
					currentSlice.AddCapability(line);
					break;
			}
		}

		cancellationToken.ThrowIfCancellationRequested();

		if (currentSlice is not null)
		{
			slices.Add(currentSlice.Build(declaredProjectFilePath ?? projectFilePath));
		}

		cancellationToken.ThrowIfCancellationRequested();

		if (expectedProjectFilePath is not null
			&& (declaredProjectFilePath is null
				|| !string.Equals(declaredProjectFilePath, expectedProjectFilePath, StringComparisons.Paths)))
		{
			// DIAG: cache file's project= line resolves to a different absolute path than the
			// caller expected. Should not happen when the cache was written for the same project.
			System.Diagnostics.Trace.TraceWarning(
				"[lscache-diag] Declared project path mismatch: declared='{0}' expected='{1}' projectFilePath='{2}' projectDirectory='{3}' slices.Count={4}",
				declaredProjectFilePath ?? "<null>",
				expectedProjectFilePath,
				projectFilePath,
				projectDirectory,
				slices.Count);
			return [];
		}

		// DIAG: log a successful read so we can correlate path/count in CI logs. A normal read is
		// an informational event, not a warning.
		if (slices.Count > 0)
		{
			System.Diagnostics.Trace.TraceInformation(
				"[lscache-diag] Read {0} slice(s) for {1} (declared='{2}', expected='{3}')",
				slices.Count,
				projectFilePath,
				declaredProjectFilePath ?? "<null>",
				expectedProjectFilePath ?? "<null>");
		}
		else
		{
			// 0 slices despite a header that passed every check is the "DTB succeeded but the cache
			// is empty/partially-written" anomaly the diagnostics were added to catch — keep it a warning.
			System.Diagnostics.Trace.TraceWarning(
				"[lscache-diag] Read 0 slices for {0} despite passing header check (declared='{1}', expected='{2}')",
				projectFilePath,
				declaredProjectFilePath ?? "<null>",
				expectedProjectFilePath ?? "<null>");
		}

		cancellationToken.ThrowIfCancellationRequested();
		return slices.ToImmutable();

		static void ParseProjectLine(
			SliceBuilder builder,
			string line,
			CachePathResolver resolver,
			string projectDirectory,
			ref string? declaredProjectFilePath)
		{
			if (line.StartsWith(CacheFormat.ProjectHeaderPrefix, StringComparison.Ordinal))
			{
				RecordDeclaredProjectFilePath(line[CacheFormat.ProjectHeaderPrefix.Length..], resolver, projectDirectory, ref declaredProjectFilePath);
			}
			else if (line.StartsWith(CacheFormat.LanguagePrefix, StringComparison.Ordinal))
			{
				builder.LanguageName = line[CacheFormat.LanguagePrefix.Length..];
			}
			else if (line == CacheFormat.PrimaryMarker)
			{
				builder.IsPrimary = true;
			}
			else if (line == CacheFormat.LastDtbSucceededMarker)
			{
				builder.LastDesignTimeBuildSucceeded = true;
			}
		}

		static void RecordDeclaredProjectFilePath(
			string value,
			CachePathResolver resolver,
			string projectDirectory,
			ref string? declaredProjectFilePath)
		{
			// The first ``project=`` line wins — once set, additional slices in the
			// same file should agree (and we don't want to re-resolve unnecessarily).
			if (declaredProjectFilePath is not null) return;
			declaredProjectFilePath = ResolveDeclaredProjectFilePath(value, resolver, projectDirectory);
		}

		static string ResolveDeclaredProjectFilePath(string value, CachePathResolver resolver, string projectDirectory)
		{
			string nativeValue = value.Replace('/', Path.DirectorySeparatorChar);
			if (Path.IsPathRooted(nativeValue))
			{
				return Path.GetFullPath(nativeValue);
			}

			// "src/App.csproj" -> "<projectDirectory>/src/App.csproj";
			// "<PATH>src/App.csproj" -> the same path after sentinel expansion.
			return value.StartsWith(PathSentinels.Path, StringComparison.Ordinal)
				? resolver.MakeAbsolute(value, projectDirectory)
				: resolver.ToAbsolute(value, projectDirectory);
		}

	}

	/// <summary>
	/// Expands indentation-compressed paths back into full paths.
	/// For example, <c>src/Models/</c> followed by <c> Product.cs</c> becomes <c>src/Models/Product.cs</c>.
	/// </summary>
	internal static List<(string Path, Dictionary<string, string>? Metadata)> ExpandCompressedPaths(
		List<string> lines,
		CancellationToken cancellationToken)
	{
		List<(string Path, Dictionary<string, string>? Metadata)> results = [];
		Stack<(int Indent, string Prefix)> prefixStack = new();

		foreach (string line in lines)
		{
			cancellationToken.ThrowIfCancellationRequested();
			int indent = CountIndent(line);
			string content = line[indent..];

			if (content.Length > 0 && content[0] == '@')
			{
				// " @aliases=global" attaches aliases=global to the preceding path.
				if (results.Count > 0)
				{
					(string Path, Dictionary<string, string>? Metadata) last = results[^1];
					last.Metadata ??= [];
					ParseMetadataLine(content, last.Metadata);
					results[^1] = last;
				}
				continue;
			}

			while (prefixStack.Count > 0 && prefixStack.Peek().Indent >= indent)
			{
				_ = prefixStack.Pop();
			}

			string prefix = prefixStack.Count > 0 ? prefixStack.Peek().Prefix : "";

			if (content.Length > 0 && content[^1] == '/')
			{
				prefixStack.Push((indent, prefix + content));
			}
			else
			{
				results.Add((prefix + content, null));
			}
		}

		return results;

		static void ParseMetadataLine(string content, Dictionary<string, string> metadata)
		{
			string keyValue = content[1..]; // strip '@'
			int eq = keyValue.IndexOf('=');
			if (eq > 0)
			{
				metadata[keyValue[..eq]] = keyValue[(eq + 1)..];
			}
			else
			{
				metadata[keyValue] = "";
			}
		}

		static int CountIndent(string line)
		{
			int i = 0;
			while (i < line.Length && line[i] == ' ')
			{
				i++;
			}
			return i;
		}
	}

	internal static ImmutableArray<string> DeriveFolderNamesFromPortablePath(string portablePath)
	{
		// "src/Models/User.cs" -> ["src", "Models"]; sentinel and parent-relative paths -> [].
		if (portablePath.Length == 0 || portablePath[0] == '<' || portablePath.StartsWith("../", StringComparison.Ordinal))
			return [];

		int lastSlash = portablePath.LastIndexOf('/');
		if (lastSlash < 0)
			return [];

		return [.. portablePath[..lastSlash].Split('/')];
	}

	private sealed class SliceBuilder
	{
		private readonly CachePathResolver resolver;
		private readonly string projectDirectory;
		private readonly StringPool? stringPool;
		private readonly bool hasAuthoritativeProjectReferenceDefaults;
		private readonly CancellationToken cancellationToken;

		public SliceBuilder(
			CachePathResolver resolver,
			string projectDirectory,
			StringPool? stringPool,
			bool hasAuthoritativeProjectReferenceDefaults,
			CancellationToken cancellationToken)
		{
			this.resolver = resolver;
			this.projectDirectory = projectDirectory;
			this.stringPool = stringPool;
			this.hasAuthoritativeProjectReferenceDefaults = hasAuthoritativeProjectReferenceDefaults;
			this.cancellationToken = cancellationToken;
		}

		public string LanguageName = "";
		public bool IsPrimary;
		public bool LastDesignTimeBuildSucceeded;
		public Dictionary<string, string> SliceDimensions = [];
		public Dictionary<string, string> Properties = [];
		public List<string> CommandLineArguments = [];
		public List<string> SourceFileLines = [];
		public List<string> MetadataReferenceLines = [];
		public List<string> AnalyzerReferenceLines = [];
		public List<string> AnalyzerConfigFileLines = [];
		public List<string> AdditionalFileLines = [];
		public List<string> EmbeddedResourceLines = [];
		public List<string> ProjectReferenceLines = [];
		public List<string> FrameworkPacks = [];
		public List<string> SdkAnalyzerPacks = [];
		public List<string> SdkAnalyzerConfigPolicy = [];
		public List<string> Capabilities = [];

		public void AddSliceDimensionLine(string line)
		{
			int eq = line.IndexOf('=');
			if (eq > 0)
			{
				this.SliceDimensions[this.Pool(line[..eq])] = this.Pool(line[(eq + 1)..]);
			}
		}

		public void AddPropertyLine(string line)
		{
			int eq = line.IndexOf('=');
			if (eq > 0)
			{
				this.Properties[this.Pool(line[..eq])] = this.Pool(this.resolver.MakeAbsolute(line[(eq + 1)..], this.projectDirectory));
			}
		}

		public void AddCommandLineArgument(string value) => this.CommandLineArguments.Add(this.Pool(value));

		public void AddCapability(string value) => this.Capabilities.Add(this.Pool(value));

		public CachedSliceData Build(string projectFilePath)
		{
			(ImmutableArray<string> packManagedRefs, ImmutableArray<string> packAnalyzerRefs) = this.ExpandFrameworkPacks();
			ImmutableArray<string> sdkPackAnalyzerRefs = this.ExpandSdkAnalyzerPacks();
			ImmutableArray<string> analyzerRefs = packAnalyzerRefs.AddRange(sdkPackAnalyzerRefs);

			return new CachedSliceData
			{
				LanguageName = this.LanguageName,
				ProjectFilePath = projectFilePath,
				SliceDimensions = this.BuildDictionary(this.SliceDimensions),
				CommandLineArguments = this.BuildStringArray(this.CommandLineArguments),
				SourceFiles = this.BuildSourceFiles(),
				MetadataReferences = this.BuildMetadataReferences(packManagedRefs),
				AnalyzerReferences = this.BuildAnalyzerReferences(analyzerRefs),
				AnalyzerConfigFiles = this.BuildAnalyzerConfigFiles(),
				AdditionalFiles = this.BuildSimplePaths(this.AdditionalFileLines),
				EmbeddedResources = this.BuildEmbeddedResources(),
				ProjectReferences = this.BuildProjectReferences(),
				Capabilities = this.BuildStringArray(this.Capabilities),
				Properties = this.BuildDictionary(this.Properties),
				IsPrimary = this.IsPrimary,
				LastDesignTimeBuildSucceeded = this.LastDesignTimeBuildSucceeded,
			};
		}

		private string Pool(string value) => this.stringPool?.GetOrAdd(value) ?? value;

		private ImmutableDictionary<string, string> BuildDictionary(Dictionary<string, string> values)
		{
			if (values.Count == 0)
			{
				return ImmutableDictionary<string, string>.Empty;
			}

			ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>();
			foreach ((string key, string value) in values)
			{
				builder[this.Pool(key)] = this.Pool(value);
			}

			return builder.ToImmutable();
		}

		private ImmutableArray<string> BuildStringArray(IEnumerable<string> values)
		{
			ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();
			foreach (string value in values)
			{
				builder.Add(this.Pool(value));
			}

			return builder.ToImmutable();
		}

		private (ImmutableArray<string> Managed, ImmutableArray<string> Analyzers) ExpandFrameworkPacks()
		{
			if (this.FrameworkPacks.Count == 0)
			{
				return ([], []);
			}

			string? targetFramework = this.GetTargetFramework();

			ImmutableArray<string>.Builder managed = ImmutableArray.CreateBuilder<string>();
			ImmutableArray<string>.Builder analyzers = ImmutableArray.CreateBuilder<string>();

			// Probe both SDK install and NuGet locations. The writer always emits canonical
			// pack names (e.g. Microsoft.NETCore.App.Ref) here regardless of where they were
			// resolved at write time, so the reader must accept either source.
			ExpandPacks(
				this.FrameworkPacks,
				packName => this.resolver.FindRefPackDirectory(packName, targetFramework)
					?? this.resolver.FindNuGetFrameworkPackDirectory(packName, targetFramework),
				managed,
				analyzers,
				this.cancellationToken);

			return (managed.ToImmutable(), analyzers.ToImmutable());
		}

		private ImmutableArray<string> ExpandSdkAnalyzerPacks()
		{
			if (this.SdkAnalyzerPacks.Count == 0)
			{
				return [];
			}

			string? targetFramework = this.GetTargetFramework();

			ImmutableArray<string>.Builder analyzerRefs = ImmutableArray.CreateBuilder<string>();
			foreach (string packageId in this.SdkAnalyzerPacks)
			{
				if (string.IsNullOrWhiteSpace(packageId)) continue;
				string? packageDir = this.resolver.FindSdkAnalyzerPackDirectory(packageId, targetFramework);
				if (packageDir is null) continue;

				string analyzerDir = Path.Join(packageDir, "analyzers", "dotnet", "cs");
				if (!Directory.Exists(analyzerDir)) continue;

				List<string> analyzerPaths = [];
				foreach (string analyzerPath in Directory.EnumerateFiles(analyzerDir, "*.dll", SearchOption.AllDirectories))
				{
					this.cancellationToken.ThrowIfCancellationRequested();
					analyzerPaths.Add(analyzerPath);
				}

				analyzerPaths.Sort(StringComparer.OrdinalIgnoreCase);
				foreach (string analyzerPath in analyzerPaths)
				{
					analyzerRefs.Add(this.Pool(analyzerPath));
				}
			}

			return analyzerRefs.ToImmutable();
		}

		private static void ExpandPacks(
			List<string> packNames,
			Func<string, string?> resolvePackDirectory,
			ImmutableArray<string>.Builder managed,
			ImmutableArray<string>.Builder analyzers,
			CancellationToken cancellationToken)
		{
			foreach (string packName in packNames)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (string.IsNullOrWhiteSpace(packName)) continue;
				string? packDir = resolvePackDirectory(packName);
				if (packDir is null) continue;

				FrameworkListExpander.ExpansionResult result = FrameworkListExpander.Expand(packDir, cancellationToken);
				managed.AddRange(result.ManagedAssemblyPaths);
				analyzers.AddRange(result.AnalyzerCsPaths);
			}
		}

		private ImmutableArray<string> BuildSimplePaths(List<string> lines)
		{
			if (lines.Count == 0)
				return [];

			List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(lines, this.cancellationToken);
			if (!this.resolver.IsNetSdkBound)
			{
				expanded = this.FilterUnresolvableNetSdkPaths(expanded);
			}

			ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(expanded.Count);
			foreach ((string path, Dictionary<string, string>? _) in expanded)
			{
				builder.Add(this.Pool(this.resolver.ToAbsolute(path, this.projectDirectory)));
			}

			return builder.ToImmutable();
		}

		private ImmutableArray<string> BuildAnalyzerConfigFiles()
		{
			ImmutableArray<string> explicitFiles = this.BuildSimplePaths(this.AnalyzerConfigFileLines);
			if (this.SdkAnalyzerConfigPolicy.Count == 0)
			{
				return explicitFiles;
			}

			if (!this.resolver.IsNetSdkBound)
			{
				System.Diagnostics.Trace.TraceWarning(
					"[lscache] Skipping SDK analyzer config policy entries in {0} — no SDK binding supplied. " +
					"SDK-shipped analyzer configs will be missing until SDK info is available.",
					this.projectDirectory);
				return explicitFiles;
			}

			string? targetFramework = this.GetTargetFramework();
			ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>(explicitFiles.Length + this.SdkAnalyzerConfigPolicy.Count);
			builder.AddRange(explicitFiles);
			foreach (string policy in this.SdkAnalyzerConfigPolicy)
			{
				foreach (string path in this.resolver.ResolveSdkAnalyzerConfigPolicy(policy, targetFramework))
				{
					builder.Add(this.Pool(path));
				}
			}

			return builder.ToImmutable();
		}

		private string? GetTargetFramework()
		{
			if (this.SliceDimensions.TryGetValue(ProjectProperties.TargetFramework, out string? targetFramework))
				return targetFramework;
			if (this.Properties.TryGetValue(ProjectProperties.TargetFramework, out targetFramework))
				return targetFramework;
			return null;
		}

		private ImmutableArray<string> BuildAnalyzerReferences(ImmutableArray<string> packAnalyzerRefs)
		{
			List<string> analyzerLines = this.AnalyzerReferenceLines;
			if (!this.resolver.IsNetSdkBound)
			{
				analyzerLines = this.FilterUnresolvableNetSdkLines(analyzerLines);
			}

			ImmutableArray<string> explicitRefs = [];
			if (analyzerLines.Count > 0)
			{
				List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(analyzerLines, this.cancellationToken);
				ImmutableArray<string>.Builder explicitBuilder = ImmutableArray.CreateBuilder<string>(expanded.Count);
				foreach ((string path, Dictionary<string, string>? _) in expanded)
				{
					explicitBuilder.Add(this.Pool(this.resolver.ToAbsolute(path, this.projectDirectory)));
				}
				explicitRefs = explicitBuilder.ToImmutable();
			}

			if (packAnalyzerRefs.IsDefaultOrEmpty)
			{
				return explicitRefs;
			}

			// NuGet-wins conflict resolution: skip any framework analyzer whose filename
			// collides with an explicit entry. The cache file holds no exclusion list.
			HashSet<string> explicitBasenames = BuildBasenameSet(explicitRefs);

			ImmutableArray<string>.Builder result = ImmutableArray.CreateBuilder<string>(explicitRefs.Length + packAnalyzerRefs.Length);
			result.AddRange(explicitRefs);
			foreach (string packRef in packAnalyzerRefs)
			{
				string filename = Path.GetFileName(packRef);
				if (explicitBasenames.Contains(filename)) continue;
				result.Add(this.Pool(packRef));
			}
			return result.ToImmutable();
		}

		/// <summary>
		/// Filters out lines whose compressed-path prefix starts with the <c>&lt;NETSDK&gt;</c>
		/// sentinel, logging a single warning when any are dropped. Used when the resolver
		/// is unbound (no SDK info supplied by the caller) so we skip rather than fabricate
		/// a phantom path.
		/// </summary>
		private List<string> FilterUnresolvableNetSdkLines(List<string> lines)
		{
			List<string> filtered = [];
			bool warned = false;
			foreach (string line in lines)
			{
				string trimmed = line.TrimStart();
				if (trimmed.StartsWith(PathSentinels.NetSdk, StringComparison.Ordinal))
				{
					if (!warned)
					{
						System.Diagnostics.Trace.TraceWarning(
							"[lscache] Skipping <NETSDK> path(s) in {0} — no SDK binding supplied. " +
							"SDK-shipped analyzers and configs will be missing until SDK info is available.",
							this.projectDirectory);
						warned = true;
					}
				}
				else
				{
					filtered.Add(line);
				}
			}
			return filtered;
		}

		private List<(string Path, Dictionary<string, string>? Metadata)> FilterUnresolvableNetSdkPaths(
			List<(string Path, Dictionary<string, string>? Metadata)> expanded)
		{
			bool warned = false;
			List<(string Path, Dictionary<string, string>? Metadata)> filtered = [];
			foreach ((string path, Dictionary<string, string>? metadata) in expanded)
			{
				if (path.StartsWith(PathSentinels.NetSdk, StringComparison.Ordinal))
				{
					if (!warned)
					{
						System.Diagnostics.Trace.TraceWarning(
							"[lscache] Skipping <NETSDK> path(s) in {0} — no SDK binding supplied. " +
							"SDK-shipped analyzers and configs will be missing until SDK info is available.",
							this.projectDirectory);
						warned = true;
					}
				}
				else
				{
					filtered.Add((path, metadata));
				}
			}
			return filtered;
		}

		private ImmutableArray<CachedSourceFile> BuildSourceFiles()
		{
			if (this.SourceFileLines.Count == 0)
				return [];

			List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(this.SourceFileLines, this.cancellationToken);
			ImmutableArray<CachedSourceFile>.Builder builder = ImmutableArray.CreateBuilder<CachedSourceFile>(expanded.Count);
			foreach ((string path, Dictionary<string, string>? metadata) in expanded)
			{
				string? link = metadata is not null && metadata.TryGetValue("link", out string? value) && !string.IsNullOrEmpty(value)
					? value
					: null;
				builder.Add(new CachedSourceFile
				{
					FilePath = this.Pool(this.resolver.ToAbsolute(path, this.projectDirectory)),
					Link = link,
				});
			}

			return builder.ToImmutable();
		}

		private ImmutableArray<CachedProjectReference> BuildProjectReferences()
		{
			if (this.ProjectReferenceLines.Count == 0)
			{
				return [];
			}

			List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(this.ProjectReferenceLines, this.cancellationToken);
			ImmutableArray<CachedProjectReference>.Builder builder = ImmutableArray.CreateBuilder<CachedProjectReference>(expanded.Count);
			foreach ((string path, Dictionary<string, string>? metadata) in expanded)
			{
				bool? referenceOutputAssembly = metadata is not null &&
					metadata.TryGetValue(ProjectItems.ProjectReference.ReferenceOutputAssembly, out string? value)
						? !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
						: this.hasAuthoritativeProjectReferenceDefaults
							? true
							: null;
				builder.Add(new CachedProjectReference
				{
					FilePath = this.Pool(this.resolver.ToAbsolute(path, this.projectDirectory)),
					ReferenceOutputAssembly = referenceOutputAssembly,
				});
			}

			return builder.ToImmutable();
		}

		private ImmutableArray<CachedMetadataReference> BuildMetadataReferences(ImmutableArray<string> packManagedRefs)
		{
			ImmutableArray<CachedMetadataReference> explicitRefs = [];
			if (this.MetadataReferenceLines.Count > 0)
			{
				List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(this.MetadataReferenceLines, this.cancellationToken);
				ImmutableArray<CachedMetadataReference>.Builder explicitBuilder = ImmutableArray.CreateBuilder<CachedMetadataReference>(expanded.Count);
				bool warnedMissingNetFxRefs = false;
				foreach ((string path, Dictionary<string, string>? metadata) in expanded)
				{
					ImmutableArray<string> aliases = [];
					if (metadata is not null && metadata.TryGetValue(ProjectItems.MetadataReference.Aliases, out string? aliasText) && aliasText.Length > 0)
					{
						aliases = this.BuildStringArray(aliasText.Split(','));
					}

					string? resolvedPath;
					if (this.resolver.TryResolveNetFrameworkReferenceAssemblyPath(path, out string? netFxPath))
					{
						if (netFxPath is null)
						{
							if (!warnedMissingNetFxRefs)
							{
								System.Diagnostics.Trace.TraceWarning(
									"[lscache] Skipping <NETFXREF> metadata reference(s) in {0} because no matching .NET Framework reference assemblies were found.",
									this.projectDirectory);
								warnedMissingNetFxRefs = true;
							}
							continue;
						}

						resolvedPath = netFxPath;
					}
					else
					{
						resolvedPath = this.resolver.ToAbsolute(path, this.projectDirectory);
					}

					explicitBuilder.Add(new CachedMetadataReference
					{
						FilePath = this.Pool(resolvedPath),
						Aliases = aliases,
						EmbedInteropTypes = metadata is not null && metadata.ContainsKey(ProjectItems.MetadataReference.EmbedInteropTypes),
					});
				}
				explicitRefs = explicitBuilder.ToImmutable();
			}

			if (packManagedRefs.IsDefaultOrEmpty)
			{
				return explicitRefs;
			}

			// NuGet-wins conflict resolution: skip any framework managed assembly whose
			// filename collides with an explicit entry. Explicit NuGet/project refs win.
			HashSet<string> explicitBasenames = new(StringComparer.OrdinalIgnoreCase);
			foreach (CachedMetadataReference r in explicitRefs)
			{
				explicitBasenames.Add(Path.GetFileName(r.FilePath));
			}

			ImmutableArray<CachedMetadataReference>.Builder result = ImmutableArray.CreateBuilder<CachedMetadataReference>(explicitRefs.Length + packManagedRefs.Length);
			result.AddRange(explicitRefs);
			foreach (string packRef in packManagedRefs)
			{
				string filename = Path.GetFileName(packRef);
				if (explicitBasenames.Contains(filename)) continue;
				result.Add(new CachedMetadataReference
				{
					FilePath = this.Pool(packRef),
					Aliases = [],
					EmbedInteropTypes = false,
				});
			}
			return result.ToImmutable();
		}

		private static HashSet<string> BuildBasenameSet(ImmutableArray<string> paths)
		{
			HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);
			foreach (string p in paths)
			{
				set.Add(Path.GetFileName(p));
			}
			return set;
		}

		private ImmutableArray<CachedEmbeddedResource> BuildEmbeddedResources()
		{
			if (this.EmbeddedResourceLines.Count == 0)
				return [];

			List<(string Path, Dictionary<string, string>? Metadata)> expanded = ExpandCompressedPaths(this.EmbeddedResourceLines, this.cancellationToken);
			ImmutableArray<CachedEmbeddedResource>.Builder builder = ImmutableArray.CreateBuilder<CachedEmbeddedResource>(expanded.Count);
			foreach ((string path, Dictionary<string, string>? metadata) in expanded)
			{
				builder.Add(new CachedEmbeddedResource
				{
					FilePath = this.Pool(this.resolver.ToAbsolute(path, this.projectDirectory)),
					Generator = metadata is not null && metadata.TryGetValue(ProjectItems.EmbeddedResource.Generator, out string? gen) ? gen : null,
					LastGenOutput = metadata is not null && metadata.TryGetValue(ProjectItems.EmbeddedResource.LastGenOutput, out string? lgo) ? lgo : null,
					CustomToolNamespace = metadata is not null && metadata.TryGetValue(ProjectItems.EmbeddedResource.CustomToolNamespace, out string? ctn) ? ctn : null,
				});
			}

			return builder.ToImmutable();
		}
	}
}
