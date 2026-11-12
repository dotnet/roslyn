// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.NET.ProjectData;

namespace Microsoft.NET.ProjectData.Tasks;

/// <summary>
/// Substitutes well-known absolute paths with sentinel tokens
/// (<c>&lt;NUGET&gt;</c>, <c>&lt;DOTNET&gt;</c>, <c>&lt;NETFXREF&gt;</c>) so that
/// emitted project data is portable across machines. Used both for full path
/// sections (<see cref="ToPortable"/>) and for absolute paths embedded inside
/// property values or command-line arguments (<see cref="MakePortable"/>).
/// </summary>
internal sealed class CachePathResolver
{
	private readonly string[] nugetFolders;
	private readonly string[] dotnetRoots;
	private readonly string? netFxRefRoot;
	private readonly string projectDir;
	private readonly StringComparison pathsComparison;
	private readonly ProjectDataEnvironment environment;

	public CachePathResolver(string projectFilePath)
		: this(projectFilePath, ProjectDataEnvironment.Default)
	{
	}

	internal CachePathResolver(string projectFilePath, ProjectDataEnvironment environment)
	{
		this.environment = environment;
		this.projectDir = Path.GetDirectoryName(projectFilePath) ?? string.Empty;
		bool caseInsensitive = Path.DirectorySeparatorChar == '\\';
		this.pathsComparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		this.nugetFolders = ResolveNuGetFolders();
		this.dotnetRoots = ResolveDotNetRoots();
		this.netFxRefRoot = ResolveNetFxRefRoot();
	}

	// For testing — allows injecting roots directly.
	internal CachePathResolver(string projectDir, string[] nugetFolders, string[] dotnetRoots, string? netFxRefRoot)
	{
		this.environment = ProjectDataEnvironment.Default;
		this.projectDir = projectDir;
		bool caseInsensitive = Path.DirectorySeparatorChar == '\\';
		this.pathsComparison = caseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		this.nugetFolders = nugetFolders;
		this.dotnetRoots = dotnetRoots;
		this.netFxRefRoot = netFxRefRoot;
	}

	internal string ProjectDirectory => this.projectDir;

	// Converts a full or project-relative file path to a portable <SENTINEL>/...
	// form when it falls under one of the well-known roots, or to a project-relative
	// path otherwise.
	public string ToPortable(string inputPath)
	{
		if (string.IsNullOrEmpty(inputPath)) return inputPath;

		string absolutePath = this.ToAbsolutePath(inputPath);

		for (int i = 0; i < this.nugetFolders.Length; i++)
		{
			if (absolutePath.StartsWith(this.nugetFolders[i], this.pathsComparison))
				return PathSentinels.Nuget + "/" + absolutePath.Substring(this.nugetFolders[i].Length).Replace('\\', '/');
		}
		for (int i = 0; i < this.dotnetRoots.Length; i++)
		{
			if (absolutePath.StartsWith(this.dotnetRoots[i], this.pathsComparison))
			{
				string portable = PathSentinels.Dotnet + "/" + absolutePath.Substring(this.dotnetRoots[i].Length).Replace('\\', '/');
				return RewriteSdkPath(portable);
			}
		}
		if (this.netFxRefRoot != null && absolutePath.StartsWith(this.netFxRefRoot, this.pathsComparison))
			return PathSentinels.NetFxRef + "/" + absolutePath.Substring(this.netFxRefRoot.Length).Replace('\\', '/');

		string relative = MakeRelative(this.projectDir, absolutePath).Replace('\\', '/');
		return TryRewriteAsNuGetPp(relative) ?? relative;
	}

	internal string ToAbsolutePath(string inputPath)
	{
		if (string.IsNullOrEmpty(inputPath)) return inputPath;
		return this.environment.GetFullPath(Path.Combine(this.projectDir, inputPath));
	}

