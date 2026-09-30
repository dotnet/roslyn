// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Reads all per-TFM slice files for a multi-targeting project, parses them into
/// structured sections, deduplicates content shared across TFMs, and writes a
/// single merged file with shared sections at the top and per-TFM diffs after
/// <c>---</c> separators. Invoked from the outer multi-TFM build after all
/// inner builds have completed.
/// </summary>
internal static class ProjectDataMerger
{
	// The major version this merger emits, parsed once from the canonical version header. Used to
	// gate slice preservation: an existing merged cache of a different major is incompatible and its
	// slices must not be re-emitted under this header.
	private static readonly int CurrentMajorVersion =
		ForwardCompat.TryReadVersionHeader(CacheFormat.VersionHeader, out int major, out _) ? major : 2;

	// Section names in output order.
	private static readonly string[] ListSectionNames =
	[
		"commandLineArguments",
		"sourceFiles",
		"frameworkPacks",
		"metadataReferences",
		"sdkAnalyzerPacks",
		"analyzerReferences",
		"sdkAnalyzerConfigPolicy",
		"analyzerConfigFiles",
		"additionalFiles",
		"embeddedResources",
		"projectReferences",
		"capabilities",
	];

	// Sections whose lines use indentation-based path compression: an unindented
	// line establishes a directory/path prefix, and following lines whose indent
	// is greater than the header's belong to that group. The merger must intersect
	// such sections at the *group* level — never line-by-line — or a child line
	// (e.g. " ConsoleApp2.AssemblyInfo.cs") could be hoisted into the shared block
	// without its parent prefix line, producing a corrupt cache file.
	private static readonly HashSet<string> IndentedSections = new(StringComparer.Ordinal)
	{
		"sourceFiles",
		"metadataReferences",
		"analyzerReferences",
		"analyzerConfigFiles",
		"additionalFiles",
		"embeddedResources",
		"projectReferences",
	};

	/// <summary>
	/// Reads all slice files matching <paramref name="sliceGlob"/>, merges them into
	/// <paramref name="outputPath"/> with a banner and --- separators.
	/// Returns the number of slices merged, or 0 if none were found.
	/// </summary>
	/// <remarks>
	/// The glob is expected to follow MSBuild conventions, e.g. <c>obj/**/&lt;project&gt;.csproj.slice</c>.
	/// We split on "**" to get the base directory and the filename to search for recursively.
	/// </remarks>
	public static int Merge(string outputPath, string sliceGlob, string? targetFrameworks = null)
	{
		return Merge(outputPath, FindSlices(sliceGlob), targetFrameworks);
	}

	public static int Merge(string outputPath, IEnumerable<string> sliceFiles, string? targetFrameworks = null, bool preserveExistingSlices = false, string? tempDirectory = null)
	{
		List<string> sortedSliceFiles = sliceFiles
			.Where(static file => !string.IsNullOrWhiteSpace(file))
			.OrderBy(static file => file, StringComparer.OrdinalIgnoreCase)
			.ToList();
		if (sortedSliceFiles.Count == 0) return 0;

		var slices = new List<SliceData>(sortedSliceFiles.Count);
		foreach (string file in sortedSliceFiles)
			slices.Add(ParseSlice(File.ReadAllText(file)));

		if (preserveExistingSlices)
			AddPreservedExistingSlices(outputPath, slices);

		ProjectDataWriter.AtomicWriteStreamed(outputPath, writer => WriteMergedContent(writer, slices, targetFrameworks), tempDirectory);

		return sortedSliceFiles.Count;
	}

