// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Converts between absolute file paths and portable cache representations used in <c>.lscache</c> files.
/// </summary>
/// <remarks>
/// <para>Paths are classified into four categories:</para>
/// <list type="bullet">
///   <item><b>NuGet cache</b> — stored as <c>&lt;NUGET&gt;/package/version/...</c></item>
///   <item><b>Dotnet installation</b> — stored as <c>&lt;DOTNET&gt;/packs/...</c></item>
///   <item><b>.NET Framework reference assemblies</b> — stored as <c>&lt;NETFXREF&gt;/v4.7.2/...</c></item>
///   <item><b>Project-relative</b> — resolved relative to the project directory</item>
/// </list>
/// <para>Adapted from dotnet-project-system's CachePathResolver.cs.</para>
/// </remarks>
public sealed class CachePathResolver
{
	private readonly string[] nugetFolders;
	private readonly string[] dotnetRoots;
	private readonly string? netFxRefRoot;
	private readonly string? netSdkPath;
	private static readonly ConcurrentDictionary<string, Lazy<SdkAnalysisLevelDefaults>> SdkAnalysisLevelDefaultsCache = new(StringComparers.Paths);

	/// <summary>
	/// Constructs a resolver bound only to the ambient environment
	/// (<c>&lt;DOTNET&gt;</c>, <c>&lt;NUGET&gt;</c>, <c>&lt;NETFXREF&gt;</c>).
	/// When the cache file contains <c>&lt;NETSDK&gt;</c> entries they cannot
	/// be resolved — callers should check <see cref="IsNetSdkBound"/> and skip
	/// or warn accordingly.
	/// </summary>
	public CachePathResolver()
	{
		this.nugetFolders = ResolveNuGetFoldersFromEnvironment();
		this.dotnetRoots = ResolveDotNetRootsFromEnvironment();
		this.netFxRefRoot = ResolveNetFxRefRootFromEnvironment();
		this.netSdkPath = null;
	}

	/// <summary>
	/// Constructs a resolver bound to a specific SDK version. The SDK
	/// directory is resolved as <c>&lt;dotnetRoot&gt;/sdk/&lt;sdkVersion&gt;/</c>
	/// against the first dotnet root that contains it. The caller is
	/// responsible for choosing the SDK version that matches the
	/// constraint they are working under (e.g. the version selected by
	/// <c>global.json</c> + roll-forward).
	/// </summary>
	/// <param name="sdkVersion">The exact SDK version, e.g. <c>10.0.202</c>.</param>
	public CachePathResolver(string sdkVersion)
		: this()
	{
		ArgumentException.ThrowIfNullOrEmpty(sdkVersion);
		this.netSdkPath = this.LocateSdkPath(sdkVersion) ?? this.GuessSdkPath(sdkVersion);
	}

	/// <summary>
	/// Constructs a resolver bound to a specific SDK directory. Use this
	/// overload when the caller already has the absolute path; no
	/// directory probing is performed.
	/// </summary>
	/// <param name="sdkVersion">
	/// The SDK version corresponding to <paramref name="sdkPath"/>; recorded
	/// for diagnostics. Not currently used in resolution.
	/// </param>
	/// <param name="sdkPath">Absolute path to the SDK installation directory.</param>
	public CachePathResolver(string sdkVersion, string sdkPath)
		: this()
	{
		ArgumentException.ThrowIfNullOrEmpty(sdkVersion);
		ArgumentException.ThrowIfNullOrEmpty(sdkPath);
		this.netSdkPath = NormalizeFolderPath(sdkPath);
		this.dotnetRoots = AddSdkDotNetRoot(this.dotnetRoots, this.netSdkPath);
	}

	/// <summary>
	/// Test seam: constructs a resolver whose root sets are supplied explicitly
	/// rather than discovered from the ambient process environment. Pass empty
	/// lists / <see langword="null"/> for any root that should not contribute to
	/// resolution. Intended for tests that need full isolation from the system
	/// <c>dotnet</c> / NuGet / .NET Framework reference assembly install
	/// (e.g. so a developer machine with <c>Microsoft.NETCore.App.Ref</c>
	/// installed under <c>C:\Program Files\dotnet\packs\</c> does not leak into
	/// a synthetic test pack lookup). Not intended for production callers —
	/// the public constructors above keep their environment-discovery behaviour
	/// unchanged so the .NET server picks up the host's real installs.
	/// </summary>
	/// <param name="sdkVersion">Optional SDK version for <c>&lt;NETSDK&gt;</c> binding (see other ctors).</param>
	/// <param name="sdkPath">Optional absolute SDK path. When supplied, takes precedence over <paramref name="sdkVersion"/>-based probing.</param>
	/// <param name="dotnetRoots">Explicit list of dotnet install roots; empty means none.</param>
	/// <param name="nugetFolders">Explicit list of NuGet package roots; empty means none.</param>
	/// <param name="netFxRefRoot">Explicit .NET Framework reference assemblies root, or <see langword="null"/> for none. Never discovered from the environment by this constructor.</param>
	internal CachePathResolver(
		string? sdkVersion,
		string? sdkPath,
		IReadOnlyList<string> dotnetRoots,
		IReadOnlyList<string> nugetFolders,
		string? netFxRefRoot)
	{
		ArgumentNullException.ThrowIfNull(dotnetRoots);
		ArgumentNullException.ThrowIfNull(nugetFolders);

		this.dotnetRoots = [.. dotnetRoots.Select(NormalizeFolderPath).Distinct(StringComparers.Paths)];
		this.nugetFolders = [.. nugetFolders.Select(NormalizeFolderPath).Distinct(StringComparers.Paths)];
		this.netFxRefRoot = string.IsNullOrEmpty(netFxRefRoot) ? null : NormalizeFolderPath(netFxRefRoot);

		if (!string.IsNullOrEmpty(sdkPath))
		{
			this.netSdkPath = NormalizeFolderPath(sdkPath);
			this.dotnetRoots = AddSdkDotNetRoot(this.dotnetRoots, this.netSdkPath);
		}
		else if (!string.IsNullOrEmpty(sdkVersion))
		{
			this.netSdkPath = this.LocateSdkPath(sdkVersion) ?? this.GuessSdkPath(sdkVersion);
		}
		else
		{
			this.netSdkPath = null;
		}
	}

