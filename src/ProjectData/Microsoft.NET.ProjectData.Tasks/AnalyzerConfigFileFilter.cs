// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

internal static class AnalyzerConfigFileFilter
{
	public static List<string> Prepare(
		string[]? items,
		CachePathResolver resolver,
		ITaskItem[]? sourceFiles,
		bool filterSdkAnalyzerConfigFiles)
	{
		var result = new List<AnalyzerConfigFilePath>();
		if (items == null || items.Length == 0) return new List<string>();
		foreach (string item in items)
		{
			if (string.IsNullOrEmpty(item)) continue;
			string absolute = resolver.ToAbsolutePath(item);
			string portable = resolver.ToPortable(item);
			if (filterSdkAnalyzerConfigFiles && IsSdkAnalyzerConfigFilePath(portable))
			{
				continue;
			}

			result.Add(new AnalyzerConfigFilePath(absolute, portable));
		}

		List<string> sourceDirectories = GetSourceDirectories(sourceFiles, resolver);
		List<string> rootEditorConfigPaths = FindRootEditorConfigPaths(result, sourceDirectories);
		return result
			.Where(item => !IsEditorConfigFilePath(item.AbsolutePath)
				|| IsEditorConfigApplicableToAnySourceFile(item.AbsolutePath, sourceDirectories, rootEditorConfigPaths))
			.Select(item => item.PortablePath)
			.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	internal static bool IsSdkAnalyzerConfigFilePath(string? portablePath)
	{
		if (portablePath == null) return false;

		const string netSdkPrefix = PathSentinels.NetSdk + "/Sdks/Microsoft.NET.Sdk/";
		if (!portablePath.StartsWith(netSdkPrefix, StringComparison.OrdinalIgnoreCase)) return false;

		string sdkRelative = portablePath.Substring(netSdkPrefix.Length);
		const string analyzerConfigPrefix = "analyzers/build/config/";
		if (sdkRelative.StartsWith(analyzerConfigPrefix, StringComparison.OrdinalIgnoreCase))
		{
			string fileName = sdkRelative.Substring(analyzerConfigPrefix.Length);
			if (fileName.IndexOf('/') >= 0) return false;

			return fileName.StartsWith("analysislevel_", StringComparison.OrdinalIgnoreCase)
				&& fileName.EndsWith(".globalconfig", StringComparison.OrdinalIgnoreCase);
		}

		const string codeStylePrefix = "codestyle/";
		if (sdkRelative.StartsWith(codeStylePrefix, StringComparison.OrdinalIgnoreCase))
		{
			string rest = sdkRelative.Substring(codeStylePrefix.Length);
			const string configMarker = "/build/config/";
			int markerIndex = rest.IndexOf(configMarker, StringComparison.OrdinalIgnoreCase);
			if (markerIndex <= 0) return false;

			string language = rest.Substring(0, markerIndex);
			string fileName = rest.Substring(markerIndex + configMarker.Length);
			if (!IsSafeLogicalSegment(language) || fileName.IndexOf('/') >= 0) return false;

			const string stylePrefix = "analysislevelstyle_";
			const string globalConfigSuffix = ".globalconfig";
			return fileName.StartsWith(stylePrefix, StringComparison.OrdinalIgnoreCase)
				&& fileName.EndsWith(globalConfigSuffix, StringComparison.OrdinalIgnoreCase);
		}

		return false;
	}

	private static List<string> GetSourceDirectories(ITaskItem[]? sourceFiles, CachePathResolver resolver)
	{
		var result = new HashSet<string>(StringComparers.Paths);
		if (sourceFiles != null)
		{
			foreach (ITaskItem sourceFile in sourceFiles)
			{
				if (sourceFile == null) continue;

				string itemSpec = sourceFile.ItemSpec;
				if (string.IsNullOrEmpty(itemSpec)) continue;

				string? directory = Path.GetDirectoryName(resolver.ToAbsolutePath(itemSpec));
				if (!string.IsNullOrEmpty(directory))
				{
					result.Add(NormalizePathForComparison(directory));
				}
			}
		}

		if (result.Count == 0)
		{
			result.Add(NormalizePathForComparison(resolver.ProjectDirectory));
		}

		return result.ToList();
	}

	private static List<string> FindRootEditorConfigPaths(
		IEnumerable<AnalyzerConfigFilePath> items,
		IReadOnlyList<string> sourceDirectories)
	{
		return items
			.Select(static item => item.AbsolutePath)
			.Where(IsEditorConfigFilePath)
			.Where(path => sourceDirectories.Any(sourceDirectory =>
				IsSameOrAncestorDirectory(Path.GetDirectoryName(path) ?? string.Empty, sourceDirectory)))
			.Where(EditorConfigHasRootTrue)
			.Select(NormalizePathForComparison)
			.ToList();
	}

	private static bool IsEditorConfigApplicableToAnySourceFile(
		string editorConfigPath,
		IReadOnlyList<string> sourceDirectories,
		IReadOnlyList<string> rootEditorConfigPaths)
	{
		string editorConfigDirectory = Path.GetDirectoryName(editorConfigPath) ?? string.Empty;
		return sourceDirectories.Any(sourceDirectory => IsSameOrAncestorDirectory(editorConfigDirectory, sourceDirectory)
			&& !HasRootEditorConfigBetween(editorConfigPath, editorConfigDirectory, sourceDirectory, rootEditorConfigPaths));
	}

	private static bool HasRootEditorConfigBetween(
		string editorConfigPath,
		string editorConfigDirectory,
		string sourceDirectory,
		IReadOnlyList<string> rootEditorConfigPaths)
	{
		foreach (string rootEditorConfigPath in rootEditorConfigPaths)
		{
			if (PathsEqual(editorConfigPath, rootEditorConfigPath)) continue;

			string rootDirectory = Path.GetDirectoryName(rootEditorConfigPath) ?? string.Empty;
			if (IsSameOrAncestorDirectory(rootDirectory, sourceDirectory) && IsAncestorDirectory(editorConfigDirectory, rootDirectory))
			{
				return true;
			}
		}

		return false;
	}

	private static bool EditorConfigHasRootTrue(string path)
	{
		if (!File.Exists(path)) return false;

		foreach (string line in File.ReadLines(path))
		{
			string trimmed = line.Trim();
			if (trimmed.Length == 0 || trimmed[0] == '#' || trimmed[0] == ';') continue;
			if (trimmed[0] == '[') return false;

			int equals = trimmed.IndexOf('=');
			if (equals < 0) continue;

			string key = trimmed.Substring(0, equals).Trim();
			string value = trimmed.Substring(equals + 1).Trim();
			if (string.Equals(key, "root", StringComparison.OrdinalIgnoreCase)
				&& string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsEditorConfigFilePath(string path)
		=> string.Equals(Path.GetFileName(path), ".editorconfig", StringComparison.OrdinalIgnoreCase);

	private static bool IsSameOrAncestorDirectory(string candidateDirectory, string descendantDirectory)
		=> PathsEqual(candidateDirectory, descendantDirectory) || IsAncestorDirectory(candidateDirectory, descendantDirectory);

	private static bool IsAncestorDirectory(string candidateDirectory, string descendantDirectory)
	{
		if (string.IsNullOrEmpty(candidateDirectory) || string.IsNullOrEmpty(descendantDirectory)) return false;

		string normalizedCandidate = NormalizeDirectoryForComparison(candidateDirectory);
		string normalizedDescendant = NormalizeDirectoryForComparison(descendantDirectory);
		return normalizedDescendant.Length > normalizedCandidate.Length
			&& normalizedDescendant.StartsWith(normalizedCandidate, StringComparisons.Paths);
	}

	private static bool PathsEqual(string left, string right)
	{
		string normalizedLeft = Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string normalizedRight = Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return string.Equals(normalizedLeft, normalizedRight, StringComparisons.Paths);
	}

	private static string NormalizeDirectoryForComparison(string path)
	{
		string normalized = NormalizePathForComparison(path);
		return normalized + Path.DirectorySeparatorChar;
	}

	private static string NormalizePathForComparison(string path)
		=> Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

	private static bool IsSafeLogicalSegment(string value)
		=> !string.IsNullOrWhiteSpace(value)
			&& value.IndexOf("..", StringComparison.Ordinal) < 0
			&& value.IndexOf('/') < 0
			&& value.IndexOf('\\') < 0;

	private readonly struct AnalyzerConfigFilePath
	{
		public AnalyzerConfigFilePath(string absolutePath, string portablePath)
		{
			this.AbsolutePath = absolutePath;
			this.PortablePath = portablePath;
		}

		public string AbsolutePath { get; }
		public string PortablePath { get; }
	}
}