	private static void AddPreservedExistingSlices(string outputPath, List<SliceData> slices)
	{
		if (!File.Exists(outputPath))
			return;

		string existingContent = File.ReadAllText(outputPath);

		// Only preserve slices from a SAME-major cache. A different (or unrecognized) major is an
		// incompatible format: parsing it with this version's grammar and re-emitting the slices
		// under our version= header would corrupt data or smuggle future-major content into a
		// current-major file that the reader would then accept. Preserve nothing — the merge writes
		// a clean current-major file from the freshly generated slices instead.
		if (!ForwardCompat.TryReadVersionHeader(existingContent, out int existingMajor, out _)
			|| existingMajor != CurrentMajorVersion)
		{
			return;
		}

		List<SliceData> existingSlices = ParseMergedContent(existingContent);
		if (existingSlices.Count == 0)
			return;

		// Preserve every existing slice for a TFM that wasn't regenerated in THIS build. This path is
		// non-Windows-only (see _ProjectDataPreserveExistingProjectFolderCache) and exists so a slice
		// for a TFM that is OS-conditionally excluded on this machine — e.g.
		// <TargetFrameworks Condition="'$(OS)'=='Windows_NT'">net8.0;net9.0</TargetFrameworks> — is
		// carried forward instead of churning out of the committed cache. We deliberately do NOT filter
		// by the current $(TargetFrameworks): at merge time the task only sees this OS's evaluated TFM
		// list, so an OS-excluded TFM is indistinguishable from a removed one. Permanent removals are
		// cleaned up by the platform that builds the full TFM set (the Windows preserve=false path drops
		// non-regenerated slices); over-filtering here would silently churn the legitimate OS-excluded
		// case the feature exists to protect.
		var currentSliceIdentities = new HashSet<string>(
			slices.Select(GetSliceIdentity).OfType<string>(),
			StringComparer.OrdinalIgnoreCase);

		foreach (SliceData existingSlice in existingSlices)
		{
			string? identity = GetSliceIdentity(existingSlice);
			if (identity is null || currentSliceIdentities.Contains(identity))
				continue;

			slices.Add(existingSlice);
			currentSliceIdentities.Add(identity);
		}
	}

	internal static List<string> FindSlices(string sliceGlob)
	{
		string baseDir;
		string pattern;
		int starStar = sliceGlob.IndexOf("**", StringComparison.Ordinal);
		if (starStar >= 0)
		{
			baseDir = sliceGlob.Substring(0, starStar).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			if (string.IsNullOrEmpty(baseDir)) baseDir = ".";
			string rest = sliceGlob.Substring(starStar + 2).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			pattern = string.IsNullOrEmpty(rest) ? "*" : rest;
		}
		else
		{
			baseDir = Path.GetDirectoryName(sliceGlob) ?? ".";
			if (string.IsNullOrEmpty(baseDir)) baseDir = ".";
			pattern = Path.GetFileName(sliceGlob);
		}

		if (!Directory.Exists(baseDir)) return [];
		var files = new List<string>(Directory.EnumerateFiles(baseDir, pattern, SearchOption.AllDirectories));
		files.Sort(StringComparer.OrdinalIgnoreCase);
		return files;
	}