	/// <summary>
	/// Detects project-relative paths produced by the SDK's NuGet content-asset preprocessor
	/// (format: <c>obj/{Config}/{TFM}/NuGet/{XxHash3-16hex}/{PackageId}/{Version}/...</c>)
	/// and rewrites them to the <c>&lt;NUGETPP&gt;/{PackageId}/{Version}/...</c> sentinel form.
	/// This makes the cache fully portable: the obj-relative prefix and the environment-dependent
	/// hash are both removed. At read time, the reader resolves <c>&lt;NUGETPP&gt;</c> by scanning
	/// for the actual hash directory under the project's intermediate output path.
	/// </summary>
	internal static string? TryRewriteAsNuGetPp(string relativePath)
	{
		// Pattern: .../NuGet/<16-hex-chars>/<PackageId>/<Version>/...
		// We look for "/NuGet/" followed by exactly 16 hex characters and then "/"
		const string NuGetSegment = "/NuGet/";
		int nugetIdx = relativePath.IndexOf(NuGetSegment, StringComparison.OrdinalIgnoreCase);
		if (nugetIdx < 0) return null;

		int hashStart = nugetIdx + NuGetSegment.Length;
		// Must have at least 16 chars + trailing '/'
		if (hashStart + 17 > relativePath.Length) return null;
		if (relativePath[hashStart + 16] != '/') return null;

		// Validate all 16 characters are hex digits
		for (int i = 0; i < 16; i++)
		{
			char c = relativePath[hashStart + i];
			if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
				return null;
		}

		// Everything after the hash+separator is "<PackageId>/<Version>/..."
		string packageRelativePath = relativePath.Substring(hashStart + 17);
		return PathSentinels.NugetPp + "/" + packageRelativePath;
	}

	// <DOTNET>/sdk/<version>/Sdks/Microsoft.NET.Sdk/analyzers/Foo.dll
	//   → <NETSDK>/Sdks/Microsoft.NET.Sdk/analyzers/Foo.dll
	//
	// The version segment is dropped because:
	//  - the contents of <DOTNET>/sdk/<version>/ are otherwise identical across SDK
	//    patch releases (analyzer DLLs, global configs);
	//  - keeping the version in the cache would invalidate every project's lscache
	//    on every SDK patch with no behavioral benefit.
	//
	// The reader's CachePathResolver requires a caller-supplied SDK binding to
	// resolve <NETSDK>; the cache file itself is intentionally environment-agnostic.
	internal static string RewriteSdkPath(string portable)
	{
		const string SdkPrefix = PathSentinels.Dotnet + "/sdk/";
		if (!portable.StartsWith(SdkPrefix, StringComparison.Ordinal)) return portable;

		int versionEnd = portable.IndexOf('/', SdkPrefix.Length);
		if (versionEnd < 0) return portable;

		return PathSentinels.NetSdk + portable.Substring(versionEnd);
	}

	// Finds an absolute path embedded anywhere inside a property value or
	// command-line argument and replaces the first match with its sentinel form,
	// preserving any surrounding text.
	public string MakePortable(string text)
	{
		if (string.IsNullOrEmpty(text)) return text;

		for (int i = 0; i < this.nugetFolders.Length; i++)
		{
			int idx = text.IndexOf(this.nugetFolders[i], this.pathsComparison);
			if (idx >= 0)
				return FormatEmbedded(text, idx, PathSentinels.Nuget, idx + this.nugetFolders[i].Length);
		}
		for (int i = 0; i < this.dotnetRoots.Length; i++)
		{
			int idx = text.IndexOf(this.dotnetRoots[i], this.pathsComparison);
			if (idx >= 0)
			{
				string portable = FormatEmbedded(text, idx, PathSentinels.Dotnet, idx + this.dotnetRoots[i].Length);
				return RewriteEmbeddedSdkPath(portable, idx);
			}
		}
		if (this.netFxRefRoot != null)
		{
			int idx = text.IndexOf(this.netFxRefRoot, this.pathsComparison);
			if (idx >= 0)
				return FormatEmbedded(text, idx, PathSentinels.NetFxRef, idx + this.netFxRefRoot.Length);
		}

		int pathStart = FindAbsolutePathStart(text);
		if (pathStart >= 0)
		{
			string prefix = text.Substring(0, pathStart);
			string absolutePath = text.Substring(pathStart);
			string relativePath = MakeRelative(this.projectDir, absolutePath).Replace('\\', '/');
			return prefix + PathSentinels.Path + relativePath;
		}

		return text.Replace('\\', '/');
	}

	private static string FormatEmbedded(string source, int prefixLength, string sentinel, int suffixStart)
	{
		string prefix = source.Substring(0, prefixLength);
		string suffix = source.Substring(suffixStart).Replace('\\', '/');
		return prefix + sentinel + "/" + suffix;
	}

