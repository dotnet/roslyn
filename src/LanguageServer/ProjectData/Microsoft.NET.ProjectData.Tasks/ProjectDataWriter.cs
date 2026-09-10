// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Frozen;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Builds the textual project-data content for a single slice and writes it atomically.
/// All methods are pure (no MSBuild dependency) so they can be unit-tested directly
/// without mocking MSBuild infrastructure; <see cref="AtomicWrite"/> is the only
/// member that does file I/O.
/// </summary>
internal static class ProjectDataWriter
{
	private const string NewLine = "\n";

	// Portable-path prefix for entries rooted in an SDK targeting/runtime ref pack.
	// Reader expands these via FrameworkList.xml at read time.
	internal const string DotNetPacksPrefix = PathSentinels.Dotnet + "/packs/";
	internal const string NetFxRefPrefix = PathSentinels.NetFxRef + "/";
	internal const string NuGetPrefix = PathSentinels.Nuget + "/";
	internal const string MissingNetFrameworkReferenceAssembliesReason = "MissingNetFrameworkReferenceAssemblies";

	// Roslyn repo/toolset CSharp targets suppress CS8002 for .NETCoreApp because strong naming is ignored there.
	// Normalize it so caches stay stable when DTB imports SDK inbox targets instead.
	private const string NetCoreAppIgnoredStrongNameWarning = "8002";
	private static readonly StringComparer PathComparer = StringComparers.Paths;

	private static int ComparePortablePaths(string? left, string? right)
	{
		int comparison = StringComparer.OrdinalIgnoreCase.Compare(left, right);
		return comparison != 0 ? comparison : StringComparer.Ordinal.Compare(left, right);
	}