	/// <summary>
	/// Writes the merged .lscache content directly to <paramref name="writer"/>.
	/// </summary>
	internal static void WriteMergedContent(TextWriter writer, List<SliceData> slices, string? targetFrameworks = null)
	{
		WriteBanner(writer);
		StampPrimarySlice(slices, targetFrameworks);

		if (slices.Count == 1)
		{
			WriteSingleSliceContent(writer, slices[0]);
			return;
		}

		// Compute shared content across all slices.
		List<string> sharedProjectLines = IntersectAllOrdered(slices.Select(s => s.ProjectLines).ToList());
		List<string> sharedProperties = IntersectAllOrdered(slices.Select(s => s.Properties).ToList());
		var sharedSections = new Dictionary<string, List<string>>();
		foreach (string section in ListSectionNames)
		{
			var allLists = slices.Select(s => s.ListSections.TryGetValue(section, out List<string>? v) ? v : []).ToList();
			sharedSections[section] = IndentedSections.Contains(section)
				? FlattenGroups(IntersectAllOrderedGroups(allLists.Select(GroupByIndentation).ToList()))
				: IntersectAllOrdered(allLists);
		}

		// Write shared [project] (no [sliceDimensions]).
		writer.WriteLine();
		WriteProjectSection(writer, sharedProjectLines);

		// Write shared [properties].
		if (sharedProperties.Count > 0)
		{
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Properties));
			foreach (string line in sharedProperties)
				writer.WriteLine(line);
		}

		// Write shared list sections.
		foreach (string section in ListSectionNames)
		{
			if (sharedSections[section].Count > 0)
			{
				writer.WriteLine();
				writer.WriteLine(CacheFormat.SectionHeader(section));
				foreach (string line in sharedSections[section])
					writer.WriteLine(line);
			}
		}

		// Write per-TFM slices (diff only).
		for (int i = 0; i < slices.Count; i++)
		{
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SliceSeparator);
			writer.WriteLine();

			// Per-TFM [project] lines (excluding shared ones).
			var sharedProjectSet = new HashSet<string>(sharedProjectLines, StringComparer.Ordinal);
			WriteProjectSection(writer, slices[i].ProjectLines.Where(line => !sharedProjectSet.Contains(line)), slices[i].IsPrimary);

			// [sliceDimensions] is always per-TFM.
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.SliceDimensions));
			foreach (string line in slices[i].SliceDimensions)
				writer.WriteLine(line);

			// Per-TFM [properties] (excluding shared ones).
			var sharedPropsSet = new HashSet<string>(sharedProperties, StringComparer.Ordinal);
			var diffProps = slices[i].Properties.Where(p => !sharedPropsSet.Contains(p)).ToList();
			if (diffProps.Count > 0)
			{
				writer.WriteLine();
				writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Properties));
				foreach (string line in diffProps)
					writer.WriteLine(line);
			}

			// Per-TFM list sections (excluding shared lines).
			foreach (string section in ListSectionNames)
			{
				List<string> sliceLines = slices[i].ListSections.TryGetValue(section, out List<string>? v) ? v : [];
				List<string> diffLines;
				if (IndentedSections.Contains(section))
				{
					// Group-aware diff: drop entire groups whose stringified content matches a shared group.
					var sharedGroupKeys = new HashSet<string>(
						GroupByIndentation(sharedSections[section]).Select(StringifyGroup),
						StringComparer.Ordinal);
					diffLines = FlattenGroups(GroupByIndentation(sliceLines)
						.Where(g => !sharedGroupKeys.Contains(StringifyGroup(g)))
						.ToList());
				}
				else
				{
					var sharedSet = new HashSet<string>(sharedSections[section], StringComparer.Ordinal);
					diffLines = sliceLines.Where(line => !sharedSet.Contains(line)).ToList();
				}
				if (diffLines.Count > 0)
				{
					writer.WriteLine();
					writer.WriteLine(CacheFormat.SectionHeader(section));
					foreach (string line in diffLines)
						writer.WriteLine(line);
				}
			}
		}
	}

	private static void WriteSingleSliceContent(TextWriter writer, SliceData slice)
	{
		writer.WriteLine();
		// Intentionally omit the ``primary`` marker for single-slice output: the reader's
		// ``ToProjectDto`` falls back to ``slices[0]`` when no slice has ``IsPrimary``, so
		// the marker is redundant when only one slice exists. Skipping it keeps cache
		// files smaller and avoids spurious diffs when a project's TargetFrameworks set
		// shrinks from multi- to single-targeting. The multi-slice path below still
		// writes ``primary`` on the canonical (non-.NETFramework) slice, which is where
		// the marker actually disambiguates between sibling slices.
		WriteProjectSection(writer, slice.ProjectLines, isPrimary: false);

		if (slice.Properties.Count > 0)
		{
			writer.WriteLine();
			writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Properties));
			foreach (string line in slice.Properties)
				writer.WriteLine(line);
		}

		foreach (string section in ListSectionNames)
		{
			if (slice.ListSections.TryGetValue(section, out List<string>? lines) && lines.Count > 0)
			{
				writer.WriteLine();
				writer.WriteLine(CacheFormat.SectionHeader(section));
				foreach (string line in lines)
					writer.WriteLine(line);
			}
		}
	}

	private static void WriteProjectSection(TextWriter writer, IEnumerable<string> projectLines, bool isPrimary = false)
	{
		List<string> lines = projectLines.ToList();
		writer.WriteLine(CacheFormat.SectionHeader(CacheFormat.Sections.Project));
		foreach (string line in lines.Where(static l => l.StartsWith(CacheFormat.ProjectHeaderPrefix, StringComparison.Ordinal)))
			writer.WriteLine(line);
		writer.Write(CacheFormat.LanguagePrefix); writer.WriteLine("C#");
		if (isPrimary)
			writer.WriteLine(CacheFormat.PrimaryMarker);
		foreach (string line in lines.Where(static l => !l.StartsWith(CacheFormat.ProjectHeaderPrefix, StringComparison.Ordinal)))
			writer.WriteLine(line);
	}

	private static void StampPrimarySlice(List<SliceData> slices, string? targetFrameworks)
	{
		if (slices.Count == 0) return;

		foreach (SliceData slice in slices)
			slice.IsPrimary = false;

		SliceData? primarySlice = GetPrimarySlice(slices, targetFrameworks);

		// If the outer build does not provide a usable TargetFrameworks list, fall
		// back to the deterministic slice order supplied by FindSlices/callers.
		(primarySlice ?? slices[0]).IsPrimary = true;
	}

	private static SliceData? GetPrimarySlice(List<SliceData> slices, string? targetFrameworks)
	{
		List<string> orderedTargetFrameworks = GetTargetFrameworks(targetFrameworks);
		SliceData? primarySlice = orderedTargetFrameworks
			.Where(static targetFramework => !IsNetFrameworkTargetFramework(targetFramework))
			.Select(targetFramework => FindSlice(slices, targetFramework))
			.FirstOrDefault(static slice => slice is not null);
		if (primarySlice is not null)
			return primarySlice;

		primarySlice = orderedTargetFrameworks
			.Select(targetFramework => FindSlice(slices, targetFramework))
			.FirstOrDefault(static slice => slice is not null);
		if (primarySlice is not null)
			return primarySlice;

		return slices.FirstOrDefault(slice => !IsNetFrameworkTargetFramework(slice.GetSliceDimension(ProjectProperties.TargetFramework)));
	}

	private static SliceData? FindSlice(List<SliceData> slices, string targetFramework)
	{
		return slices.FirstOrDefault(slice => string.Equals(
			slice.GetSliceDimension(ProjectProperties.TargetFramework),
			targetFramework,
			StringComparison.OrdinalIgnoreCase));
	}

	private static List<string> GetTargetFrameworks(string? targetFrameworks)
	{
		if (string.IsNullOrWhiteSpace(targetFrameworks))
			return [];

		var result = new List<string>();
		foreach (string targetFramework in targetFrameworks!.Split(';'))
		{
			string trimmed = targetFramework.Trim();
			if (trimmed.Length > 0)
				result.Add(trimmed);
		}

		return result;
	}

	private static bool IsNetFrameworkTargetFramework(string? targetFramework)
	{
		if (string.IsNullOrWhiteSpace(targetFramework))
			return false;

		string normalized = targetFramework!.Trim();
		if (!normalized.StartsWith("net", StringComparison.OrdinalIgnoreCase))
			return false;

		string version = normalized.Substring(3);
		return version.Length is 2 or 3
			&& version.All(static c => c >= '0' && c <= '9')
			&& version[0] is >= '1' and <= '4';
	}

	/// <summary>Parses a slice file into structured sections.</summary>
	internal static SliceData ParseSlice(string content)
	{
		var data = new SliceData();
		string? currentSection = null;
		List<string>? currentList = null;

		foreach (string rawLine in content.Split('\n'))
		{
			string line = rawLine.TrimEnd('\r');
			if (line.Length == 0) continue;
			if (line.StartsWith("version=", StringComparison.Ordinal)) continue;
			if (line[0] == CacheFormat.CommentChar) continue;

			if (line[0] == '[' && line[line.Length - 1] == ']')
			{
				currentSection = line.Substring(1, line.Length - 2);
				currentList = null;
				if (currentSection != CacheFormat.Sections.Project && currentSection != CacheFormat.Sections.SliceDimensions && currentSection != CacheFormat.Sections.Properties)
				{
					currentList = [];
					data.ListSections[currentSection] = currentList;
				}
				continue;
			}

			switch (currentSection)
			{
				case CacheFormat.Sections.Project:
					if (string.Equals(line, CacheFormat.PrimaryMarker, StringComparison.Ordinal))
						data.IsPrimary = true;
					else if (!line.StartsWith(CacheFormat.LanguagePrefix, StringComparison.Ordinal))
						data.ProjectLines.Add(line);
					break;
				case CacheFormat.Sections.SliceDimensions:
					data.SliceDimensions.Add(line);
					break;
				case CacheFormat.Sections.Properties:
					data.Properties.Add(line);
					break;
				default:
					currentList?.Add(string.Equals(currentSection, CacheFormat.Sections.SdkAnalyzerConfigPolicy, StringComparison.Ordinal)
						? ProjectDataWriter.CanonicalizeSdkAnalyzerConfigPolicyLine(line, data.GetTargetFrameworkIdentifier(), data.GetTargetFrameworkVersion())
						: line);
					break;
			}
		}

		return data;
	}

	internal static List<SliceData> ParseMergedContent(string content)
	{
		List<string> segments = SplitMergedContent(content);
		if (segments.Count == 0)
			return [];

		if (segments.Count == 1)
		{
			SliceData singleSlice = ParseSlice(segments[0]);
			return singleSlice.SliceDimensions.Count == 0 ? [] : [singleSlice];
		}

		SliceData sharedData = ParseSlice(segments[0]);
		var slices = new List<SliceData>(segments.Count - 1);
		foreach (string segment in segments.Skip(1))
		{
			SliceData sliceDiff = ParseSlice(segment);
			if (sliceDiff.SliceDimensions.Count == 0)
				continue;

			slices.Add(CombineSlices(sharedData, sliceDiff));
		}

		return slices;
	}

	private static List<string> SplitMergedContent(string content)
	{
		var segments = new List<string>();
		var builder = new StringBuilder();
		using var reader = new StringReader(content);
		string? line;
		while ((line = reader.ReadLine()) is not null)
		{
			if (string.Equals(line.TrimEnd('\r'), CacheFormat.SliceSeparator, StringComparison.Ordinal))
			{
				segments.Add(builder.ToString());
				builder.Clear();
				continue;
			}

			builder.AppendLine(line);
		}

		segments.Add(builder.ToString());
		return segments;
	}

	private static SliceData CombineSlices(SliceData sharedData, SliceData sliceDiff)
	{
		var combined = new SliceData { IsPrimary = sliceDiff.IsPrimary };
		combined.ProjectLines.AddRange(sharedData.ProjectLines);
		combined.ProjectLines.AddRange(sliceDiff.ProjectLines);
		combined.SliceDimensions.AddRange(sliceDiff.SliceDimensions);
		combined.Properties.AddRange(sharedData.Properties);
		combined.Properties.AddRange(sliceDiff.Properties);

		foreach (string section in ListSectionNames)
		{
			var lines = new List<string>();
			if (sharedData.ListSections.TryGetValue(section, out List<string>? sharedLines))
				lines.AddRange(sharedLines);
			if (sliceDiff.ListSections.TryGetValue(section, out List<string>? diffLines))
				lines.AddRange(diffLines);
			if (lines.Count > 0)
				combined.ListSections[section] = lines;
		}

		return combined;
	}

	private static string? GetSliceIdentity(SliceData slice)
		=> slice.SliceDimensions.Count == 0
			? null
			: string.Join("\n", slice.SliceDimensions.OrderBy(static line => line, StringComparer.OrdinalIgnoreCase));

	/// <summary>Computes the ordered intersection of multiple lists (preserves order from first).</summary>
	private static List<string> IntersectAllOrdered(List<List<string>> lists)
	{
		if (lists.Count == 0) return [];
		if (lists.Count == 1) return new List<string>(lists[0]);
		var sets = lists.Skip(1).Select(l => new HashSet<string>(l, StringComparer.Ordinal)).ToList();
		return lists[0].Where(item => sets.All(s => s.Contains(item))).ToList();
	}

	/// <summary>
	/// Splits a path-section's lines into "groups": an unindented (indent-0) line
	/// plus every following line whose indent is greater than zero. Each group is
	/// the unit of compression — the unindented line is a path or directory prefix,
	/// and the indented lines below it are continuations (children whose paths
	/// share that prefix, or <c>@</c>-metadata attached to a leaf).
	/// </summary>
	/// <remarks>
	/// Path sections cannot be intersected line-by-line: an indented continuation
	/// has no meaning without its preceding indent-0 header. Multi-TFM merging
	/// must compare these groups whole.
	/// </remarks>
	internal static List<List<string>> GroupByIndentation(List<string> lines)
	{
		var groups = new List<List<string>>();
		List<string>? current = null;
		foreach (string line in lines)
		{
			bool isIndented = line.Length > 0 && line[0] == ' ';
			if (!isIndented)
			{
				current = [line];
				groups.Add(current);
			}
			else
			{
				// Indented continuation: attach to the most recent group, or start
				// a new group if none exists (defensive; shouldn't normally happen).
				if (current == null)
				{
					current = [line];
					groups.Add(current);
				}
				else
				{
					current.Add(line);
				}
			}
		}
		return groups;
	}

	/// <summary>Joins a group's lines with <c>\n</c> for use as a hash/equality key.</summary>
	private static string StringifyGroup(List<string> group) => string.Join("\n", group);

	/// <summary>Flattens a list of groups back into a flat line list.</summary>
	private static List<string> FlattenGroups(List<List<string>> groups)
	{
		var result = new List<string>();
		foreach (List<string> g in groups) result.AddRange(g);
		return result;
	}

	/// <summary>Computes the ordered intersection of grouped lists (preserves group order from first).</summary>
	private static List<List<string>> IntersectAllOrderedGroups(List<List<List<string>>> groupedLists)
	{
		if (groupedLists.Count == 0) return [];
		if (groupedLists.Count == 1) return new List<List<string>>(groupedLists[0]);
		var sets = groupedLists.Skip(1)
			.Select(gl => new HashSet<string>(gl.Select(StringifyGroup), StringComparer.Ordinal))
			.ToList();
		return groupedLists[0].Where(g => sets.All(s => s.Contains(StringifyGroup(g)))).ToList();
	}

	private static void WriteBanner(TextWriter writer)
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

	internal sealed class SliceData
	{
		public List<string> ProjectLines { get; } = [];
		public List<string> SliceDimensions { get; } = [];
		public List<string> Properties { get; } = [];
		public Dictionary<string, List<string>> ListSections { get; } = new(StringComparer.Ordinal);
		public bool IsPrimary { get; set; }

		public string? GetSliceDimension(string name)
		{
			foreach (string line in this.SliceDimensions)
			{
				int equalsIndex = line.IndexOf('=');
				if (equalsIndex > 0 && string.Equals(line.Substring(0, equalsIndex), name, StringComparison.Ordinal))
					return line.Substring(equalsIndex + 1);
			}

			return null;
		}

		public string? GetTargetFramework()
			=> this.GetSliceDimension(ProjectProperties.TargetFramework) ?? GetValue(this.Properties, ProjectProperties.TargetFramework);

		public string? GetTargetFrameworkIdentifier()
			=> GetValue(this.Properties, ProjectProperties.TargetFrameworkIdentifier);

		public string? GetTargetFrameworkVersion()
			=> GetValue(this.Properties, ProjectProperties.TargetFrameworkVersion);

		private static string? GetValue(List<string> lines, string name)
		{
			foreach (string line in lines)
			{
				int equalsIndex = line.IndexOf('=');
				if (equalsIndex > 0 && string.Equals(line.Substring(0, equalsIndex), name, StringComparison.Ordinal))
					return line.Substring(equalsIndex + 1);
			}

			return null;
		}
	}
}