	/// <summary>
	/// <see langword="true"/> when this resolver was constructed with an explicit SDK
	/// version or path and can resolve <c>&lt;NETSDK&gt;</c> sentinel paths.
	/// <see langword="false"/> for the default (ambient-only) constructor — callers
	/// must skip or warn on any <c>&lt;NETSDK&gt;</c> entries in the cache file.
	/// </summary>
	public bool IsNetSdkBound => this.netSdkPath is not null;

	private static string[] AddSdkDotNetRoot(string[] dotnetRoots, string sdkPath)
	{
		string? dotnetRoot = TryGetDotNetRootFromSdkPath(sdkPath);
		if (dotnetRoot is null || dotnetRoots.Contains(dotnetRoot, StringComparers.Paths))
		{
			return dotnetRoots;
		}

		return [dotnetRoot, .. dotnetRoots];
	}

	private static string? TryGetDotNetRootFromSdkPath(string sdkPath)
	{
		string sdkDirectory = NormalizeFolderPath(sdkPath);
		string? sdkParent = Path.GetDirectoryName(sdkDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (sdkParent is null || !string.Equals(Path.GetFileName(sdkParent), "sdk", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		string? dotnetRoot = Path.GetDirectoryName(sdkParent);
		return dotnetRoot is null ? null : NormalizeFolderPath(dotnetRoot);
	}

	/// <summary>
	/// Converts a portable cache path back to an absolute path.
	/// </summary>
	public string ToAbsolute(string portablePath, string projectDirectory)
	{
		ArgumentNullException.ThrowIfNull(portablePath);
		ArgumentNullException.ThrowIfNull(projectDirectory);

		// <NUGETPP>/ sentinel requires the project directory to locate the intermediate output.
		if (TryStripNuGetPpSentinel(portablePath, out string ppBody))
		{
			return this.ResolveNuGetPpPath(ppBody, projectDirectory);
		}

		if (this.TryResolveLeadingSentinel(portablePath) is string resolved)
		{
			return resolved;
		}

		// Project-relative path. ``<PATH>`` is not a valid leading sentinel for ``ToAbsolute``;
		// it only appears inline within argument strings (handled by ``MakeAbsolute``).
		return Path.GetFullPath(Path.Join(projectDirectory, portablePath.Replace('/', Path.DirectorySeparatorChar)));
	}

	private static bool TryStripNuGetPpSentinel(string path, out string body)
	{
		if (path.StartsWith(PathSentinels.NugetPp, StringComparison.Ordinal)
			&& path.Length > PathSentinels.NugetPp.Length
			&& path[PathSentinels.NugetPp.Length] == '/')
		{
			body = path[(PathSentinels.NugetPp.Length + 1)..];
			return true;
		}
		body = string.Empty;
		return false;
	}

	/// <summary>
	/// Defensive guard for the relative suffix of a <c>&lt;NUGETPP&gt;</c> sentinel.
	/// Lscache content is writer-generated (not user-supplied), so this is defense in
	/// depth rather than a security boundary — but it ensures the suffix can never
	/// escape the per-hash directory if a malformed cache or stray external lscache
	/// were ever read. Rejects rooted paths and any path containing a <c>..</c>
	/// segment. Returns <see langword="true"/> for safe values.
	/// </summary>
	private static bool IsSafeRelativeNuGetPpSuffix(string body)
	{
		if (string.IsNullOrEmpty(body)) return false;
		// Rooted on Unix (`/foo`), rooted on Windows (`\foo`), or a drive letter (`C:`).
		if (body[0] == '/' || body[0] == '\\') return false;
		if (Path.IsPathRooted(body)) return false;
		// Reject any `..` segment. We split on both separators so this works for
		// either writer output style. We don't reject `.` (current directory) because
		// it cannot escape, and we don't try to canonicalize — any `..` is suspect.
		foreach (string segment in body.Split(['/', '\\']))
		{
			if (segment == "..") return false;
		}
		return true;
	}

	/// <summary>
	/// Resolves a <c>&lt;NUGETPP&gt;/{PackageId}/{Version}/...</c> path by scanning for the
	/// preprocessor hash directory under the project's intermediate output paths.
	/// The SDK writes preprocessed content to <c>obj/{Config}/{TFM}/NuGet/{Hash}/{PackageId}/...</c>.
	/// </summary>
	private string ResolveNuGetPpPath(string packageRelativePath, string projectDirectory)
	{
		string objDir = Path.Combine(projectDirectory, "obj");

		// Reject malformed suffixes (rooted or containing `..`) before doing any I/O.
		// Returning a deliberately non-existent placeholder under `obj/NuGet` keeps
		// the downstream contract intact (a string path that File.Exists will report
		// false on) while ensuring we never combine a rooted or traversal-containing
		// suffix with `objDir`. `Path.Combine(objDir, "NuGet", "<rooted>")` would
		// otherwise discard the prefix entirely.
		if (!IsSafeRelativeNuGetPpSuffix(packageRelativePath))
		{
			return Path.Combine(objDir, "NuGet");
		}

		string nativeSuffix = packageRelativePath.Replace('/', Path.DirectorySeparatorChar);

		// Scan obj/**/NuGet/*/<packageRelativePath> for any configuration/TFM combination.
		try
		{
			if (Directory.Exists(objDir))
			{
				// Prefer NuGet folders under `obj/Debug/` before any other configuration.
				// The committed lscaches are always generated in Debug (the refresh-lscache
				// skill builds Debug, and Aspire/Roslyn/CPS all treat Debug as the universal
				// local-dev configuration), so paths recorded by the writer correspond to a
				// Debug-time restore. Without this pass, a stale Release build on the
				// developer's machine could be picked first purely because the OS enumerates
				// it before Debug, returning content that doesn't match what was recorded.
				string? candidate = TryResolveUnderConfiguration(objDir, nativeSuffix, "Debug");
				if (candidate != null) return candidate;

				// Fallback: any other configuration (Release, custom configs). Rare in
				// practice, but covers checkouts where the user never built Debug locally.
				candidate = TryResolveUnderConfiguration(objDir, nativeSuffix, configurationName: null);
				if (candidate != null) return candidate;
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			// obj/ may not exist yet (e.g. fresh clone before first build)
		}

		// Fallback: return best-guess path (file won't exist but keeps downstream
		// consumers from crashing with a null/empty path).
		return Path.Combine(objDir, "NuGet", nativeSuffix);
	}

	/// <summary>
	/// Scans <paramref name="objDir"/> for <c>NuGet/{hash}/{nativeSuffix}</c> matches.
	/// When <paramref name="configurationName"/> is non-null, only NuGet folders whose
	/// path contains <c>{sep}obj{sep}{configurationName}{sep}</c> are considered. When
	/// it is null, NuGet folders that match a previously-considered configuration are
	/// skipped so the caller can chain "Debug first, others second" without revisiting.
	/// Within matching NuGet folders, hash directories are ordered by descending
	/// last-write time so the most recent restore wins. Returns the first
	/// <c>{hash}/{nativeSuffix}</c> that exists on disk, or <c>null</c> if none match.
	/// </summary>
	private static string? TryResolveUnderConfiguration(string objDir, string nativeSuffix, string? configurationName)
	{
		string? include = configurationName == null
			? null
			: $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}{configurationName}{Path.DirectorySeparatorChar}";
		// When falling back (configurationName == null) we want to skip the Debug pass
		// we already ran. We don't enumerate non-Debug configuration names explicitly
		// because we don't know them; instead we just exclude the Debug segment.
		string excludeDebug = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}";

		foreach (string nugetDir in Directory.EnumerateDirectories(objDir, "NuGet", SearchOption.AllDirectories))
		{
			if (include != null)
			{
				if (nugetDir.IndexOf(include, StringComparison.OrdinalIgnoreCase) < 0) continue;
			}
			else
			{
				if (nugetDir.IndexOf(excludeDebug, StringComparison.OrdinalIgnoreCase) >= 0) continue;
			}

			// Order hash directories by descending last-write time so we resolve
			// against the most recent restore's output. Enumeration order is
			// otherwise OS-defined (alphabetical on NTFS, inode/insertion order
			// elsewhere) and bears no relation to chronology. Picking the newest
			// avoids reading stale preprocessor output if a project property
			// referenced by a `.pp` token (e.g. `$rootnamespace$`) changed
			// between restores and the older hash dir was not rewritten.
			IOrderedEnumerable<string> hashDirsNewestFirst = Directory
				.EnumerateDirectories(nugetDir)
				.OrderByDescending(d => Directory.GetLastWriteTimeUtc(d));
			foreach (string hashDir in hashDirsNewestFirst)
			{
				string candidate = Path.Combine(hashDir, nativeSuffix);
				if (File.Exists(candidate))
					return candidate;
			}
		}
		return null;
	}

	/// <summary>
	/// Replaces a sentinel marker within an arbitrary string with the resolved absolute path prefix.
	/// Used for command-line arguments and property values that may embed sentinel paths.
	/// </summary>
	public string MakeAbsolute(string text, string projectDirectory)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentNullException.ThrowIfNull(projectDirectory);

		// Fast path: no sentinel can be present without an opening ``<``.
		if (text.IndexOf('<') < 0)
		{
			return text;
		}

		// Sentinels are mutually exclusive in practice, so dispatch on the first one found
		// in declaration order rather than scanning all five to find the leftmost. ``MakeAbsolute``
		// is called per command-line argument and per property value during cache load, so the
		// inner loop cost matters. For path-style sentinels (``<NUGET>``, ``<DOTNET>``,
		// ``<NETFXREF>``, ``<NETSDK>``) we require a ``/`` separator after the sentinel;
		// ``<PATH>`` is followed directly by the relative path (writer convention).
		foreach ((string sentinel, bool requiresSeparator) in InlineSentinels)
		{
			int idx = text.IndexOf(sentinel, StringComparison.Ordinal);
			if (idx < 0)
			{
				continue;
			}
			if (requiresSeparator && (idx + sentinel.Length >= text.Length || text[idx + sentinel.Length] != '/'))
			{
				continue;
			}

			string prefix = text[..idx];
			// For path-style sentinels we step past the trailing ``/`` so the resolved path
			// receives just the body (``a/b`` not ``/a/b``). For ``<PATH>`` we take everything
			// after the sentinel itself.
			int bodyStart = idx + sentinel.Length + (requiresSeparator ? 1 : 0);
			string body = text[bodyStart..];
			string resolved = this.ResolveSentinelBody(sentinel, body, projectDirectory);
			return prefix + resolved;
		}

		return text;
	}

	private string ResolveSentinelBody(string sentinel, string body, string projectDirectory) => sentinel switch
	{
		PathSentinels.Nuget => this.ResolveNuGetPath(body),
		PathSentinels.NugetPp => this.ResolveNuGetPpPath(body, projectDirectory),
		PathSentinels.Dotnet => this.ResolveDotNetPath(body),
		PathSentinels.NetFxRef => this.ResolveNetFrameworkReferenceAssembly(body) ?? body.Replace('/', Path.DirectorySeparatorChar),
		PathSentinels.NetSdk => JoinWithNativeSeparators(this.RequireNetSdkPath(), body),
		PathSentinels.Path => Path.GetFullPath(Path.Join(projectDirectory, body.Replace('/', Path.DirectorySeparatorChar))),
		_ => throw new InvalidOperationException($"Unhandled sentinel '{sentinel}'."),
	};

	private static readonly (string Sentinel, bool RequiresSeparator)[] InlineSentinels =
	[
		(PathSentinels.Nuget, true),
		(PathSentinels.NugetPp, true),
		(PathSentinels.Dotnet, true),
		(PathSentinels.NetFxRef, true),
		(PathSentinels.NetSdk, true),
		(PathSentinels.Path, false),
	];

	/// <summary>
	/// Resolves a ``relative`` path (no leading separator) under the configured NuGet folders,
	/// returning the first folder where the file exists, or falling back to the first folder
	/// if none exist on disk (so callers still get a usable absolute path).
	/// </summary>
	private string ResolveNuGetPath(string relative)
	{
		foreach (string nugetFolder in this.nugetFolders)
		{
			string candidate = JoinWithNativeSeparators(nugetFolder, relative);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		if (this.nugetFolders.Length > 0)
		{
			return JoinWithNativeSeparators(this.nugetFolders[0], relative);
		}

		return relative.Replace('/', Path.DirectorySeparatorChar);
	}

	/// <summary>
	/// If <paramref name="portablePath"/> starts with a known path-style sentinel followed by
	/// a ``/`` separator, returns the resolved absolute path. Otherwise returns ``null``.
	/// Malformed sentinels (no trailing ``/``, or end-of-string) are rejected so the caller
	/// can fall back to project-relative resolution rather than silently dropping a character.
	/// </summary>
	private string? TryResolveLeadingSentinel(string portablePath)
	{
		if (TryStripSentinel(portablePath, PathSentinels.Nuget, out string body))
		{
			return this.ResolveNuGetPath(body);
		}
		if (TryStripSentinel(portablePath, PathSentinels.Dotnet, out body))
		{
			return this.ResolveDotNetPath(body);
		}
		if (TryStripSentinel(portablePath, PathSentinels.NetFxRef, out body))
		{
			return this.ResolveNetFrameworkReferenceAssembly(body) ?? body.Replace('/', Path.DirectorySeparatorChar);
		}
		if (TryStripSentinel(portablePath, PathSentinels.NetSdk, out body))
		{
			return JoinWithNativeSeparators(this.RequireNetSdkPath(), body);
		}
		return null;

		static bool TryStripSentinel(string s, string sentinel, out string body)
		{
			if (s.StartsWith(sentinel, StringComparison.Ordinal)
				&& s.Length > sentinel.Length
				&& s[sentinel.Length] == '/')
			{
				body = s[(sentinel.Length + 1)..];
				return true;
			}
			body = string.Empty;
			return false;
		}
	}

	private string ResolveDotNetPath(string relativePart)
	{
		foreach (string root in this.dotnetRoots)
		{
			string candidate = JoinWithNativeSeparators(root, relativePart);
			if (File.Exists(candidate))
			{
				return candidate;
			}
		}

		if (this.dotnetRoots.Length > 0)
		{
			return JoinWithNativeSeparators(this.dotnetRoots[0], relativePart);
		}

		return relativePart.Replace('/', Path.DirectorySeparatorChar);
	}

	private string RequireNetSdkPath()
	{
		if (this.netSdkPath is null)
		{
			throw new InvalidOperationException(
				"This CachePathResolver was constructed without an SDK binding and cannot resolve <NETSDK> paths. " +
				"Check IsNetSdkBound before calling ToAbsolute on paths that may contain the <NETSDK> sentinel.");
		}
		return this.netSdkPath;
	}

	private string? LocateSdkPath(string sdkVersion)
	{
		foreach (string root in this.dotnetRoots)
		{
			string candidate = Path.Join(root, "sdk", sdkVersion);
			if (Directory.Exists(candidate))
			{
				return NormalizeFolderPath(candidate);
			}
		}
		return null;
	}

	private string GuessSdkPath(string sdkVersion)
	{
		// No installed match found. Construct a best-effort absolute path under the
		// first known dotnet root so callers get a path that is consistent with other
		// <DOTNET>-rooted resolutions (resolution failures here mirror the existing
		// "file not found" behavior of <DOTNET> sentinel handling — the caller deals
		// with missing-on-disk via DTB regeneration, not by us throwing).
		string root = this.dotnetRoots.Length > 0 ? this.dotnetRoots[0] : string.Empty;
		return NormalizeFolderPath(Path.Join(root, "sdk", sdkVersion));
	}

	/// <summary>
	/// Locates the on-disk directory for an SDK ref pack.
	/// </summary>
	/// <param name="packName">The pack name, e.g. <c>Microsoft.NETCore.App.Ref</c>.</param>
	/// <param name="targetFramework">
	/// The slice's TFM (e.g. <c>net10.0</c>). When non-null and parseable, only pack
	/// versions whose major matches the TFM major are considered. When null or unparseable,
	/// the highest installed version is returned.
	/// </param>
	/// <returns>
	/// The absolute path of the chosen pack version directory (e.g.
	/// <c>C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\10.0.7</c>),
	/// or <see langword="null"/> if no installed version is found under any dotnet root.
	/// </returns>
	public string? FindRefPackDirectory(string packName, string? targetFramework)
	{
		int? requiredMajor = ParseTfmMajor(targetFramework);

		string? bestPath = null;
		PackageVersion? bestVer = null;

		foreach (string root in this.dotnetRoots)
		{
			string packDir = Path.Join(root, "packs", packName);
			if (!Directory.Exists(packDir)) continue;

			foreach (string versionDir in Directory.EnumerateDirectories(packDir))
			{
				string versionName = Path.GetFileName(versionDir);
				if (!TryParsePackageVersion(versionName, out PackageVersion ver)) continue;
				if (requiredMajor.HasValue && ver.Major != requiredMajor.Value) continue;
				if (bestVer is null || ver > bestVer.Value)
				{
					bestVer = ver;
					bestPath = versionDir;
				}
			}
		}

		return bestPath;
	}

	/// <summary>
	/// Locates the on-disk directory for an SDK-known ref pack restored through NuGet.
	/// </summary>
	/// <param name="packName">The targeting pack package name, e.g. <c>Microsoft.NETCore.App.Ref</c>.</param>
	/// <param name="targetFramework">The slice's TFM, e.g. <c>net8.0</c>.</param>
	/// <returns>
	/// The absolute path of the chosen NuGet package directory, or <see langword="null"/> if
	/// no matching package is found under any configured NuGet package root.
	/// </returns>
	public string? FindNuGetFrameworkPackDirectory(string packName, string? targetFramework)
	{
		if (string.IsNullOrWhiteSpace(packName))
		{
			return null;
		}

		if (SdkKnownPackResolver.TryGetTargetingPackVersionForSdk(this.netSdkPath, packName, targetFramework, out string? sdkVersion))
		{
			string? exactPath = this.FindNuGetPackageDirectory(packName, sdkVersion);
			if (exactPath is not null)
			{
				return exactPath;
			}
		}

		return this.FindHighestNuGetPackageDirectory(packName, ParseTfmMajor(targetFramework));
	}

	/// <summary>
	/// Locates the on-disk directory for an SDK-known analyzer package restored through NuGet.
	/// </summary>
	/// <param name="packageId">The analyzer package ID, e.g. <c>Microsoft.NET.ILLink.Tasks</c>.</param>
	/// <param name="targetFramework">The slice's TFM, e.g. <c>net10.0</c>.</param>
	/// <returns>
	/// The absolute path of the chosen NuGet package directory, or <see langword="null"/> if
	/// no matching package is found under any configured NuGet package root.
	/// </returns>
	public string? FindSdkAnalyzerPackDirectory(string packageId, string? targetFramework)
	{
		if (string.IsNullOrWhiteSpace(packageId))
		{
			return null;
		}

		if (SdkKnownPackResolver.TryGetSdkAnalyzerPackVersionForSdk(this.netSdkPath, packageId, targetFramework, out string? sdkVersion))
		{
			string? exactPath = this.FindNuGetPackageDirectory(packageId, sdkVersion);
			if (exactPath is not null)
			{
				return exactPath;
			}
		}

		return this.FindHighestNuGetPackageDirectory(packageId, ParseTfmMajor(targetFramework));
	}

	public string? ResolveNetFrameworkReferenceAssembly(string entry)
	{
		if (!IsSafeNetFrameworkReferenceAssemblyEntry(entry))
		{
			return null;
		}

		string? developerPackPath = this.ResolveNetFrameworkDeveloperPackPath(entry);
		if (developerPackPath is not null)
		{
			return developerPackPath;
		}

		return this.ResolveNetFrameworkNuGetReferenceAssemblyPath(entry);
	}

	public bool TryResolveNetFrameworkReferenceAssemblyPath(string portablePath, out string? resolvedPath)
	{
		const string prefix = PathSentinels.NetFxRef + "/";
		if (!portablePath.StartsWith(prefix, StringComparison.Ordinal))
		{
			resolvedPath = null;
			return false;
		}

		resolvedPath = this.ResolveNetFrameworkReferenceAssembly(portablePath[prefix.Length..]);
		return true;
	}

	private string? ResolveNetFrameworkDeveloperPackPath(string entry)
	{
		if (this.netFxRefRoot is null)
		{
			return null;
		}

		string candidate = JoinWithNativeSeparators(this.netFxRefRoot, entry);
		return File.Exists(candidate) ? candidate : null;
	}

	private string? ResolveNetFrameworkNuGetReferenceAssemblyPath(string entry)
	{
		int slash = entry.IndexOf('/');
		if (slash <= 0)
		{
			return null;
		}

		string? packageDirectory = this.ResolveNetFrameworkNuGetReferenceAssemblyPackage(entry[..slash]);
		if (packageDirectory is null)
		{
			return null;
		}

		string candidate = Path.Join(packageDirectory, "build", ".NETFramework", entry.Replace('/', Path.DirectorySeparatorChar));
		return File.Exists(candidate) ? candidate : null;
	}

	private string? ResolveNetFrameworkNuGetReferenceAssemblyPackage(string version)
	{
		string? packageId = GetNetFrameworkReferenceAssembliesPackageId(version);
		if (packageId is not null)
		{
			string? packageDirectory = this.FindHighestNuGetPackageDirectory(packageId, requiredMajor: null);
			if (packageDirectory is not null)
			{
				return packageDirectory;
			}
		}

		return this.FindHighestNuGetPackageDirectory("Microsoft.NETFramework.ReferenceAssemblies", requiredMajor: null);
	}

	/// <summary>
	/// Resolves a stable SDK analyzer-config policy to the selected SDK's concrete globalconfig files.
	/// Missing SDK bindings or missing files are represented by an empty result.
	/// </summary>
	public IEnumerable<string> ResolveSdkAnalyzerConfigPolicy(string policy, string? targetFramework)
	{
		if (this.netSdkPath is null || string.IsNullOrWhiteSpace(policy))
		{
			yield break;
		}

		if (!TryParseSdkAnalyzerConfigPolicy(policy, out string? identity, out Dictionary<string, string>? properties))
		{
			yield break;
		}

		if (string.Equals(identity, "Microsoft.NET.Sdk/analyzers", StringComparison.OrdinalIgnoreCase))
		{
			string? file = this.ResolveNetAnalyzersConfigFile(properties, targetFramework);
			if (file is not null)
			{
				yield return file;
			}

			yield break;
		}

		const string codeStylePrefix = "Microsoft.NET.Sdk/codestyle/";
		if (identity.StartsWith(codeStylePrefix, StringComparison.OrdinalIgnoreCase))
		{
			string language = identity[codeStylePrefix.Length..];
			if (!IsSafePathSegment(language))
			{
				yield break;
			}

			string? file = this.ResolveCodeStyleConfigFile(language, properties, targetFramework);
			if (file is not null)
			{
				yield return file;
			}
		}
	}

	private string? ResolveNetAnalyzersConfigFile(Dictionary<string, string> properties, string? targetFramework)
	{
		SdkAnalysisLevelDefaults defaults = this.GetSdkAnalysisLevelDefaults();
		string analysisLevel = GetPolicyValue(properties, "AnalysisLevel");
		SplitAnalysisLevel(analysisLevel, out _, out string analysisLevelSuffix);
		string effectiveAnalysisLevel = ComputeEffectiveAnalysisLevel(analysisLevel, targetFramework, defaults);
		string rulesVersion = GetPolicyValue(properties, "MicrosoftCodeAnalysisNetAnalyzersRulesVersion");
		if (string.IsNullOrWhiteSpace(rulesVersion))
		{
			rulesVersion = TrimTrailingDotZero(effectiveAnalysisLevel);
		}

		if (string.IsNullOrWhiteSpace(rulesVersion))
		{
			return null;
		}

		string mode = GetPolicyValue(properties, "AnalysisLevelSuffix");
		if (string.IsNullOrWhiteSpace(mode))
		{
			mode = analysisLevelSuffix;
		}

		if (string.IsNullOrWhiteSpace(mode))
		{
			mode = GetPolicyValue(properties, "AnalysisMode");
		}

		mode = NormalizeAnalysisMode(mode);
		string effectiveWarningsAsErrors = GetPolicyValue(properties, "EffectiveCodeAnalysisTreatWarningsAsErrors");
		if (string.IsNullOrWhiteSpace(effectiveWarningsAsErrors))
		{
			effectiveWarningsAsErrors = GetPolicyValue(properties, "CodeAnalysisTreatWarningsAsErrors");
		}

		string warnAsErrorSuffix = string.Equals(effectiveWarningsAsErrors, "true", StringComparison.OrdinalIgnoreCase)
			? "_warnaserror"
			: string.Empty;
		string fileName = $"analysislevel_{rulesVersion.Replace(".", "_")}_{mode}{warnAsErrorSuffix}.globalconfig".ToLowerInvariant();
		string file = Path.Join(this.netSdkPath, "Sdks", "Microsoft.NET.Sdk", "analyzers", "build", "config", fileName);
		return File.Exists(file) ? file : null;
	}

	private string? ResolveCodeStyleConfigFile(string language, Dictionary<string, string> properties, string? targetFramework)
	{
		SdkAnalysisLevelDefaults defaults = this.GetSdkAnalysisLevelDefaults();
		string analysisLevel = GetPolicyValue(properties, "AnalysisLevel");
		string analysisMode = GetPolicyValue(properties, "AnalysisMode");
		string analysisLevelStyle = GetPolicyValue(properties, "AnalysisLevelStyle");
		if (string.IsNullOrWhiteSpace(analysisLevelStyle))
		{
			analysisLevelStyle = analysisLevel;
		}

		string analysisModeStyle = GetPolicyValue(properties, "AnalysisModeStyle");
		if (string.IsNullOrWhiteSpace(analysisModeStyle))
		{
			analysisModeStyle = analysisMode;
		}

		SplitAnalysisLevel(analysisLevel, out _, out string analysisLevelSuffix);
		SplitAnalysisLevel(analysisLevelStyle, out _, out string analysisLevelSuffixFromStyle);
		string analysisLevelSuffixStyle = GetPolicyValue(properties, "AnalysisLevelSuffixStyle");
		if (string.IsNullOrWhiteSpace(analysisLevelSuffixStyle))
		{
			analysisLevelSuffixStyle = !string.IsNullOrWhiteSpace(analysisLevelSuffixFromStyle)
				? analysisLevelSuffixFromStyle
				: GetPolicyValue(properties, "AnalysisLevelSuffix");
		}

		if (string.IsNullOrWhiteSpace(analysisLevelSuffixStyle))
		{
			analysisLevelSuffixStyle = analysisLevelSuffix;
		}

		string effectiveAnalysisLevelStyle = ComputeEffectiveAnalysisLevel(analysisLevelStyle, targetFramework, defaults);
		bool shouldInclude =
			!string.Equals(analysisLevelStyle, analysisLevel, StringComparison.OrdinalIgnoreCase)
			|| !string.Equals(analysisModeStyle, analysisMode, StringComparison.OrdinalIgnoreCase)
			|| VersionGreaterThanOrEquals(effectiveAnalysisLevelStyle, "11.0");

		if (!shouldInclude)
		{
			return null;
		}

		string mode = !string.IsNullOrWhiteSpace(analysisModeStyle) ? analysisModeStyle : analysisLevelSuffixStyle;
		mode = NormalizeAnalysisMode(mode);
		string file = Path.Join(this.netSdkPath, "Sdks", "Microsoft.NET.Sdk", "codestyle", language, "build", "config", $"analysislevelstyle_{mode.ToLowerInvariant()}.globalconfig");
		return File.Exists(file) ? file : null;
	}

	private static bool TryParseSdkAnalyzerConfigPolicy(string policy, out string identity, out Dictionary<string, string> properties)
	{
		identity = string.Empty;
		properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		string[] segments = policy.Split('|');
		if (segments.Length == 0 || string.IsNullOrWhiteSpace(segments[0]))
		{
			return false;
		}

		identity = segments[0];
		for (int i = 1; i < segments.Length; i++)
		{
			string segment = segments[i];
			int equals = segment.IndexOf('=');
			if (equals <= 0)
			{
				continue;
			}

			string name = segment[..equals];
			string value = UnescapePolicyValue(segment[(equals + 1)..]);
			if (!string.IsNullOrWhiteSpace(name))
			{
				properties[name] = value;
			}
		}

		return true;
	}

	private static string UnescapePolicyValue(string value)
	{
		return value
			.Replace("%3D", "=", StringComparison.OrdinalIgnoreCase)
			.Replace("%7C", "|", StringComparison.OrdinalIgnoreCase)
			.Replace("%25", "%", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetPolicyValue(Dictionary<string, string> properties, string name)
		=> properties.TryGetValue(name, out string? value) ? value : string.Empty;

	private SdkAnalysisLevelDefaults GetSdkAnalysisLevelDefaults()
	{
		if (this.netSdkPath is null)
		{
			return SdkAnalysisLevelDefaults.CreateFallback(null);
		}

		// Wrap the IO+parse step in Lazy so ConcurrentDictionary contention can't run
		// ParseSdkAnalysisLevelDefaults more than once for the same SDK path.
		return SdkAnalysisLevelDefaultsCache.GetOrAdd(
			this.netSdkPath,
			static path => new Lazy<SdkAnalysisLevelDefaults>(() => ParseSdkAnalysisLevelDefaults(path), LazyThreadSafetyMode.ExecutionAndPublication)).Value;
	}

	private static SdkAnalysisLevelDefaults ParseSdkAnalysisLevelDefaults(string sdkPath)
	{
		string targetsPath = Path.Join(sdkPath, "Sdks", "Microsoft.NET.Sdk", "targets", "Microsoft.NET.Sdk.Analyzers.targets");
		if (!File.Exists(targetsPath))
		{
			return SdkAnalysisLevelDefaults.CreateFallback(ParseSdkMajor(sdkPath));
		}

		try
		{
			string content = File.ReadAllText(targetsPath);
			string? latest = TryReadSimpleElementValue(content, "_LatestAnalysisLevel");
			string? preview = TryReadSimpleElementValue(content, "_PreviewAnalysisLevel");
			string? none = TryReadSimpleElementValue(content, "_NoneAnalysisLevel");
			SdkAnalysisLevelDefaults fallback = SdkAnalysisLevelDefaults.CreateFallback(ParseSdkMajor(sdkPath));
			return new SdkAnalysisLevelDefaults(
				string.IsNullOrWhiteSpace(none) ? fallback.None : none!,
				string.IsNullOrWhiteSpace(latest) ? fallback.Latest : latest!,
				string.IsNullOrWhiteSpace(preview) ? fallback.Preview : preview!);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			System.Diagnostics.Trace.TraceWarning(
				"[lscache] Failed to read SDK analysis-level defaults for {0}: {1}",
				sdkPath,
				ex.Message);
			return SdkAnalysisLevelDefaults.CreateFallback(ParseSdkMajor(sdkPath));
		}
	}

	private static string? TryReadSimpleElementValue(string content, string elementName)
	{
		string startTag = "<" + elementName + ">";
		int start = content.IndexOf(startTag, StringComparison.Ordinal);
		if (start < 0)
		{
			return null;
		}

		start += startTag.Length;
		string endTag = "</" + elementName + ">";
		int end = content.IndexOf(endTag, start, StringComparison.Ordinal);
		if (end < start)
		{
			return null;
		}

		string value = content[start..end].Trim();
		return value.Length == 0 || value.Contains('<', StringComparison.Ordinal) ? null : value;
	}

	private static string ComputeEffectiveAnalysisLevel(string analysisLevel, string? targetFramework, SdkAnalysisLevelDefaults defaults)
	{
		if (string.IsNullOrWhiteSpace(analysisLevel))
		{
			analysisLevel = GetDefaultAnalysisLevel(targetFramework, defaults);
		}

		SplitAnalysisLevel(analysisLevel, out string prefix, out _);
		string level = string.IsNullOrWhiteSpace(prefix) ? analysisLevel : prefix;
		if (string.Equals(level, "none", StringComparison.OrdinalIgnoreCase))
		{
			return defaults.None;
		}

		if (string.Equals(level, "latest", StringComparison.OrdinalIgnoreCase))
		{
			return defaults.Latest;
		}

		if (string.Equals(level, "preview", StringComparison.OrdinalIgnoreCase))
		{
			return defaults.Preview;
		}

		return level;
	}

	private static string GetDefaultAnalysisLevel(string? targetFramework, SdkAnalysisLevelDefaults defaults)
	{
		if (!TryParseTfmVersion(targetFramework, out Version? targetFrameworkVersion))
		{
			return string.Empty;
		}

		if (targetFrameworkVersion.Major < 5)
		{
			return string.Empty;
		}

		return VersionEquals(targetFrameworkVersion, defaults.Latest)
			? "latest"
			: $"{targetFrameworkVersion.Major}.{targetFrameworkVersion.Minor}";
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

		prefix = analysisLevel[..separator];
		suffix = analysisLevel[(separator + 1)..];
	}

	private static string NormalizeAnalysisMode(string mode)
	{
		if (string.Equals(mode, "AllEnabledByDefault", StringComparison.OrdinalIgnoreCase))
		{
			return "All";
		}

		if (string.Equals(mode, "AllDisabledByDefault", StringComparison.OrdinalIgnoreCase))
		{
			return "None";
		}

		return string.IsNullOrWhiteSpace(mode) ? "Default" : mode;
	}

	private static string TrimTrailingDotZero(string value)
	{
		while (value.EndsWith(".0", StringComparison.Ordinal))
		{
			value = value[..^2];
		}

		return value;
	}

	private static bool VersionGreaterThanOrEquals(string version, string minimum)
	{
		return TryParsePackageVersion(version, out PackageVersion parsed)
			&& TryParsePackageVersion(minimum, out PackageVersion parsedMinimum)
			&& parsed >= parsedMinimum;
	}

	private static bool VersionEquals(Version left, string right)
	{
		return TryParsePackageVersion(right, out PackageVersion parsedRight)
			&& left.Major == parsedRight.Major
			&& left.Minor == parsedRight.Minor;
	}

	private static bool TryParseTfmVersion(string? tfm, out Version version)
	{
		version = new Version(0, 0);
		if (string.IsNullOrEmpty(tfm)) return false;

		int start = tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) ? "netcoreapp".Length
			: tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) ? "net".Length
			: -1;
		if (start < 0) return false;

		int end = start;
		while (end < tfm.Length && (char.IsDigit(tfm[end]) || tfm[end] == '.')) end++;
		string versionText = tfm[start..end];
		if (versionText.Length == 0) return false;
		if (!versionText.Contains('.', StringComparison.Ordinal))
		{
			versionText += ".0";
		}

		if (Version.TryParse(versionText, out Version? parsedVersion))
		{
			version = parsedVersion;
			return true;
		}

		return false;
	}

	private static int? ParseSdkMajor(string sdkPath)
	{
		string sdkVersion = Path.GetFileName(sdkPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		return TryParsePackageVersion(sdkVersion, out PackageVersion version) ? version.Major : null;
	}

	private readonly struct SdkAnalysisLevelDefaults
	{
		public SdkAnalysisLevelDefaults(string none, string latest, string preview)
		{
			this.None = none;
			this.Latest = latest;
			this.Preview = preview;
		}

		public string None { get; }
		public string Latest { get; }
		public string Preview { get; }

		public static SdkAnalysisLevelDefaults CreateFallback(int? sdkMajor)
		{
			int latest = sdkMajor ?? 0;
			return new SdkAnalysisLevelDefaults(
				"4.0",
				latest > 0 ? latest + ".0" : string.Empty,
				latest > 0 ? (latest + 1) + ".0" : string.Empty);
		}
	}

	private string? FindNuGetPackageDirectory(string packageId, string? packageVersion)
	{
		if (string.IsNullOrWhiteSpace(packageVersion))
		{
			return null;
		}

		foreach (string nugetFolder in this.nugetFolders)
		{
			string? packageRoot = FindChildDirectory(nugetFolder, packageId);
			if (packageRoot is null) continue;

			string? versionDir = FindChildDirectory(packageRoot, packageVersion);
			if (versionDir is not null)
			{
				return versionDir;
			}
		}

		return null;
	}

	private string? FindHighestNuGetPackageDirectory(string packageId, int? requiredMajor)
	{
		string? bestPath = null;
		PackageVersion? bestVer = null;

		foreach (string nugetFolder in this.nugetFolders)
		{
			string? packageRoot = FindChildDirectory(nugetFolder, packageId);
			if (packageRoot is null) continue;

			foreach (string versionDir in Directory.EnumerateDirectories(packageRoot))
			{
				string versionName = Path.GetFileName(versionDir);
				if (!TryParsePackageVersion(versionName, out PackageVersion ver)) continue;
				if (requiredMajor.HasValue && ver.Major != requiredMajor.Value) continue;
				if (bestVer is null || ver > bestVer.Value)
				{
					bestVer = ver;
					bestPath = versionDir;
				}
			}
		}

		return bestPath;
	}

	private static string? FindChildDirectory(string parent, string childName)
	{
		string direct = Path.Join(parent, childName);
		if (Directory.Exists(direct))
		{
			return direct;
		}

		string lower = Path.Join(parent, childName.ToLowerInvariant());
		if (Directory.Exists(lower))
		{
			return lower;
		}

		return null;
	}

	private static bool IsSafePathSegment(string value)
	{
		return !string.IsNullOrWhiteSpace(value)
			&& value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
			&& !value.Contains("..", StringComparison.Ordinal)
			&& !value.Contains('/', StringComparison.Ordinal)
			&& !value.Contains('\\', StringComparison.Ordinal);
	}

	private static bool IsSafeNetFrameworkReferenceAssemblyEntry(string entry)
	{
		if (string.IsNullOrWhiteSpace(entry)
			|| !entry.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
			|| entry.Contains('\\', StringComparison.Ordinal)
			|| entry.Contains("../", StringComparison.Ordinal)
			|| entry.StartsWith("/", StringComparison.Ordinal))
		{
			return false;
		}

		int slash = entry.IndexOf('/');
		return slash > 0 && IsSafeNetFrameworkVersion(entry[..slash]);
	}

	private static bool IsSafeNetFrameworkVersion(string version)
		=> version.StartsWith("v", StringComparison.OrdinalIgnoreCase)
			&& Version.TryParse(version[1..], out _)
			&& !version.Contains('/', StringComparison.Ordinal)
			&& !version.Contains('\\', StringComparison.Ordinal);

	private static string? GetNetFrameworkReferenceAssembliesPackageId(string version)
	{
		if (!IsSafeNetFrameworkVersion(version))
		{
			return null;
		}

		string[] parts = version[1..].Split('.');
		return parts.Length switch
		{
			2 => "Microsoft.NETFramework.ReferenceAssemblies.net" + parts[0] + parts[1],
			3 => "Microsoft.NETFramework.ReferenceAssemblies.net" + parts[0] + parts[1] + parts[2],
			_ => null,
		};
	}

	private static int? ParseTfmMajor(string? tfm)
	{
		if (string.IsNullOrEmpty(tfm)) return null;
		// netN.M  → N (e.g. net10.0 → 10)
		// netcoreappN.M → N
		int start = tfm.StartsWith("netcoreapp", StringComparison.OrdinalIgnoreCase) ? "netcoreapp".Length
				   : tfm.StartsWith("net", StringComparison.OrdinalIgnoreCase) ? "net".Length
				   : -1;
		if (start < 0) return null;
		int end = start;
		while (end < tfm.Length && char.IsDigit(tfm[end])) end++;
		if (end == start) return null;
		return int.TryParse(tfm.AsSpan(start, end - start), out int n) ? n : null;
	}

	private static bool TryParsePackageVersion(string? versionName, out PackageVersion version)
	{
		version = default;
		if (string.IsNullOrWhiteSpace(versionName))
		{
			return false;
		}

		string numericVersion = versionName;
		int prereleaseIndex = numericVersion.IndexOf('-', StringComparison.Ordinal);
		if (prereleaseIndex >= 0)
		{
			numericVersion = numericVersion[..prereleaseIndex];
		}

		if (!Version.TryParse(numericVersion, out Version? parsed))
		{
			return false;
		}

		version = new PackageVersion(parsed, IsPrerelease: prereleaseIndex >= 0, Original: versionName);
		return true;
	}

	private readonly record struct PackageVersion(Version Version, bool IsPrerelease, string Original) : IComparable<PackageVersion>
	{
		public int Major => this.Version.Major;

		public int Minor => this.Version.Minor;

		public int CompareTo(PackageVersion other)
		{
			int versionComparison = this.Version.CompareTo(other.Version);
			if (versionComparison != 0)
			{
				return versionComparison;
			}

			if (this.IsPrerelease != other.IsPrerelease)
			{
				return this.IsPrerelease ? -1 : 1;
			}

			return string.Compare(this.Original, other.Original, StringComparison.OrdinalIgnoreCase);
		}

		public static bool operator >(PackageVersion left, PackageVersion right) => left.CompareTo(right) > 0;

		public static bool operator <(PackageVersion left, PackageVersion right) => left.CompareTo(right) < 0;

		public static bool operator >=(PackageVersion left, PackageVersion right) => left.CompareTo(right) >= 0;

		public static bool operator <=(PackageVersion left, PackageVersion right) => left.CompareTo(right) <= 0;
	}

	private static string[] ResolveNuGetFoldersFromEnvironment()
	{
		string? nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!string.IsNullOrWhiteSpace(nugetPackages))
		{
			return [NormalizeFolderPath(nugetPackages)];
		}

		string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		if (!string.IsNullOrEmpty(userProfile))
		{
			string defaultFolder = Path.Join(userProfile, ".nuget", "packages");
			return [NormalizeFolderPath(defaultFolder)];
		}

		return [];
	}

	private static string[] ResolveDotNetRootsFromEnvironment()
	{
		return [.. EnumerateCandidates()
			.Where(Directory.Exists)
			.Select(NormalizeFolderPath)
			.Distinct(StringComparers.Paths)];

		static IEnumerable<string> EnumerateCandidates()
		{
			string? dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
			if (!string.IsNullOrWhiteSpace(dotnetRoot))
			{
				yield return dotnetRoot;
			}

			if (OperatingSystem.IsWindows())
			{
				string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
				if (!string.IsNullOrEmpty(programFiles))
				{
					yield return Path.Join(programFiles, "dotnet");
				}
			}
			else
			{
				yield return "/usr/share/dotnet";
				yield return "/usr/local/share/dotnet";
			}

			string? userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (string.IsNullOrEmpty(userHome))
			{
				userHome = Environment.GetEnvironmentVariable("HOME");
			}

			if (!string.IsNullOrEmpty(userHome))
			{
				yield return Path.Join(userHome, ".dotnet");
			}
		}
	}

	private static string? ResolveNetFxRefRootFromEnvironment()
	{
		if (OperatingSystem.IsWindows())
		{
			string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (!string.IsNullOrEmpty(programFilesX86))
			{
				string candidate = Path.Join(programFilesX86, "Reference Assemblies", "Microsoft", "Framework", ".NETFramework");
				if (Directory.Exists(candidate))
				{
					return NormalizeFolderPath(candidate);
				}
			}
		}

		return null;
	}

	private static string NormalizeFolderPath(string path)
	{
		string full = Path.GetFullPath(path);

		if (!full.EndsWith(Path.DirectorySeparatorChar))
		{
			full += Path.DirectorySeparatorChar;
		}

		return full;
	}

	private static string JoinWithNativeSeparators(string root, string portablePath)
	{
		return string.Create(root.Length + portablePath.Length, (root, portablePath), static (span, state) =>
		{
			state.root.CopyTo(span);
			Span<char> suffix = span[state.root.Length..];
			state.portablePath.CopyTo(suffix);
			suffix.Replace('/', Path.DirectorySeparatorChar);
		});
	}
}