	/// <summary>
	/// Capabilities that are universal to managed projects and add no
	/// filtering value. Excluding them keeps cache files smaller and avoids noise
	/// in capability-based queries.
	/// </summary>
	private static readonly FrozenSet<string> ExcludedCapabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		"AllTargetOutputGroups",
		"AppServicePublish",
		"AspNetCoreInProcessHosting",
		"AssemblyReferences",
		"BuildWindowsDesktopTarget",
		"COMReferences",
		"CSharp",
		"DeclaredSourceItems",
		"DotNetCoreRazorConfiguration",
		"DynamicDependentFile",
		"DynamicFileNesting",
		"GenerateDocumentationFile",
		"LanguageService",
		"Managed",
		"NetSdkOCIImageBuild",
		"OutputGroups",
		"ProjectReferences",
		"ReferencesFolder",
		"RelativePathDerivedDefaultNamespace",
		"SharedProjectReferences",
		"SingleFileGenerators",
		"SupportHierarchyContextSvc",
		"SupportsComputeRunCommand",
		"SupportsTypeScriptNuGet",
		"UserSourceItems",
		"VisualStudioWellKnownOutputGroups",
		"WebNestingDefaults",
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Properties that must never appear in the cache file. These are
	/// environment-specific values injected at read time by the snapshot
	/// factory; writing them would produce stale/machine-local data.
	/// </summary>
	private static readonly FrozenSet<string> ExcludedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		ProjectProperties.SolutionPath,
	}.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Properties that must always be written, even when empty/whitespace.
	/// Generated from <c>server/src/Microsoft.NET.ProjectData.Generators/project-data-schema.json</c> by <c>DataModelSchemaGenerator</c>.
	/// </summary>
	private static readonly FrozenSet<string> RequiredProperties = ProjectProperties.Required.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

	// Builds the complete content for one slice. When writeHeader=true the
	// version/banner/[project] header is included (single-target, or any single
	// file consumed directly); when false only the [project] section onward
	// is emitted (multi-TFM inner-build slices, merged later by ProjectDataMerger).
	public static string BuildContent(
		string projectFilePath,
		bool writeHeader,
		bool isPrimary,
		bool lastDtbSucceeded,
		ITaskItem[]? sliceDimensions,
		ITaskItem[]? properties,
		string[]? commandLineArguments,
		ITaskItem[]? sourceFiles,
		ITaskItem[]? metadataReferences,
		ITaskItem[]? analyzerReferences,
		string[]? analyzerConfigFiles,
		string[]? additionalFiles,
		ITaskItem[]? embeddedResources = null,
		ITaskItem[]? projectReferences = null,
		string[]? capabilities = null,
		ITaskItem[]? sdkKnownAnalyzerPacks = null,
		ITaskItem[]? sdkAnalyzerConfigPolicy = null,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter = null)
	{
		using var writer = new StringWriter();
		writer.NewLine = NewLine;
		WriteContent(
			writer,
			projectFilePath,
			writeHeader,
			isPrimary,
			lastDtbSucceeded,
			sliceDimensions,
			properties,
			commandLineArguments,
			sourceFiles,
			metadataReferences,
			analyzerReferences,
			analyzerConfigFiles,
			additionalFiles,
			embeddedResources,
			projectReferences,
			capabilities,
			sdkKnownAnalyzerPacks,
			sdkAnalyzerConfigPolicy,
			duplicateItemReporter);

		return writer.ToString();
	}

	public static void WriteContent(
		TextWriter writer,
		string projectFilePath,
		bool writeHeader,
		bool isPrimary,
		bool lastDtbSucceeded,
		ITaskItem[]? sliceDimensions,
		ITaskItem[]? properties,
		string[]? commandLineArguments,
		ITaskItem[]? sourceFiles,
		ITaskItem[]? metadataReferences,
		ITaskItem[]? analyzerReferences,
		string[]? analyzerConfigFiles,
		string[]? additionalFiles,
		ITaskItem[]? embeddedResources = null,
		ITaskItem[]? projectReferences = null,
		string[]? capabilities = null,
		ITaskItem[]? sdkKnownAnalyzerPacks = null,
		ITaskItem[]? sdkAnalyzerConfigPolicy = null,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter = null)
	{
		CachePathResolver resolver = new CachePathResolver(projectFilePath);

		if (writeHeader)
		{
			writer.WriteLine(CacheFormat.VersionHeader);
			writer.WriteLine();
			writer.WriteLine("# This file caches language service data to improve the performance of C# Dev Kit.");
			writer.WriteLine("# It is not intended for manual editing. It can safely be deleted and will be");
			writer.WriteLine("# regenerated automatically. For more information, see https://aka.ms/lscache");
			writer.WriteLine("#");
			writer.WriteLine("# To control where cache files are stored, use the following VS Code setting:");
			writer.WriteLine("#   \"dotnet.projectsystem.cacheInProjectFolder\": true");
		}

		// [project]
		if (writeHeader) writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Project));
		writer.Write(CacheFormat.ProjectHeaderPrefix); writer.WriteLine(resolver.ToPortable(projectFilePath));
		writer.Write(CacheFormat.LanguagePrefix); writer.WriteLine("C#");
		if (isPrimary) writer.WriteLine(CacheFormat.PrimaryMarker);
		if (lastDtbSucceeded) writer.WriteLine(CacheFormat.LastDtbSucceededMarker);

		// [sliceDimensions]
		List<KeyValuePair<string, string>> sliceKvps = ToSortedKvps(sliceDimensions);
		if (sliceKvps.Count > 0)
		{
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SliceDimensions));
			foreach (KeyValuePair<string, string> kvp in sliceKvps)
			{
				writer.Write(kvp.Key);
				writer.Write('=');
				writer.WriteLine(kvp.Value);
			}
		}

		// [properties] — sorted OrdinalIgnoreCase; values MakePortable'd.
		// Empty / unset values are skipped for optional properties, but required
		// properties (from project-data-schema.json) are always written — even when
		// empty — so the reader can distinguish "not set" from "set to empty".
		// Properties in the ExcludedProperties set are never written (defense-in-depth).
		List<KeyValuePair<string, string>> propKvps = ToSortedKvps(properties);
		if (propKvps.Count > 0)
		{
			bool wroteHeader = false;
			foreach (KeyValuePair<string, string> kvp in propKvps)
			{
				string value = kvp.Value ?? string.Empty;
				if (value == "*Undefined*") continue;
				if (ExcludedProperties.Contains(kvp.Key)) continue;
				if (string.IsNullOrWhiteSpace(value) && !RequiredProperties.Contains(kvp.Key)) continue;
				if (!wroteHeader)
				{
					writer.WriteLine();
					writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Properties));
					wroteHeader = true;
				}
				writer.Write(kvp.Key);
				writer.Write('=');
				writer.WriteLine(resolver.MakePortable(value));
			}
		}

		var targetFramework = new TargetFramework(
			GetItemValue(sliceDimensions, ProjectProperties.TargetFramework) ?? GetItemValue(properties, ProjectProperties.TargetFramework),
			GetItemValue(properties, ProjectProperties.TargetFrameworkIdentifier),
			GetItemValue(properties, ProjectProperties.TargetFrameworkVersion));

		// [commandLineArguments] — order preserved; file-based args filtered out
		// Required item type — always write header even if empty.
		{
			var filtered = new List<string>();
			if (commandLineArguments != null)
			{
				foreach (var arg in commandLineArguments)
				{
					if (arg == null) continue;
					if (IsFileArgument(arg)) continue;
					if (IsMachineSpecificArgument(arg)) continue;
					if (IsPlatformArgument(arg)) continue;
					if (ShouldSkipCommandLineArgument(arg, targetFramework.Identifier)) continue;
					string normalized = NormalizeCommandLineArgument(arg, targetFramework.Identifier);
					if (!string.IsNullOrEmpty(normalized))
					{
						filtered.Add(normalized);
					}
				}
			}
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.CommandLineArguments));
			foreach (var arg in filtered)
				writer.WriteLine(resolver.MakePortable(arg));
		}

		// Path sections.
		// Metadata and analyzer refs are pre-processed: shared-framework entries
		// rooted in an SDK .App.Ref pack or a recognized NuGet ref pack are removed,
		// and the pack name is added to [frameworkPacks] for the reader to expand via
		// FrameworkList.xml. Workload-pack entries remain explicit because their
		// versions do not follow the target framework's version scheme.
		SortedSet<string> frameworkPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		SortedSet<string> sdkAnalyzerPacks = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		SortedSet<string> sdkAnalyzerConfigPolicyLines = BuildSdkAnalyzerConfigPolicy(sdkAnalyzerConfigPolicy, targetFramework);
		List<KeyValuePair<string, ITaskItem>> preparedMetadataRefs = PrepareMetadataRefs(
			metadataReferences,
			resolver,
			frameworkPacks,
			targetFramework);
		List<string> preparedAnalyzerRefs = PrepareAnalyzerRefs(
			analyzerReferences,
			resolver,
			frameworkPacks,
			sdkAnalyzerPacks,
			sdkKnownAnalyzerPacks,
			targetFramework,
			projectFilePath,
			duplicateItemReporter);
		List<string> preparedAnalyzerConfigFiles = AnalyzerConfigFileFilter.Prepare(
			analyzerConfigFiles,
			resolver,
			sourceFiles,
			filterSdkAnalyzerConfigFiles: sdkAnalyzerConfigPolicyLines.Count > 0);
		preparedAnalyzerConfigFiles = ToSortedDistinctPortablePaths(
			preparedAnalyzerConfigFiles,
			CacheFormat.Sections.AnalyzerConfigFiles,
			projectFilePath,
			duplicateItemReporter);

		EmitSourceFileSection(writer, sourceFiles, resolver, projectFilePath, duplicateItemReporter);
		WriteFrameworkPacksSection(writer, frameworkPacks);
		EmitMetadataRefSection(writer, preparedMetadataRefs);
		WriteSdkAnalyzerPacksSection(writer, sdkAnalyzerPacks);
		EmitSimplePathSectionRequired(writer, CacheFormat.SectionHeader(CacheFormat.Sections.AnalyzerReferences), preparedAnalyzerRefs);
		WriteSdkAnalyzerConfigPolicySection(writer, sdkAnalyzerConfigPolicyLines);
		EmitSimplePathSection(writer, CacheFormat.SectionHeader(CacheFormat.Sections.AnalyzerConfigFiles), preparedAnalyzerConfigFiles);
		WritePreparedPathSection(
			writer,
			CacheFormat.SectionHeader(CacheFormat.Sections.AdditionalFiles),
			PrepareDistinctPortablePaths(additionalFiles, resolver, CacheFormat.Sections.AdditionalFiles, projectFilePath, duplicateItemReporter),
			required: false);
		EmitEmbeddedResourceSection(writer, embeddedResources, resolver);
		EmitProjectReferenceSection(writer, projectReferences, resolver, projectFilePath, duplicateItemReporter);

		// Capabilities — simple string list, one per line.
		// Exclude well-known capabilities that are universal to managed projects
		// and add no filtering value — they would just bloat every cache file.
		if (capabilities is { Length: > 0 })
		{
			// Deduplicate (MSBuild can emit duplicates), exclude banned, and sort for stable output.
			var uniqueCaps = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string cap in capabilities)
			{
				if (!ExcludedCapabilities.Contains(cap))
					uniqueCaps.Add(cap);
			}

			if (uniqueCaps.Count > 0)
			{
				writer.WriteLine();
				writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Capabilities));
				foreach (string cap in uniqueCaps)
					writer.WriteLine(cap);
			}
		}
	}

	// The major version this build understands, parsed from the writer's own version header.
	// Forward-compatibility preservation only runs against an existing file of the same major.
	private static readonly int CurrentMajorVersion = ForwardCompat.TryParseVersion(CacheFormat.VersionHeader, out int major, out _)
		? major
		: throw new InvalidOperationException($"CacheFormat.VersionHeader '{CacheFormat.VersionHeader}' is not a valid 'version=<major>[.<minor>]' header.");

	internal static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	/// <summary>
	/// Atomically writes arbitrary <paramref name="content"/> to <paramref name="outputPath"/> via a
	/// <c>.tmp</c> side-file, skipping the write when the existing file already has identical content
	/// (so a no-op never touches the file and never churns watchers / git). No forward-compat
	/// preservation is applied — this overload is for non-cache content.
	/// </summary>
	public static void AtomicWrite(string outputPath, string content, string? tempDirectory = null)
	{
		string normalized = NormalizeLineEndings(content);
		byte[] bytes = Utf8NoBom.GetBytes(normalized);
		WriteAtomicallyCore(outputPath, bytes, bytes.Length, preserveUnknownData: false, candidateTextFactory: null, tempDirectory);
	}

	/// <summary>
	/// Atomically writes cache content produced by <paramref name="writeContent"/> to
	/// <paramref name="outputPath"/>. The existing file is read once (into a pooled buffer) and is
	/// used both to preserve forward-compatible data a newer minor version may have written and to
	/// decide whether the write can be skipped — a content match means the file is left untouched.
	/// No SHA-256 / hash header is written; a no-op build never touches the file.
	/// </summary>
	/// <remarks>
	/// The candidate is rendered into pooled <see cref="char"/>/<see cref="byte"/> buffers rather
	/// than a <see cref="string"/> plus a <see cref="byte"/> array, so the common no-op /
	/// same-version path allocates nothing on the large-object heap. The candidate string is
	/// materialized lazily only on the rare path where a newer minor version's data actually has to
	/// be spliced in.
	/// </remarks>
	public static void AtomicWriteStreamed(string outputPath, Action<TextWriter> writeContent, string? tempDirectory = null)
	{
		using PooledCacheRender render = PooledCacheRender.Create(writeContent);
		WriteAtomicallyCore(outputPath, render.Bytes, render.ByteLength, preserveUnknownData: true, candidateTextFactory: render.GetText, tempDirectory);
	}

	/// <summary>
	/// Shared write core: compares the candidate (<paramref name="candidateBuffer"/> /
	/// <paramref name="candidateLength"/>) against the existing file, optionally splices in
	/// forward-compatible data, and atomically replaces the file only when the final bytes differ
	/// (or a legacy <c>hash=</c> header still needs stripping). The existing file is read into a
	/// pooled buffer to stay off the large-object heap. <paramref name="candidateTextFactory"/>
	/// lazily produces the candidate as a string and is invoked only on the rare preservation path.
	/// </summary>
	private static void WriteAtomicallyCore(
		string outputPath,
		byte[] candidateBuffer,
		int candidateLength,
		bool preserveUnknownData,
		Func<string>? candidateTextFactory,
		string? tempDirectory)
	{
		byte[]? rented = null;
		try
		{
			int existingLength = 0;
			int contentStart = 0;
			bool existingHadHashLine = false;
			bool existingPresent = false;

			if (TryGetFileLength(outputPath, out int fileLength) && fileLength > 0)
			{
				rented = ArrayPool<byte>.Shared.Rent(fileLength);
				if (TryReadAll(outputPath, rented, fileLength, out existingLength))
				{
					existingPresent = true;
					contentStart = SkipLegacyHashLine(rented, existingLength, out existingHadHashLine);
				}
			}

			byte[] finalBuffer = candidateBuffer;
			int finalLength = candidateLength;

			if (existingPresent)
			{
				int existingContentLength = existingLength - contentStart;
				var existingContent = new ReadOnlySpan<byte>(rented!, contentStart, existingContentLength);
				var candidateContent = new ReadOnlySpan<byte>(candidateBuffer, 0, candidateLength);

				// A cache's minor stamp marks payload compatibility, not the writer binary. If a newer
				// writer emits no new data, retain the still-valid older stamp and leave the file
				// untouched rather than churning every cache after each minor schema bump.
				bool candidateMatchesExisting = existingContent.SequenceEqual(candidateContent)
					|| (preserveUnknownData
						&& ForwardCompat.MatchesExceptForOlderMinorVersion(
							existingContent,
							candidateContent,
							CurrentMajorVersion));
				bool bufferWasMerged = false;

				// Only decode the existing/candidate bytes to strings (and run the full splice) when a
				// byte-level probe says the existing file was authored by a NEWER minor — the one case
				// where ForwardCompat.PreserveUnknownData can carry anything forward. On the common
				// same-version change this skips two ~file-sized string allocations.
				if (preserveUnknownData
					&& !candidateMatchesExisting
					&& ForwardCompat.ExistingHasNewerMinor(existingContent, candidateContent, CurrentMajorVersion))
				{
					string existingText = Utf8NoBom.GetString(rented!, contentStart, existingContentLength);
					string candidateText = candidateTextFactory!();
					string merged = ForwardCompat.PreserveUnknownData(existingText, candidateText, CurrentMajorVersion);
					if (!ReferenceEquals(merged, candidateText))
					{
						byte[] mergedBytes = Utf8NoBom.GetBytes(merged);
						finalBuffer = mergedBytes;
						finalLength = mergedBytes.Length;
						bufferWasMerged = true;
					}
				}

				// Skip the write only when the final content already matches AND there is no stale legacy
				// hash line to strip. The legacy-hash case forces exactly one rewrite per file.
				bool finalMatchesExisting = bufferWasMerged
					? existingContent.SequenceEqual(new ReadOnlySpan<byte>(finalBuffer, 0, finalLength))
					: candidateMatchesExisting;
				if (!existingHadHashLine && finalMatchesExisting)
				{
					return;
				}
			}

			AtomicReplace(outputPath, finalBuffer, finalLength, tempDirectory);
		}
		finally
		{
			if (rented != null)
				ArrayPool<byte>.Shared.Return(rented);
		}
	}

	private static bool TryGetFileLength(string path, out int length)
	{
		length = 0;
		try
		{
			long len = new FileInfo(path).Length;
			if (len <= 0 || len > int.MaxValue)
				return false;
			length = (int)len;
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	private static bool TryReadAll(string path, byte[] buffer, int length, out int bytesRead)
	{
		bytesRead = 0;
		try
		{
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			int total = 0;
			int read;
			while (total < length && (read = stream.Read(buffer, total, length - total)) > 0)
				total += read;
			bytesRead = total;
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	/// <summary>
	/// Returns the offset of the first byte after a leading legacy <c>hash=...</c> line, or 0 when
	/// the file has no such line. Pre-regeneration cache files still start with this header; the
	/// reader skips it too. The flag lets the writer force a one-time rewrite that strips it.
	/// </summary>
	private static int SkipLegacyHashLine(byte[] buffer, int length, out bool hadHashLine)
	{
		hadHashLine = false;
		ReadOnlySpan<byte> prefix = "hash="u8;
		if (length < prefix.Length)
			return 0;
		if (!new ReadOnlySpan<byte>(buffer, 0, prefix.Length).SequenceEqual(prefix))
			return 0;

		for (int i = prefix.Length; i < length; i++)
		{
			if (buffer[i] == (byte)'\n')
			{
				hadHashLine = true;
				return i + 1;
			}
		}

		// A file that is nothing but a hash line (no newline) — treat the whole thing as the header.
		hadHashLine = true;
		return length;
	}

	private static void AtomicReplace(string outputPath, byte[] content, int length, string? tempDirectory)
	{
		string? outputDir = Path.GetDirectoryName(outputPath);
		if (outputDir != null) Directory.CreateDirectory(outputDir);

		// Keep the transient side-file out of the (committed, watched) project folder by preferring
		// the intermediate output directory, falling back to the output directory when none is given
		// or it is on a different volume.
		string tempDir = ResolveTempDirectory(tempDirectory, outputPath, outputDir);
		string tempPath = Path.Combine(tempDir, Path.GetFileName(outputPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");
		try
		{
			using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096))
				stream.Write(content, 0, length);

			ReplaceOrMove(tempPath, outputPath);
		}
		finally
		{
			// Best-effort cleanup of the temp file (may already be gone after a successful move).
			try { File.Delete(tempPath); } catch { }
		}
	}

	/// <summary>
	/// Chooses the directory for the atomic-write temp side-file. Prefers <paramref name="tempDirectory"/>
	/// (the project's intermediate output directory) so transient <c>.tmp</c> files never appear next
	/// to committed source. Falls back to the output directory when no temp directory is requested,
	/// when it is on a different volume (<see cref="File.Replace(string, string, string)"/> requires
	/// the temp file and the destination to share a volume), or when it cannot be created.
	/// </summary>
	private static string ResolveTempDirectory(string? tempDirectory, string outputPath, string? outputDir)
	{
		string fallback = string.IsNullOrEmpty(outputDir) ? "." : outputDir!;
		if (string.IsNullOrEmpty(tempDirectory))
			return fallback;

		try
		{
			string full = Path.GetFullPath(tempDirectory!);
			StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? StringComparison.OrdinalIgnoreCase
				: StringComparison.Ordinal;
			if (!string.Equals(Path.GetPathRoot(full), Path.GetPathRoot(Path.GetFullPath(outputPath)), comparison))
				return fallback;

			Directory.CreateDirectory(full);
			return full;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
		{
			return fallback;
		}
	}

	private static string NormalizeLineEndings(string content)
		=> content.IndexOf('\r') < 0
			? content
			: content.Replace("\r\n", "\n").Replace('\r', '\n');

	private static void ReplaceOrMove(string tempPath, string outputPath)
	{
		try
		{
			File.Replace(tempPath, outputPath, null);
		}
		catch (FileNotFoundException)
		{
			try
			{
				File.Move(tempPath, outputPath);
			}
			catch (IOException moveException)
			{
				try
				{
					File.Replace(tempPath, outputPath, null);
				}
				catch (FileNotFoundException)
				{
					ExceptionDispatchInfo.Capture(moveException).Throw();
					throw;
				}
			}
		}
	}

	// Returns true for command-line args that represent file inputs. These are
	// excluded from [commandLineArguments] because CPS puts them in dedicated sections.
	internal static bool IsFileArgument(string arg)
	{
		if (arg.StartsWith("/reference:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/analyzer:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/analyzerconfig:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/additionalfile:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/sourcelink:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/embed:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("/resource:", StringComparison.OrdinalIgnoreCase)) return true;
		// Bare source-file paths go into [sourceFiles]. On Unix, absolute source
		// paths start with "/" and would otherwise be mistaken for compiler switches.
		if (IsRootedSourceFileArgument(arg)) return true;
		if (IsPortableSourceFileArgument(arg)) return true;
		if (arg.Length > 0 && arg[0] != '/' && arg[0] != '-') return true;
		return false;
	}

	// Returns true for compiler switches that vary per machine but have no effect
	// on compilation output. Including them would cause cache churn across locales.
	internal static bool IsMachineSpecificArgument(string arg)
	{
		// /preferreduilang:<locale> controls the language of diagnostic messages,
		// not the compilation result. It varies by OS locale.
		if (arg.StartsWith("/preferreduilang:", StringComparison.OrdinalIgnoreCase)) return true;
		if (arg.StartsWith("-preferreduilang:", StringComparison.OrdinalIgnoreCase)) return true;
		return false;
	}

	internal static bool IsPlatformArgument(string arg)
		=> arg.StartsWith("/platform:", StringComparison.OrdinalIgnoreCase)
			|| arg.StartsWith("-platform:", StringComparison.OrdinalIgnoreCase);

	internal static string NormalizeCommandLineArgument(string arg, string? targetFrameworkIdentifier)
	{
		if (!string.Equals(targetFrameworkIdentifier, ".NETCoreApp", StringComparison.OrdinalIgnoreCase))
		{
			return arg;
		}

		const string slashNoWarn = "/nowarn:";
		const string dashNoWarn = "-nowarn:";
		string prefix;
		if (arg.StartsWith(slashNoWarn, StringComparison.OrdinalIgnoreCase))
		{
			prefix = arg.Substring(0, slashNoWarn.Length);
		}
		else if (arg.StartsWith(dashNoWarn, StringComparison.OrdinalIgnoreCase))
		{
			prefix = arg.Substring(0, dashNoWarn.Length);
		}
		else
		{
			return arg;
		}

		string warnings = arg.Substring(prefix.Length);
		foreach (string warning in warnings.Split(',', ';'))
		{
			if (string.Equals(warning.Trim(), NetCoreAppIgnoredStrongNameWarning, StringComparison.OrdinalIgnoreCase))
			{
				return arg;
			}
		}

		return string.IsNullOrEmpty(warnings)
			? prefix + NetCoreAppIgnoredStrongNameWarning
			: arg + "," + NetCoreAppIgnoredStrongNameWarning;
	}

	internal static bool ShouldSkipCommandLineArgument(string arg, string? targetFrameworkIdentifier)
	{
		return string.Equals(targetFrameworkIdentifier, ".NETFramework", StringComparison.OrdinalIgnoreCase)
			&& (arg.StartsWith("/platform:", StringComparison.OrdinalIgnoreCase)
				|| arg.StartsWith("-platform:", StringComparison.OrdinalIgnoreCase));
	}

	internal static bool TryValidateNetFrameworkReferences(
		string projectFilePath,
		ITaskItem[]? sliceDimensions,
		ITaskItem[]? properties,
		ITaskItem[]? metadataReferences,
		out string unsupportedReason)
	{
		unsupportedReason = string.Empty;
		var targetFramework = new TargetFramework(
			GetItemValue(sliceDimensions, ProjectProperties.TargetFramework) ?? GetItemValue(properties, ProjectProperties.TargetFramework),
			GetItemValue(properties, ProjectProperties.TargetFrameworkIdentifier),
			GetItemValue(properties, ProjectProperties.TargetFrameworkVersion));
		if (!targetFramework.IsNetFramework)
		{
			return true;
		}

		if (metadataReferences == null || metadataReferences.Length == 0)
		{
			unsupportedReason = MissingNetFrameworkReferenceAssembliesReason;
			return false;
		}

		string projectDirectory = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
		CachePathResolver resolver = new CachePathResolver(projectFilePath);
		bool foundCanonicalReferenceAssembly = false;

		foreach (ITaskItem item in metadataReferences)
		{
			if (item == null || string.IsNullOrWhiteSpace(item.ItemSpec))
			{
				continue;
			}

			string absolutePath = Path.IsPathRooted(item.ItemSpec)
				? Path.GetFullPath(item.ItemSpec)
				: Path.GetFullPath(Path.Combine(projectDirectory, item.ItemSpec));
			string portable = resolver.ToPortable(absolutePath);

			if (TryExtractNetFrameworkReferenceAssembly(portable, targetFramework) is not null)
			{
				foundCanonicalReferenceAssembly = true;
				if (!File.Exists(absolutePath))
				{
					unsupportedReason = MissingNetFrameworkReferenceAssembliesReason;
					return false;
				}

				continue;
			}

			if (IsNetFrameworkCoreAssemblyName(Path.GetFileName(item.ItemSpec)) && !File.Exists(absolutePath))
			{
				unsupportedReason = MissingNetFrameworkReferenceAssembliesReason;
				return false;
			}
		}

		if (!foundCanonicalReferenceAssembly)
		{
			unsupportedReason = MissingNetFrameworkReferenceAssembliesReason;
			return false;
		}

		return true;
	}

	private static bool IsRootedSourceFileArgument(string arg)
	{
		if (!Path.IsPathRooted(arg)) return false;
		string extension = Path.GetExtension(arg);
		return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPortableSourceFileArgument(string arg)
	{
		if (!arg.StartsWith(PathSentinels.Path, StringComparison.OrdinalIgnoreCase)
			&& !arg.StartsWith(PathSentinels.Nuget, StringComparison.OrdinalIgnoreCase)
			&& !arg.StartsWith(PathSentinels.Dotnet, StringComparison.OrdinalIgnoreCase)
			&& !arg.StartsWith(PathSentinels.NetSdk, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		string extension = Path.GetExtension(arg);
		return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase);
	}

	private static void WritePreparedPathSection(TextWriter writer, string header, List<string> portables, bool required)
	{
		if (portables.Count == 0 && !required) return;
		writer.WriteLine();
		writer.WriteLine(header);
		if (portables.Count > 0)
		{
			portables.Sort(ComparePortablePaths);
			EmitCompressed(writer, portables, 0);
		}
	}

	private static List<string> PrepareDistinctPortablePaths(
		ITaskItem[]? items,
		CachePathResolver resolver,
		string section,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		if (items == null || items.Length == 0) return [];

		var portables = new List<string>(items.Length);
		var seenPortables = new HashSet<string>(PathComparer);
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			string path = item.ItemSpec;
			if (!string.IsNullOrEmpty(path))
			{
				AddDistinctPortablePath(portables, seenPortables, resolver.ToPortable(path), section, projectFilePath, duplicateItemReporter);
			}
		}

		return portables;
	}

	private static List<string> PrepareDistinctPortablePaths(
		string[]? items,
		CachePathResolver resolver,
		string section,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		if (items == null || items.Length == 0) return [];

		var portables = new List<string>(items.Length);
		var seenPortables = new HashSet<string>(PathComparer);
		foreach (string item in items)
		{
			if (!string.IsNullOrEmpty(item))
			{
				AddDistinctPortablePath(portables, seenPortables, resolver.ToPortable(item), section, projectFilePath, duplicateItemReporter);
			}
		}

		return portables;
	}

	private static void EmitSourceFileSection(
		TextWriter writer,
		ITaskItem[]? items,
		CachePathResolver resolver,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		if (items == null || items.Length == 0)
		{
			// Required item type — write empty section header
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SourceFiles));
			return;
		}

		var sortedPaths = new List<string>(items.Length);
		var lookup = new Dictionary<string, ITaskItem>(PathComparer);
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			string path = item.ItemSpec;
			if (string.IsNullOrEmpty(path)) continue;

			string portable = resolver.ToPortable(path);
			// First occurrence wins. Two ``ITaskItem``s can collapse to the same portable
			// form (e.g. case variations on case-insensitive file systems, or wildcards
			// overlapping explicit ``Include``s with metadata). Without this dedup the
			// trie writer emits the path twice with its metadata block duplicated.
			// ``Dictionary.TryAdd`` is netstandard2.1+ so use the ``ContainsKey`` shape.
			if (!lookup.ContainsKey(portable))
			{
				lookup.Add(portable, item);
				sortedPaths.Add(portable);
			}
			else if (duplicateItemReporter is not null)
			{
				duplicateItemReporter(new ProjectDataDuplicateItemDiagnostic(projectFilePath, CacheFormat.Sections.SourceFiles, portable));
			}
		}

		sortedPaths.Sort(ComparePortablePaths);
		if (sortedPaths.Count == 0) return;

		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SourceFiles));
		EmitCompressedWithMetadata(writer, sortedPaths, 0, "", lookup, EmitSourceFileMetadata);
	}

	private static void EmitProjectReferenceSection(
		TextWriter writer,
		ITaskItem[]? items,
		CachePathResolver resolver,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.ProjectReferences));
		if (items is null || items.Length == 0)
		{
			return;
		}

		var sortedPaths = new List<string>(items.Length);
		var lookup = new Dictionary<string, ITaskItem>(PathComparer);
		foreach (ITaskItem item in items)
		{
			if (item is null || string.IsNullOrEmpty(item.ItemSpec))
			{
				continue;
			}

			string portable = resolver.ToPortable(item.ItemSpec);
			if (lookup.ContainsKey(portable))
			{
				duplicateItemReporter?.Invoke(new ProjectDataDuplicateItemDiagnostic(projectFilePath, CacheFormat.Sections.ProjectReferences, portable));
				continue;
			}

			lookup.Add(portable, item);
			sortedPaths.Add(portable);
		}

		sortedPaths.Sort(ComparePortablePaths);
		EmitCompressedWithMetadata(writer, sortedPaths, 0, "", lookup, EmitProjectReferenceMetadata);
	}

	// Emits a pre-built (already portable + sorted) string-path section.
	private static void EmitSimplePathSection(TextWriter writer, string header, List<string> portables)
	{
		if (portables.Count == 0) return;
		writer.WriteLine();
		writer.WriteLine(header);
		EmitCompressed(writer, portables, 0);
	}

	// Required variant: always writes the header, even when empty.
	private static void EmitSimplePathSectionRequired(TextWriter writer, string header, List<string> portables)
	{
		writer.WriteLine();
		writer.WriteLine(header);
		if (portables.Count > 0)
			EmitCompressed(writer, portables, 0);
	}

	// Converts metadataReferences ITaskItem[] to a sorted portable form. Entries
	// rooted in an SDK .App.Ref pack or a recognized NuGet framework ref pack are diverted into
	// <paramref name="frameworkPacks"/> and dropped from the result; .NET Framework
	// reference assemblies remain in metadataReferences as canonical
	// <NETFXREF>/vX.Y.Z/*.dll entries.
	internal static List<KeyValuePair<string, ITaskItem>> PrepareMetadataRefs(
		ITaskItem[]? items, CachePathResolver resolver, SortedSet<string> frameworkPacks)
		=> PrepareMetadataRefs(items, resolver, frameworkPacks, default);

	internal static List<KeyValuePair<string, ITaskItem>> PrepareMetadataRefs(
		ITaskItem[]? items,
		CachePathResolver resolver,
		SortedSet<string> frameworkPacks,
		TargetFramework targetFramework)
	{
		var result = new List<KeyValuePair<string, ITaskItem>>();
		if (items == null || items.Length == 0) return result;
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			string path = item.ItemSpec;
			if (string.IsNullOrEmpty(path)) continue;
			string portable = resolver.ToPortable(path);
			string? netFrameworkReferenceAssembly = TryExtractNetFrameworkReferenceAssembly(portable, targetFramework);
			if (netFrameworkReferenceAssembly != null)
			{
				portable = NetFxRefPrefix + netFrameworkReferenceAssembly;
			}

			string? packName = TryExtractRefPackName(portable);
			if (packName != null)
			{
				frameworkPacks.Add(packName);
				continue;
			}
			packName = TryExtractNuGetRefPackName(item, portable, targetFramework);
			if (packName != null)
			{
				// Always emit to [frameworkPacks] regardless of resolution location.
				// The same canonical pack (e.g. Microsoft.NETCore.App.Ref) may resolve from
				// <DOTNET>/packs/ on one machine and <NUGET>/ on another depending on which
				// SDKs/targeting packs are installed. Classifying by location produces
				// environment-dependent lscache churn. The reader probes both locations.
				frameworkPacks.Add(packName);
				continue;
			}
			result.Add(new KeyValuePair<string, ITaskItem>(portable, item));
		}
		result.Sort(static (a, b) => ComparePortablePaths(a.Key, b.Key));
		return result;
	}

	// Converts analyzerReferences string[] to a sorted portable form,
	// diverting entries rooted in an SDK .App.Ref pack or a recognized NuGet framework ref pack into the
	// <paramref name="frameworkPacks"/> set (and dropping them from the result).
	internal static List<string> PrepareAnalyzerRefs(
		ITaskItem[]? items, CachePathResolver resolver, SortedSet<string> frameworkPacks)
		=> PrepareAnalyzerRefs(items, resolver, frameworkPacks, default);

	internal static List<string> PrepareAnalyzerRefs(
		ITaskItem[]? items,
		CachePathResolver resolver,
		SortedSet<string> frameworkPacks,
		TargetFramework targetFramework)
		=> PrepareAnalyzerRefs(items, resolver, frameworkPacks, new SortedSet<string>(StringComparer.OrdinalIgnoreCase), sdkKnownAnalyzerPacks: null, targetFramework);

	internal static List<string> PrepareAnalyzerRefs(
		ITaskItem[]? items,
		CachePathResolver resolver,
		SortedSet<string> frameworkPacks,
		SortedSet<string> sdkAnalyzerPacks,
		ITaskItem[]? sdkKnownAnalyzerPacks,
		TargetFramework targetFramework,
		string projectFilePath = "",
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter = null)
	{
		if (items == null || items.Length == 0) return [];

		var result = new List<string>(items.Length);
		var seenResult = new HashSet<string>(PathComparer);
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			string path = item.ItemSpec;
			if (string.IsNullOrEmpty(path)) continue;
			string portable = resolver.ToPortable(path);
			string? packName = TryExtractRefPackName(portable);
			if (packName != null)
			{
				frameworkPacks.Add(packName);
				continue;
			}
			packName = TryExtractNuGetRefPackName(item, portable, targetFramework);
			if (packName != null)
			{
				// See PrepareMetadataRefs for rationale: classify by canonical pack name,
				// not resolution location.
				frameworkPacks.Add(packName);
				continue;
			}
			string? sdkAnalyzerPackName = TryExtractSdkAnalyzerPackName(item, portable, sdkKnownAnalyzerPacks, targetFramework);
			if (sdkAnalyzerPackName != null)
			{
				sdkAnalyzerPacks.Add(sdkAnalyzerPackName);
				continue;
			}
			AddDistinctPortablePath(result, seenResult, portable, CacheFormat.Sections.AnalyzerReferences, projectFilePath, duplicateItemReporter);
		}
		result.Sort(ComparePortablePaths);
		return result;
	}

	private static List<string> ToSortedDistinctPortablePaths(
		IEnumerable<string> paths,
		string section,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		List<string> result = [];
		HashSet<string> seenResult = new(PathComparer);
		foreach (string path in paths)
		{
			AddDistinctPortablePath(result, seenResult, path, section, projectFilePath, duplicateItemReporter);
		}

		result.Sort(ComparePortablePaths);
		return result;
	}

	private static void AddDistinctPortablePath(
		List<string> paths,
		HashSet<string> seenPaths,
		string portablePath,
		string section,
		string projectFilePath,
		Action<ProjectDataDuplicateItemDiagnostic>? duplicateItemReporter)
	{
		if (seenPaths.Add(portablePath))
		{
			paths.Add(portablePath);
		}
		else
		{
			if (duplicateItemReporter is not null)
			{
				duplicateItemReporter(new ProjectDataDuplicateItemDiagnostic(projectFilePath, section, portablePath));
			}
		}
	}

	internal static SortedSet<string> BuildSdkAnalyzerConfigPolicy(ITaskItem[]? items, TargetFramework targetFramework)
	{
		var policies = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
		if (items == null || items.Length == 0) return policies;

		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			if (!string.Equals(item.ItemSpec, "Microsoft.NET.Sdk", StringComparison.OrdinalIgnoreCase)) continue;
			if (IsTrue(GetMetadataValue(item, "SkipGlobalAnalyzerConfigForPackage"))) continue;

			// Only emit each policy line when the SDK actually applies the corresponding
			// analyzer pack. The SDK gates NetAnalyzer DLLs on `$(EnableNETAnalyzers)` and
			// CodeStyle DLLs on `$(EnforceCodeStyleInBuild) And '$(Language)' == 'C#'`
			// (Microsoft.NET.Sdk.Analyzers.targets). Emitting the policy unconditionally
			// produces orphan entries that reference packs with zero DLLs in
			// `[analyzerReferences]`.
			if (IsTrue(GetMetadataValue(item, "EnableNETAnalyzers")))
			{
				policies.Add(BuildNetAnalyzersPolicyLine(item, targetFramework));
			}

			string? languageSegment = TryGetSdkCodeStyleLanguageSegment(GetMetadataValue(item, "Language"));
			if (languageSegment != null && IsTrue(GetMetadataValue(item, "EnforceCodeStyleInBuild")))
			{
				policies.Add(BuildCodeStylePolicyLine(item, languageSegment, targetFramework));
			}
		}

		return policies;
	}

	// Emits the [frameworkPacks] section, listing pack names only (no version).
	// The reader expands each pack via <DOTNET>/packs/<PackName>/<HighestCompat>/data/FrameworkList.xml
	// into managed metadata references and CS analyzer references.
	internal static void WriteFrameworkPacksSection(StringBuilder sb, SortedSet<string> packs)
	{
		using var writer = new StringWriter(sb);
		WriteFrameworkPacksSection(writer, packs);
	}

	internal static void WriteFrameworkPacksSection(TextWriter writer, SortedSet<string> packs)
	{
		if (packs.Count == 0) return;
		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.FrameworkPacks));
		foreach (string name in packs) writer.WriteLine(name);
	}

	internal static void WriteSdkAnalyzerPacksSection(TextWriter writer, SortedSet<string> packs)
	{
		if (packs.Count == 0) return;
		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SdkAnalyzerPacks));
		foreach (string name in packs) writer.WriteLine(name);
	}

	internal static void WriteSdkAnalyzerConfigPolicySection(TextWriter writer, SortedSet<string> policies)
	{
		if (policies.Count == 0) return;
		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SdkAnalyzerConfigPolicy));
		foreach (string policy in policies) writer.WriteLine(policy);
	}

	// Returns the ref-pack name if <paramref name="portablePath"/> is rooted under
	// <DOTNET>/packs/<PackName>/<Version>/... and the pack follows the shared-framework
	// <Name>.App.Ref naming convention; otherwise null. Workload packs use independent
	// version schemes and must remain explicit metadata/analyzer references.
	internal static string? TryExtractRefPackName(string? portablePath)
	{
		if (portablePath == null) return null;
		if (!portablePath.StartsWith(DotNetPacksPrefix, StringComparison.OrdinalIgnoreCase)) return null;
		int nameStart = DotNetPacksPrefix.Length;
		int nameEnd = portablePath.IndexOf('/', nameStart);
		if (nameEnd <= nameStart) return null;
		string packName = portablePath.Substring(nameStart, nameEnd - nameStart);
		if (!packName.EndsWith(".App.Ref", StringComparison.OrdinalIgnoreCase)) return null;
		// Require at least one more '/' after the version segment so we don't
		// misclassify <DOTNET>/packs/Foo/Bar (no file under it).
		int verEnd = portablePath.IndexOf('/', nameEnd + 1);
		if (verEnd < 0) return null;
		return packName;
	}

	// Returns the canonical targeting-pack package name when the item is rooted under
	// <NUGET>/<PackageId>/<Version>/... and MSBuild marked it as a framework-reference asset.
	internal static string? TryExtractNuGetRefPackName(ITaskItem item, string? portablePath, TargetFramework targetFramework)
	{
		if (portablePath == null) return null;
		const string NuGetPrefix = PathSentinels.Nuget + "/";
		if (!portablePath.StartsWith(NuGetPrefix, StringComparison.OrdinalIgnoreCase)) return null;

		int packageStart = NuGetPrefix.Length;
		int packageEnd = portablePath.IndexOf('/', packageStart);
		if (packageEnd <= packageStart) return null;

		int versionStart = packageEnd + 1;
		int versionEnd = portablePath.IndexOf('/', versionStart);
		if (versionEnd <= versionStart) return null;

		string packageId = portablePath.Substring(packageStart, packageEnd - packageStart);
		string packageVersion = portablePath.Substring(versionStart, versionEnd - versionStart);
		string pathUnderPackage = portablePath.Substring(versionEnd + 1);

		if (!IsNuGetRefPackAssetPath(pathUnderPackage, targetFramework)) return null;

		string? canonicalPackageId = TryGetKnownNuGetFrameworkPackPackageId(packageId);
		if (canonicalPackageId == null)
		{
			return null;
		}

		string metadataPackageId = item.GetMetadata("NuGetPackageId");
		string metadataPackageVersion = item.GetMetadata("NuGetPackageVersion");

		if ((!string.IsNullOrWhiteSpace(metadataPackageId)
				&& !string.Equals(metadataPackageId, packageId, StringComparison.OrdinalIgnoreCase))
			|| (!string.IsNullOrWhiteSpace(metadataPackageVersion)
				&& !string.Equals(metadataPackageVersion, packageVersion, StringComparison.OrdinalIgnoreCase)))
		{
			return null;
		}

		// Always return the canonical package id from `TryGetKnownNuGetFrameworkPackPackageId`,
		// never the case-as-NuGet-emitted `metadataPackageId`. NuGet preserves the casing
		// from the package's own `.nuspec`, which has historically varied across .NET
		// SDK versions and feeds (e.g. `microsoft.netcore.app.ref` vs `Microsoft.NETCore.App.Ref`).
		// Echoing that casing through to the cache reintroduces the very environment
		// dependence this PR is eliminating. The canonical id is the single source of
		// truth used elsewhere in the writer and reader.
		return canonicalPackageId;
	}

	internal static string? TryExtractSdkAnalyzerPackName(ITaskItem item, string? portablePath, ITaskItem[]? sdkKnownAnalyzerPacks, TargetFramework targetFramework)
	{
		if (portablePath == null) return null;
		const string NuGetPrefix = PathSentinels.Nuget + "/";
		if (!portablePath.StartsWith(NuGetPrefix, StringComparison.OrdinalIgnoreCase)) return null;

		int packageStart = NuGetPrefix.Length;
		int packageEnd = portablePath.IndexOf('/', packageStart);
		if (packageEnd <= packageStart) return null;

		int versionStart = packageEnd + 1;
		int versionEnd = portablePath.IndexOf('/', versionStart);
		if (versionEnd <= versionStart) return null;

		string packageId = portablePath.Substring(packageStart, packageEnd - packageStart);
		string packageVersion = portablePath.Substring(versionStart, versionEnd - versionStart);
		string pathUnderPackage = portablePath.Substring(versionEnd + 1);
		if (!IsNuGetAnalyzerAssetPath(pathUnderPackage)) return null;

		string metadataPackageId = item.GetMetadata("NuGetPackageId");
		string metadataPackageVersion = item.GetMetadata("NuGetPackageVersion");
		if (!string.Equals(metadataPackageId, packageId, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(metadataPackageVersion, packageVersion, StringComparison.OrdinalIgnoreCase)
			|| !IsSdkKnownAnalyzerPack(sdkKnownAnalyzerPacks, metadataPackageId, targetFramework))
		{
			return null;
		}

		return metadataPackageId;
	}

	internal static string? TryExtractNetFrameworkReferenceAssembly(string? portablePath, TargetFramework targetFramework)
	{
		if (portablePath == null || string.IsNullOrWhiteSpace(targetFramework.VersionString))
		{
			return null;
		}

		if (portablePath.StartsWith(NetFxRefPrefix, StringComparison.OrdinalIgnoreCase))
		{
			string netFxRelative = portablePath.Substring(NetFxRefPrefix.Length);
			int netFxVersionEnd = netFxRelative.IndexOf('/');
			string netFxVersion = netFxVersionEnd >= 0 ? netFxRelative.Substring(0, netFxVersionEnd) : netFxRelative;
			return IsSameNetFrameworkVersion(netFxVersion, targetFramework.VersionString!) && IsNetFrameworkReferenceAssemblyPath(netFxRelative)
				? "v" + targetFramework.VersionString + netFxRelative.Substring(netFxVersion.Length)
				: null;
		}

		if (!portablePath.StartsWith(NuGetPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return TryExtractNetFrameworkReferenceAssemblyFromFrameworkRootPath(portablePath, targetFramework.VersionString!);
		}

		int packageStart = NuGetPrefix.Length;
		int packageEnd = portablePath.IndexOf('/', packageStart);
		if (packageEnd <= packageStart)
		{
			return null;
		}

		string packageId = portablePath.Substring(packageStart, packageEnd - packageStart);
		if (!IsNetFrameworkReferenceAssembliesPackage(packageId))
		{
			return null;
		}

		int versionStart = packageEnd + 1;
		int versionEnd = portablePath.IndexOf('/', versionStart);
		if (versionEnd <= versionStart)
		{
			return null;
		}

		string pathUnderPackage = portablePath.Substring(versionEnd + 1);
		const string buildPrefix = "build/.NETFramework/";
		if (!pathUnderPackage.StartsWith(buildPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		int frameworkVersionStart = buildPrefix.Length;
		int frameworkVersionEnd = pathUnderPackage.IndexOf('/', frameworkVersionStart);
		if (frameworkVersionEnd <= frameworkVersionStart)
		{
			return null;
		}

		string packageFrameworkVersion = pathUnderPackage.Substring(frameworkVersionStart, frameworkVersionEnd - frameworkVersionStart);
		string packageRelativePath = pathUnderPackage.Substring(frameworkVersionStart);
		return IsSameNetFrameworkVersion(packageFrameworkVersion, targetFramework.VersionString!) && IsNetFrameworkReferenceAssemblyPath(packageRelativePath)
			? "v" + targetFramework.VersionString + packageRelativePath.Substring(packageFrameworkVersion.Length)
			: null;
	}

	private static bool IsNuGetRefPackAssetPath(string pathUnderPackage, TargetFramework targetFramework)
	{
		if (string.IsNullOrWhiteSpace(targetFramework.Alias)) return false;

		string refPrefix = "ref/" + targetFramework.Alias + "/";
		if (pathUnderPackage.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		if (!string.IsNullOrWhiteSpace(targetFramework.VersionString))
		{
			refPrefix = "ref/net" + targetFramework.VersionString + "/";
			if (pathUnderPackage.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return pathUnderPackage.StartsWith("analyzers/", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsNuGetAnalyzerAssetPath(string pathUnderPackage)
		=> pathUnderPackage.StartsWith("analyzers/", StringComparison.OrdinalIgnoreCase);

	private static bool IsSdkKnownAnalyzerPack(ITaskItem[]? sdkKnownAnalyzerPacks, string packageId, TargetFramework targetFramework)
	{
		if (sdkKnownAnalyzerPacks == null || string.IsNullOrWhiteSpace(packageId)) return false;

		foreach (ITaskItem item in sdkKnownAnalyzerPacks)
		{
			if (item == null) continue;
			string knownPackageId = item.GetMetadata("PackageId");
			if (string.IsNullOrWhiteSpace(knownPackageId))
			{
				knownPackageId = item.ItemSpec;
			}

			if (!string.Equals(knownPackageId, packageId, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			string knownTargetFramework = item.GetMetadata(ProjectProperties.TargetFramework);
			if (!string.IsNullOrWhiteSpace(knownTargetFramework)
				&& !string.IsNullOrWhiteSpace(targetFramework.Alias)
				&& !string.Equals(knownTargetFramework, targetFramework.Alias, StringComparison.OrdinalIgnoreCase)
				&& !IsSameMajorVersion(knownTargetFramework, targetFramework.Version))
			{
				continue;
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Checks whether a TFM alias from SDK metadata (e.g. <c>"net8.0"</c>, <c>"netcoreapp3.1"</c>) has the
	/// same major version as the given parsed version. Handles both <c>net</c> and <c>netcoreapp</c> prefixes.
	/// </summary>
	private static bool IsSameMajorVersion(string sdkTargetFramework, Version? targetVersion)
	{
		if (targetVersion == null) return false;

		ReadOnlySpan<char> span = sdkTargetFramework.AsSpan();
		if (span.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase))
			span = span.Slice("netcoreapp".Length);
		else if (span.StartsWith("net", StringComparison.OrdinalIgnoreCase))
			span = span.Slice("net".Length);
		else
			return false;

		// Extract digits up to the first non-digit (e.g. '.' or '-')
		int end = 0;
		while (end < span.Length && char.IsDigit(span[end])) end++;
		return end > 0 && int.TryParse(span.Slice(0, end).ToString(), out int major) && major == targetVersion.Major;
	}

	private static bool IsNetFrameworkCoreAssemblyName(string? fileName)
		=> string.Equals(fileName, "mscorlib.dll", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(fileName, "System.dll", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(fileName, "System.Core.dll", StringComparison.OrdinalIgnoreCase);

	private static bool IsNetFrameworkReferenceAssembliesPackage(string packageId)
		=> string.Equals(packageId, "microsoft.netframework.referenceassemblies", StringComparison.OrdinalIgnoreCase)
			|| packageId.StartsWith("microsoft.netframework.referenceassemblies.net", StringComparison.OrdinalIgnoreCase);

	private static bool IsNetFrameworkReferenceAssemblyPath(string relativePath)
		=> relativePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
			&& relativePath.IndexOf('\\') < 0
			&& !relativePath.Contains("../", StringComparison.Ordinal)
			&& !Path.GetFileName(relativePath).Contains("..", StringComparison.Ordinal);

	private static string? TryExtractNetFrameworkReferenceAssemblyFromFrameworkRootPath(string portablePath, string targetVersion)
	{
		const string frameworkMarker = ".NETFramework/";
		int frameworkMarkerIndex = portablePath.IndexOf(frameworkMarker, StringComparison.OrdinalIgnoreCase);
		if (frameworkMarkerIndex < 0)
		{
			return null;
		}

		string relative = portablePath.Substring(frameworkMarkerIndex + frameworkMarker.Length);
		int versionEnd = relative.IndexOf('/');
		if (versionEnd <= 0)
		{
			return null;
		}

		string version = relative.Substring(0, versionEnd);
		return IsSameNetFrameworkVersion(version, targetVersion) && IsNetFrameworkReferenceAssemblyPath(relative)
			? "v" + targetVersion + relative.Substring(version.Length)
			: null;
	}

	/// <summary>
	/// Compares a version extracted from a file path (e.g. <c>"v4.7.2"</c>) with the target framework
	/// version string (e.g. <c>"4.7.2"</c>). Tolerates a leading <c>"v"</c> on either side.
	/// </summary>
	private static bool IsSameNetFrameworkVersion(string pathVersion, string targetFrameworkVersion)
	{
		string left = pathVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? pathVersion.Substring(1) : pathVersion;
		string right = targetFrameworkVersion.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? targetFrameworkVersion.Substring(1) : targetFrameworkVersion;
		return Version.TryParse(left, out Version? leftVersion)
			&& Version.TryParse(right, out Version? rightVersion)
			&& leftVersion.Major == rightVersion.Major
			&& (leftVersion.Minor < 0 ? 0 : leftVersion.Minor) == (rightVersion.Minor < 0 ? 0 : rightVersion.Minor)
			&& (leftVersion.Build < 0 ? 0 : leftVersion.Build) == (rightVersion.Build < 0 ? 0 : rightVersion.Build);
	}

	private static string BuildNetAnalyzersPolicyLine(ITaskItem item, TargetFramework targetFramework)
	{
		var builder = new StringBuilder("Microsoft.NET.Sdk/analyzers");
		string analysisLevel = GetMetadataValue(item, "AnalysisLevel");
		string effectiveAnalysisLevel = GetMetadataValue(item, "EffectiveAnalysisLevel");
		string canonicalAnalysisLevel = CanonicalizeAnalysisLevel(analysisLevel, effectiveAnalysisLevel, targetFramework, out string parsedAnalysisLevelSuffix);
		string analysisLevelSuffix = GetMetadataValue(item, "AnalysisLevelSuffix");
		string codeAnalysisTreatWarningsAsErrors = GetMetadataValue(item, "CodeAnalysisTreatWarningsAsErrors");
		string effectiveCodeAnalysisTreatWarningsAsErrors = GetMetadataValue(item, "EffectiveCodeAnalysisTreatWarningsAsErrors");

		AppendPolicyValue(builder, "AnalysisLevel", canonicalAnalysisLevel);
		AppendPolicyValue(builder, "AnalysisMode", GetMetadataValue(item, "AnalysisMode"));

		SplitAnalysisLevel(canonicalAnalysisLevel, out _, out string parsedCanonicalAnalysisLevelSuffix);
		string effectiveAnalysisLevelSuffix = !string.IsNullOrWhiteSpace(analysisLevelSuffix) ? analysisLevelSuffix : parsedAnalysisLevelSuffix;
		if (!StringEquals(effectiveAnalysisLevelSuffix, parsedCanonicalAnalysisLevelSuffix))
		{
			AppendPolicyValue(builder, "AnalysisLevelSuffix", effectiveAnalysisLevelSuffix);
		}

		string rulesVersion = GetMetadataValue(item, "MicrosoftCodeAnalysisNetAnalyzersRulesVersion");
		if (!StringEquals(rulesVersion, TrimTrailingDotZero(effectiveAnalysisLevel)))
		{
			AppendPolicyValue(builder, "MicrosoftCodeAnalysisNetAnalyzersRulesVersion", rulesVersion);
		}

		AppendPolicyValue(builder, "CodeAnalysisTreatWarningsAsErrors", codeAnalysisTreatWarningsAsErrors);
		if (!StringEquals(effectiveCodeAnalysisTreatWarningsAsErrors, codeAnalysisTreatWarningsAsErrors))
		{
			AppendPolicyValue(builder, "EffectiveCodeAnalysisTreatWarningsAsErrors", effectiveCodeAnalysisTreatWarningsAsErrors);
		}

		return builder.ToString();
	}

	private static string BuildCodeStylePolicyLine(ITaskItem item, string languageSegment, TargetFramework targetFramework)
	{
		var builder = new StringBuilder("Microsoft.NET.Sdk/codestyle/");
		builder.Append(languageSegment);

		string analysisLevel = GetMetadataValue(item, "AnalysisLevel");
		string analysisMode = GetMetadataValue(item, "AnalysisMode");
		string analysisLevelSuffix = GetMetadataValue(item, "AnalysisLevelSuffix");
		string effectiveAnalysisLevel = GetMetadataValue(item, "EffectiveAnalysisLevel");
		string canonicalAnalysisLevel = CanonicalizeAnalysisLevel(analysisLevel, effectiveAnalysisLevel, targetFramework, out string parsedAnalysisLevelSuffix);
		string analysisLevelStyle = GetMetadataValue(item, "AnalysisLevelStyle");
		string analysisModeStyle = GetMetadataValue(item, "AnalysisModeStyle");
		string analysisLevelSuffixStyle = GetMetadataValue(item, "AnalysisLevelSuffixStyle");

		AppendPolicyValue(builder, "AnalysisLevel", canonicalAnalysisLevel);
		AppendPolicyValue(builder, "AnalysisMode", analysisMode);

		if (!StringEquals(analysisLevelStyle, analysisLevel))
		{
			AppendPolicyValue(builder, "AnalysisLevelStyle", analysisLevelStyle);
		}

		if (!StringEquals(analysisModeStyle, analysisMode))
		{
			AppendPolicyValue(builder, "AnalysisModeStyle", analysisModeStyle);
		}

		SplitAnalysisLevel(canonicalAnalysisLevel, out _, out string parsedCanonicalAnalysisLevelSuffix);
		string effectiveAnalysisLevelSuffix = !string.IsNullOrWhiteSpace(analysisLevelSuffix) ? analysisLevelSuffix : parsedAnalysisLevelSuffix;
		if (!StringEquals(effectiveAnalysisLevelSuffix, parsedCanonicalAnalysisLevelSuffix))
		{
			AppendPolicyValue(builder, "AnalysisLevelSuffix", effectiveAnalysisLevelSuffix);
		}

		string styleFallbackSuffix = string.IsNullOrWhiteSpace(analysisLevelStyle) ? effectiveAnalysisLevelSuffix : parsedAnalysisLevelSuffix;
		SplitAnalysisLevel(string.IsNullOrWhiteSpace(analysisLevelStyle) ? analysisLevel : analysisLevelStyle, out _, out string parsedAnalysisLevelSuffixStyle);
		if (!StringEquals(analysisLevelSuffixStyle, parsedAnalysisLevelSuffixStyle) && !StringEquals(analysisLevelSuffixStyle, styleFallbackSuffix))
		{
			AppendPolicyValue(builder, "AnalysisLevelSuffixStyle", analysisLevelSuffixStyle);
		}

		return builder.ToString();
	}

	private static string CanonicalizeAnalysisLevel(string analysisLevel, string effectiveAnalysisLevel, TargetFramework targetFramework, out string parsedAnalysisLevelSuffix)
	{
		SplitAnalysisLevel(analysisLevel, out string analysisLevelPrefix, out parsedAnalysisLevelSuffix);
		string analysisLevelCore = string.IsNullOrWhiteSpace(analysisLevelPrefix) ? analysisLevel : analysisLevelPrefix;
		return IsDefaultAnalysisLevel(analysisLevelCore, effectiveAnalysisLevel, targetFramework)
			? string.Empty
			: analysisLevel;
	}

	private static bool IsDefaultAnalysisLevel(string analysisLevelCore, string effectiveAnalysisLevel, TargetFramework targetFramework)
	{
		if (string.IsNullOrWhiteSpace(analysisLevelCore))
		{
			return true;
		}

		if (targetFramework.Version == null)
		{
			return false;
		}

		if (targetFramework.IsNetFramework)
		{
			return string.Equals(analysisLevelCore, "latest", StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(effectiveAnalysisLevel)
					|| string.Equals(effectiveAnalysisLevel, "latest", StringComparison.OrdinalIgnoreCase));
		}

		if (string.Equals(analysisLevelCore, "latest", StringComparison.OrdinalIgnoreCase))
		{
			return string.IsNullOrWhiteSpace(effectiveAnalysisLevel)
				|| string.Equals(effectiveAnalysisLevel, "latest", StringComparison.OrdinalIgnoreCase)
				|| VersionGreaterThanOrEquals(effectiveAnalysisLevel, targetFramework.Version);
		}

		return VersionEquals(analysisLevelCore, targetFramework.Version);
	}

	private static string? TryGetKnownNuGetFrameworkPackPackageId(string packageId)
	{
		if (string.Equals(packageId, "microsoft.netcore.app.ref", StringComparison.OrdinalIgnoreCase))
		{
			return "Microsoft.NETCore.App.Ref";
		}

		if (string.Equals(packageId, "microsoft.aspnetcore.app.ref", StringComparison.OrdinalIgnoreCase))
		{
			return "Microsoft.AspNetCore.App.Ref";
		}

		if (string.Equals(packageId, "microsoft.windowsdesktop.app.ref", StringComparison.OrdinalIgnoreCase))
		{
			return "Microsoft.WindowsDesktop.App.Ref";
		}

		return null;
	}

	internal static string CanonicalizeSdkAnalyzerConfigPolicyLine(string line, string? targetFrameworkIdentifier, string? targetFrameworkVersion)
	{
		if (!line.StartsWith("Microsoft.NET.Sdk/analyzers", StringComparison.OrdinalIgnoreCase)
			&& !line.StartsWith("Microsoft.NET.Sdk/codestyle/", StringComparison.OrdinalIgnoreCase))
		{
			return line;
		}

		string[] parts = line.Split('|');
		int analysisLevelIndex = FindPolicyValueIndex(parts, "AnalysisLevel");
		if (analysisLevelIndex < 0)
		{
			return line;
		}

		var targetFramework = new TargetFramework(alias: null, targetFrameworkIdentifier, targetFrameworkVersion);
		string analysisLevel = parts[analysisLevelIndex].Substring("AnalysisLevel=".Length);
		string canonicalAnalysisLevel = CanonicalizeAnalysisLevel(analysisLevel, effectiveAnalysisLevel: string.Empty, targetFramework, out string parsedAnalysisLevelSuffix);
		if (StringEquals(canonicalAnalysisLevel, analysisLevel))
		{
			return line;
		}

		var canonicalParts = new List<string>(parts.Length + 1);
		for (int i = 0; i < parts.Length; i++)
		{
			if (i == analysisLevelIndex)
			{
				if (!string.IsNullOrWhiteSpace(canonicalAnalysisLevel))
				{
					canonicalParts.Add("AnalysisLevel=" + EscapePolicyValue(canonicalAnalysisLevel));
				}

				continue;
			}

			canonicalParts.Add(parts[i]);
		}

		if (!string.IsNullOrWhiteSpace(parsedAnalysisLevelSuffix)
			&& FindPolicyValueIndex(parts, "AnalysisLevelSuffix") < 0)
		{
			canonicalParts.Add("AnalysisLevelSuffix=" + EscapePolicyValue(parsedAnalysisLevelSuffix));
		}

		return string.Join("|", canonicalParts);
	}

	private static int FindPolicyValueIndex(string[] parts, string name)
	{
		string prefix = name + "=";
		for (int i = 1; i < parts.Length; i++)
		{
			if (parts[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}

		return -1;
	}

	private static void AppendPolicyValue(StringBuilder builder, string name, string value)
	{
		if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "*Undefined*", StringComparison.Ordinal))
		{
			return;
		}

		builder.Append('|');
		builder.Append(name);
		builder.Append('=');
		builder.Append(EscapePolicyValue(value.Trim()));
	}

	private static string EscapePolicyValue(string value)
	{
		return value
			.Replace("%", "%25")
			.Replace("|", "%7C")
			.Replace("=", "%3D")
			.Replace("\r", string.Empty)
			.Replace("\n", string.Empty);
	}

	private static string GetMetadataValue(ITaskItem item, string name)
		=> item.GetMetadata(name) ?? string.Empty;

	private static bool IsTrue(string value)
		=> string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	private static bool StringEquals(string? left, string? right)
		=> string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);

	private static string TrimTrailingDotZero(string value)
	{
		while (value.EndsWith(".0", StringComparison.Ordinal))
		{
			value = value.Substring(0, value.Length - 2);
		}

		return value;
	}

	private static void SplitAnalysisLevel(string analysisLevel, out string prefix, out string suffix)
	{
		int separator = analysisLevel.IndexOf('-');
		if (separator <= 0 || separator == analysisLevel.Length - 1)
		{
			prefix = string.Empty;
			suffix = string.Empty;
			return;
		}

		prefix = analysisLevel.Substring(0, separator);
		suffix = analysisLevel.Substring(separator + 1);
	}

	private static string? TryGetSdkCodeStyleLanguageSegment(string language)
	{
		if (string.Equals(language, "C#", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(language, "CSharp", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase))
		{
			return "cs";
		}

		return null;
	}

	private static bool VersionEquals(string value, Version version)
	{
		if (!Version.TryParse(value, out Version? parsedVersion))
		{
			return int.TryParse(value, out int major)
				&& major == version.Major
				&& version.Minor == 0;
		}

		int minor = parsedVersion.Minor < 0 ? 0 : parsedVersion.Minor;
		return parsedVersion.Major == version.Major && minor == version.Minor;
	}

	private static bool VersionGreaterThanOrEquals(string value, Version version)
	{
		if (!Version.TryParse(value, out Version? parsedVersion))
		{
			return int.TryParse(value, out int major)
				&& major >= version.Major
				&& version.Minor == 0;
		}

		int minor = parsedVersion.Minor < 0 ? 0 : parsedVersion.Minor;
		return parsedVersion.Major > version.Major
			|| (parsedVersion.Major == version.Major && minor >= version.Minor);
	}

	/// <summary>
	/// Bundles the MSBuild-evaluated target framework properties for a project slice,
	/// pre-computing a parsed <see cref="System.Version"/> from the version string.
	/// </summary>
	internal readonly struct TargetFramework
	{
		/// <summary>The TFM alias, e.g. <c>net8.0</c>, <c>net472</c>. From <c>$(TargetFramework)</c>.</summary>
		public string? Alias { get; }

		/// <summary>E.g. <c>.NETCoreApp</c>, <c>.NETFramework</c>. From <c>$(TargetFrameworkIdentifier)</c>.</summary>
		public string? Identifier { get; }

		/// <summary>E.g. <c>8.0</c>, <c>4.8</c>. From <c>$(TargetFrameworkVersion)</c>, with the leading <c>"v"</c> stripped.</summary>
		public string? VersionString { get; }

		/// <summary>Parsed form of <see cref="VersionString"/>. Null when the raw value is missing or malformed.</summary>
		public Version? Version { get; }

		/// <summary>Whether the target framework identifier is <c>.NETFramework</c>.</summary>
		public bool IsNetFramework => string.Equals(this.Identifier, ".NETFramework", StringComparison.OrdinalIgnoreCase);

		public TargetFramework(string? alias, string? identifier, string? version)
		{
			this.Alias = alias;
			this.Identifier = identifier;

			if (!string.IsNullOrWhiteSpace(version))
			{
				this.VersionString = version!.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version.Substring(1) : version;
				this.Version = Version.TryParse(this.VersionString, out Version v) ? v : null;
			}
		}
	}

	private static string? GetItemValue(ITaskItem[]? items, string itemSpec)
	{
		if (items == null) return null;
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			if (!string.Equals(item.ItemSpec, itemSpec, StringComparison.OrdinalIgnoreCase)) continue;

			string value = item.GetMetadata("Value");
			return string.IsNullOrWhiteSpace(value) ? null : value;
		}

		return null;
	}

	private static void EmitMetadataRefSection(TextWriter writer, List<KeyValuePair<string, ITaskItem>> portableItems)
	{
		// Required item type — always write header
		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.MetadataReferences));
		if (portableItems.Count == 0) return;

		var sortedPaths = new List<string>(portableItems.Count);
		var lookup = new Dictionary<string, ITaskItem>(PathComparer);
		foreach (KeyValuePair<string, ITaskItem> kvp in portableItems)
		{
			// First occurrence wins. See ``EmitSourceFileSection`` for the rationale —
			// two upstream items can collapse to the same portable form.
			if (!lookup.ContainsKey(kvp.Key))
			{
				lookup.Add(kvp.Key, kvp.Value);
				sortedPaths.Add(kvp.Key);
			}
		}

		EmitCompressedWithMetadata(writer, sortedPaths, 0, "", lookup, EmitMetadataReferenceMetadata);
	}

	// Trie-based path compression: groups paths by directory segment so each
	// directory is emitted exactly once with its files and subdirectories nested
	// under it. Output is wire-compatible with CacheFileReader.ExpandCompressedPaths
	// (lines ending in '/' are directory headers pushed onto an indent stack).
	internal static void EmitCompressed(StringBuilder sb, List<string> paths, int indent)
	{
		using var writer = new StringWriter(sb);
		EmitCompressed(writer, paths, indent);
	}

	private static void EmitCompressed(TextWriter writer, IEnumerable<string> paths, int indent)
	{
		PathTrieNode root = BuildTrie(paths);
		EmitTrie(writer, root, indent, lookup: null, emitMetadata: null);
	}

	private static void EmitCompressedWithMetadata(
		TextWriter writer, List<string> paths, int indent,
		string accPrefix, Dictionary<string, ITaskItem> lookup,
		Action<TextWriter, int, ITaskItem> emitMetadata)
	{
		// accPrefix is unused here: the trie carries each leaf's full portable
		// path directly so the metadata lookup does not need a running prefix.
		_ = accPrefix;
		PathTrieNode root = BuildTrie(paths);
		EmitTrie(writer, root, indent, lookup, emitMetadata);
	}

	// Builds a directory-segment trie from already-sorted portable paths.
	// Sentinel tokens (e.g. "<NUGET>", "<DOTNET>") naturally land as a single
	// first segment because they contain no '/' before the next separator.
	private static PathTrieNode BuildTrie(IEnumerable<string> paths)
	{
		var root = new PathTrieNode();
		foreach (string path in paths)
		{
			if (string.IsNullOrEmpty(path)) continue;
			string[] segments = path.Split('/');
			PathTrieNode node = root;
			for (int i = 0; i < segments.Length; i++)
			{
				string seg = segments[i];
				if (seg.Length == 0)
				{
					// Skip empty segments from a leading '/' or doubled separator.
					continue;
				}
				bool isLeaf = i == segments.Length - 1;
				if (isLeaf)
				{
					node.Files.Add(new PathTrieLeaf(seg, path));
				}
				else
				{
					if (!node.Directories.TryGetValue(seg, out PathTrieNode? child))
					{
						child = new PathTrieNode();
						node.Directories.Add(seg, child);
					}
					node = child;
				}
			}
		}
		return root;
	}

	private static void EmitTrie(
		TextWriter writer, PathTrieNode node, int indent,
		Dictionary<string, ITaskItem>? lookup,
		Action<TextWriter, int, ITaskItem>? emitMetadata)
	{
		var directories = new List<(string DisplayName, PathTrieNode? Dir, PathTrieLeaf? File)>(node.Directories.Count);
		var files = new List<(string DisplayName, PathTrieLeaf File)>(node.Files.Count);

		foreach (KeyValuePair<string, PathTrieNode> kvp in node.Directories)
		{
			if (TryCollapseSingleFileSubtree(kvp.Value, kvp.Key, out PathTrieLeaf leaf, out string leafDisplayName))
			{
				directories.Add((leafDisplayName, null, leaf));
				continue;
			}

			// Collapse chains of single-child directories so a/b/c/ emits on one line
			// when each intermediate has exactly one directory child and no files.
			string name = kvp.Key;
			PathTrieNode target = kvp.Value;
			while (target.Files.Count == 0 && target.Directories.Count == 1)
			{
				KeyValuePair<string, PathTrieNode> only = target.Directories.First();
				name = name + "/" + only.Key;
				target = only.Value;
			}
			directories.Add((name, target, null));
		}
		foreach (PathTrieLeaf leaf in node.Files)
		{
			files.Add((leaf.Name, leaf));
		}

		directories.Sort(static (a, b) => ComparePortablePaths(a.DisplayName, b.DisplayName));
		files.Sort(static (a, b) => ComparePortablePaths(a.DisplayName, b.DisplayName));

		// Emit directory-origin entries first and direct files second, with
		// deterministic ordinal-ignore-case ordering within each group. A
		// collapsed single-file subtree is still a directory-origin entry, so
		// package paths under <NUGET>/ keep package-name alphabetical order.
		foreach ((string displayName, PathTrieNode? dir, PathTrieLeaf? file) in directories)
		{
			WriteIndent(writer, indent);
			if (dir is not null)
			{
				writer.Write(displayName);
				writer.WriteLine('/');
				EmitTrie(writer, dir, indent + 1, lookup, emitMetadata);
			}
			else
			{
				PathTrieLeaf leaf = file!.Value;
				writer.WriteLine(displayName);
				if (lookup != null && emitMetadata != null
					&& lookup.TryGetValue(leaf.FullPath, out ITaskItem? refItem))
				{
					emitMetadata(writer, indent + 1, refItem);
				}
			}
		}

		foreach ((string displayName, PathTrieLeaf leaf) in files)
		{
			WriteIndent(writer, indent);
			writer.WriteLine(displayName);
			if (lookup != null && emitMetadata != null
				&& lookup.TryGetValue(leaf.FullPath, out ITaskItem? refItem))
			{
				emitMetadata(writer, indent + 1, refItem);
			}
		}
	}

	private static bool TryCollapseSingleFileSubtree(
		PathTrieNode node, string prefix,
		out PathTrieLeaf leaf, out string displayName)
	{
		while (node.Files.Count == 0 && node.Directories.Count == 1)
		{
			KeyValuePair<string, PathTrieNode> only = node.Directories.First();
			prefix = prefix + "/" + only.Key;
			node = only.Value;
		}

		if (node.Files.Count == 1 && node.Directories.Count == 0)
		{
			leaf = node.Files[0];
			displayName = prefix + "/" + leaf.Name;
			return true;
		}

		leaf = default;
		displayName = "";
		return false;
	}

	private sealed class PathTrieNode
	{
		public SortedDictionary<string, PathTrieNode> Directories { get; } =
			new(PathComparer);
		public List<PathTrieLeaf> Files { get; } = new();
	}

	private readonly struct PathTrieLeaf(string name, string fullPath)
	{
		public string Name { get; } = name;
		public string FullPath { get; } = fullPath;
	}

	private static void EmitSourceFileMetadata(TextWriter writer, int indent, ITaskItem item)
	{
		string link = item.GetMetadata(ProjectItems.Compile.Link);
		if (!string.IsNullOrWhiteSpace(link))
		{
			WriteIndent(writer, indent);
			writer.Write("@link=");
			writer.WriteLine(link.Replace('\\', '/'));
		}
	}

	private static void EmitProjectReferenceMetadata(TextWriter writer, int indent, ITaskItem item)
	{
		string referenceOutputAssembly = item.GetMetadata(ProjectItems.ProjectReference.ReferenceOutputAssembly);
		if (string.Equals(referenceOutputAssembly, "false", StringComparison.OrdinalIgnoreCase))
		{
			WriteIndent(writer, indent);
			writer.WriteLine("@ReferenceOutputAssembly=false");
		}
	}

	private static void EmitEmbeddedResourceSection(TextWriter writer, ITaskItem[]? items, CachePathResolver resolver)
	{
		if (items == null || items.Length == 0) return;

		var sortedPaths = new List<string>(items.Length);
		var lookup = new Dictionary<string, ITaskItem>(PathComparer);
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			string path = item.ItemSpec;
			if (string.IsNullOrEmpty(path)) continue;

			string portable = resolver.ToPortable(path);
			if (!lookup.ContainsKey(portable))
			{
				sortedPaths.Add(portable);
				lookup[portable] = item;
			}
		}

		sortedPaths.Sort(ComparePortablePaths);
		if (sortedPaths.Count == 0) return;

		writer.WriteLine();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.EmbeddedResources));
		EmitCompressedWithMetadata(writer, sortedPaths, 0, "", lookup, EmitEmbeddedResourceMetadata);
	}

	private static void EmitEmbeddedResourceMetadata(TextWriter writer, int indent, ITaskItem item)
	{
		string generator = item.GetMetadata(ProjectItems.EmbeddedResource.Generator);
		if (!string.IsNullOrEmpty(generator))
		{
			WriteIndent(writer, indent);
			writer.Write("@Generator=");
			writer.WriteLine(generator);
		}

		string lastGenOutput = item.GetMetadata(ProjectItems.EmbeddedResource.LastGenOutput);
		if (!string.IsNullOrEmpty(lastGenOutput))
		{
			WriteIndent(writer, indent);
			writer.Write("@LastGenOutput=");
			writer.WriteLine(lastGenOutput);
		}

		string customToolNamespace = item.GetMetadata(ProjectItems.EmbeddedResource.CustomToolNamespace);
		if (!string.IsNullOrEmpty(customToolNamespace))
		{
			WriteIndent(writer, indent);
			writer.Write("@CustomToolNamespace=");
			writer.WriteLine(customToolNamespace);
		}
	}

	private static void EmitMetadataReferenceMetadata(TextWriter writer, int indent, ITaskItem item)
	{
		string aliases = item.GetMetadata("Aliases");
		if (!string.IsNullOrEmpty(aliases) && aliases != "global")
		{
			WriteIndent(writer, indent);
			writer.Write("@aliases=");
			writer.WriteLine(aliases);
		}

		string embedStr = item.GetMetadata("EmbedInteropTypes");
		if (string.Equals(embedStr, "true", StringComparison.OrdinalIgnoreCase))
		{
			WriteIndent(writer, indent);
			writer.WriteLine("@embedInteropTypes");
		}
	}

	private static void WriteIndent(TextWriter writer, int count)
	{
		for (int i = 0; i < count; i++) writer.Write(' ');
	}

	private static List<KeyValuePair<string, string>> ToSortedKvps(ITaskItem[]? items)
	{
		var list = new List<KeyValuePair<string, string>>();
		if (items == null) return list;
		foreach (ITaskItem item in items)
		{
			if (item == null) continue;
			list.Add(new KeyValuePair<string, string>(
				item.ItemSpec ?? string.Empty,
				item.GetMetadata("Value") ?? string.Empty));
		}
		list.Sort(static (a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Key, b.Key));
		return list;
	}
}

internal readonly struct ProjectDataDuplicateItemDiagnostic
{
	public ProjectDataDuplicateItemDiagnostic(string projectFilePath, string section, string itemSpec)
	{
		this.ProjectFilePath = projectFilePath;
		this.Section = section;
		this.ItemSpec = itemSpec;
	}

	public string ProjectFilePath { get; }
	public string Section { get; }
	public string ItemSpec { get; }
}