	// Same rewrite rule as RewriteSdkPath but for embedded matches: scans the
	// result for "<prefix><DOTNET>/sdk/<ver>/" and rewrites to "<prefix><NETSDK>/".
	// sentinelStart marks where the original prefix ended; we look just after
	// that for the embedded "<DOTNET>" marker so we don't accidentally rewrite
	// unrelated text earlier in the string.
	internal static string RewriteEmbeddedSdkPath(string portable, int sentinelStart)
	{
		const string SdkSegment = PathSentinels.Dotnet + "/sdk/";
		int idx = portable.IndexOf(SdkSegment, sentinelStart, StringComparison.Ordinal);
		if (idx < 0) return portable;

		int versionEnd = portable.IndexOf('/', idx + SdkSegment.Length);
		if (versionEnd < 0) return portable;

		return portable.Substring(0, idx) + PathSentinels.NetSdk + portable.Substring(versionEnd);
	}

	// Finds the start index of an absolute path embedded in text.
	// Windows: looks for `[A-Za-z]:\` or `[A-Za-z]:/` not preceded by a letter-or-digit.
	// Unix: looks for `/<letter>...` at start or after `:`, `"`, or ` `.
	internal static int FindAbsolutePathStart(string text)
	{
		for (int i = 0; i <= text.Length - 3; i++)
		{
			if (char.IsLetter(text[i]) && text[i + 1] == ':' && (text[i + 2] == '\\' || text[i + 2] == '/'))
			{
				if (i == 0 || !char.IsLetterOrDigit(text[i - 1]))
					return i;
			}
		}

		if (Path.DirectorySeparatorChar != '\\')
		{
			for (int i = 0; i < text.Length - 1; i++)
			{
				if (text[i] == '/' && char.IsLetter(text[i + 1]))
				{
					if (i == 0 || text[i - 1] == ':' || text[i - 1] == '"' || text[i - 1] == ' ')
					{
						int nextSlash = text.IndexOf('/', i + 1);
						if (nextSlash > i + 1)
						{
							int colon = text.IndexOf(':', i + 1);
							if (colon >= 0 && colon < nextSlash) continue;
							return i;
						}
					}
				}
			}
		}
		return -1;
	}

	// Returns the longest common directory prefix (ending with '/') of two forward-slash paths,
	// or null if there is no common directory prefix.
	internal static string? FindSharedDirPrefix(string a, string b)
	{
		int minLen = Math.Min(a.Length, b.Length);
		int lastSlash = -1;
		for (int i = 0; i < minLen; i++)
		{
			if (char.ToUpperInvariant(a[i]) != char.ToUpperInvariant(b[i]))
				break;
			if (a[i] == '/')
				lastSlash = i;
		}
		return lastSlash < 0 ? null : a.Substring(0, lastSlash + 1);
	}

	// netstandard2.0-compatible MakeRelative. Both paths must be absolute.
	// Output is '/'-normalized by callers.
	internal static string MakeRelative(string basePath, string fullPath)
	{
		if (string.IsNullOrEmpty(basePath)) return fullPath;
		string normalizedBase = basePath.Replace('\\', '/');
		if (!normalizedBase.EndsWith("/")) normalizedBase += "/";
		string normalizedFull = fullPath.Replace('\\', '/');
		bool ci = Path.DirectorySeparatorChar == '\\';
		StringComparison cmp = ci ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		string[] baseSegs = normalizedBase.TrimEnd('/').Split('/');
		string[] fullSegs = normalizedFull.Split('/');
		int common = 0;
		int limit = Math.Min(baseSegs.Length, fullSegs.Length);
		while (common < limit && string.Equals(baseSegs[common], fullSegs[common], cmp))
			common++;

		var rel = new System.Text.StringBuilder();
		for (int i = common; i < baseSegs.Length; i++)
		{
			if (rel.Length > 0) rel.Append('/');
			rel.Append("..");
		}
		for (int i = common; i < fullSegs.Length; i++)
		{
			if (rel.Length > 0) rel.Append('/');
			rel.Append(fullSegs[i]);
		}
		return rel.Length == 0 ? "." : rel.ToString();
	}

	private string[] ResolveNuGetFolders()
	{
		string? envVal = this.environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!string.IsNullOrWhiteSpace(envVal))
			return [NormalizeFolderPath(envVal, this.environment)];

		string userProfile = this.environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(userProfile))
			return [NormalizeFolderPath(Path.Combine(userProfile, ".nuget", "packages"), this.environment)];

		return [];
	}

	[SuppressMessage("MSBuild.TaskAuthoring", "MSBuildTask0005", Justification = "Candidates are resolved through the supplied ProjectDataEnvironment before probing the file system.")]
	private string[] ResolveDotNetRoots()
	{
		var list = new List<string>();
		void Add(string? candidate)
		{
			if (string.IsNullOrEmpty(candidate)) return;
			string norm;
			try
			{
				norm = NormalizeFolderPath(candidate, this.environment);
			}
			catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
			{
				// Directory.Exists ignored invalid candidates before paths were resolved explicitly.
				return;
			}
			if (!Directory.Exists(norm)) return;
			foreach (var existing in list)
				if (string.Equals(existing, norm, StringComparison.OrdinalIgnoreCase)) return;
			list.Add(norm);
		}

		Add(this.environment.GetEnvironmentVariable("DOTNET_ROOT"));
		Add(TryGetDotNetRootFromHostPath(this.environment.GetEnvironmentVariable("DOTNET_HOST_PATH"), this.environment));
		Add(TryGetDotNetRootFromSdkPath(AppContext.BaseDirectory, this.environment));
		if (Path.DirectorySeparatorChar == '\\')
		{
			string programFiles = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			if (!string.IsNullOrEmpty(programFiles)) Add(Path.Combine(programFiles, "dotnet"));
		}
		else
		{
			Add("/usr/share/dotnet");
			Add("/usr/local/share/dotnet");
		}
		string userHome = this.environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (string.IsNullOrEmpty(userHome)) userHome = this.environment.GetEnvironmentVariable("HOME") ?? string.Empty;
		if (!string.IsNullOrEmpty(userHome)) Add(Path.Combine(userHome, ".dotnet"));
		return list.ToArray();
	}

	internal static string? TryGetDotNetRootFromHostPath(string? hostPath)
		=> TryGetDotNetRootFromHostPath(hostPath, ProjectDataEnvironment.Default);

	private static string? TryGetDotNetRootFromHostPath(string? hostPath, ProjectDataEnvironment environment)
	{
		if (string.IsNullOrWhiteSpace(hostPath) || !Path.IsPathRooted(hostPath)) return null;
		return Path.GetDirectoryName(environment.GetFullPath(hostPath));
	}

	internal static string? TryGetDotNetRootFromSdkPath(string? sdkPath)
		=> TryGetDotNetRootFromSdkPath(sdkPath, ProjectDataEnvironment.Default);

	[SuppressMessage("MSBuild.TaskAuthoring", "MSBuildTask0005", Justification = "The SDK path is resolved through the supplied ProjectDataEnvironment before constructing DirectoryInfo.")]
	private static string? TryGetDotNetRootFromSdkPath(string? sdkPath, ProjectDataEnvironment environment)
	{
		if (string.IsNullOrWhiteSpace(sdkPath)) return null;

		DirectoryInfo sdkVersionDirectory = new DirectoryInfo(environment.GetFullPath(sdkPath));
		DirectoryInfo? sdkDirectory = sdkVersionDirectory.Parent;
		if (sdkDirectory is null || !string.Equals(sdkDirectory.Name, "sdk", StringComparison.OrdinalIgnoreCase)) return null;
		return sdkDirectory.Parent?.FullName;
	}

	[SuppressMessage("MSBuild.TaskAuthoring", "MSBuildTask0005", Justification = "The candidate is resolved through the supplied ProjectDataEnvironment before probing the file system.")]
	private string? ResolveNetFxRefRoot()
	{
		if (Path.DirectorySeparatorChar != '\\') return null;
		string pfx86 = this.environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		if (string.IsNullOrEmpty(pfx86)) return null;
		string candidate = Path.Combine(pfx86, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework");
		candidate = this.environment.GetFullPath(candidate);
		return Directory.Exists(candidate) ? NormalizeFolderPath(candidate, this.environment) : null;
	}

	internal static string NormalizeFolderPath(string path)
		=> NormalizeFolderPath(path, ProjectDataEnvironment.Default);

	private static string NormalizeFolderPath(string path, ProjectDataEnvironment environment)
	{
		string full = environment.GetFullPath(path);
		if (full.Length == 0 || full[full.Length - 1] != Path.DirectorySeparatorChar)
			full += Path.DirectorySeparatorChar;
		return full;
	}
}
