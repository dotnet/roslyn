// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Computes the user-folder location for a project's <c>.lscache</c> file.
///
/// <para>This file is the single source of truth for the layout. The MSBuild
/// task (writer/merger) and the runtime reader both call <see cref="Compute"/>
/// so the path the writer produces is exactly the path the reader looks for.
/// The Tasks project links this source file into its own assembly via a
/// <c>&lt;Compile Include=...&gt;</c> reference rather than taking a project
/// dependency on the cache assembly.</para>
/// </summary>
public static class UserFolderCachePath
{
	private const string CacheFileExtension = ".lscache";
	private const string CacheBaseDirectoryEnvVar = "DOTNET_PROJECTDATA_CACHE_DIR";
	private const string XdgCacheHomeEnvVar = "XDG_CACHE_HOME";
	private const string CacheDirectoryName = "dotnet-projectdata";

	/// <summary>
	/// Returns the absolute path of the user-folder cache file for
	/// <paramref name="projectFilePath"/>. Layout (matches the v1 reader):
	/// <c>&lt;base&gt;/&lt;sha1[0..2]&gt;/&lt;sha1[2..]&gt;</c> where the SHA-1
	/// hash is taken over the lower-cased project path on case-insensitive
	/// filesystems (Windows / macOS) and the original casing on Linux.
	/// </summary>
	public static string Compute(string projectFilePath)
	{
		if (string.IsNullOrEmpty(projectFilePath))
			throw new ArgumentException("Project file path must be non-empty.", nameof(projectFilePath));

		bool caseSensitive = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
		string hashInput = caseSensitive ? projectFilePath : projectFilePath.ToLowerInvariant();

		byte[] inputBytes = Encoding.UTF8.GetBytes(hashInput);
		byte[] hashBytes;
#if NET5_0_OR_GREATER
		hashBytes = SHA1.HashData(inputBytes);
#else
		using (var sha1 = SHA1.Create())
		{
			hashBytes = sha1.ComputeHash(inputBytes);
		}
#endif

		string hex = HexEncoder.ToLowerHex(hashBytes);
		string baseDir = GetCacheBaseDirectory();
		return Path.Combine(baseDir, hex.Substring(0, 2), hex.Substring(2));
	}

	internal static bool TryCompute(string projectFilePath, out string cacheFilePath)
	{
		try
		{
			cacheFilePath = Compute(projectFilePath);
			return true;
		}
		catch (InvalidOperationException)
		{
			cacheFilePath = string.Empty;
			return false;
		}
	}

	/// <summary>
	/// Returns the cache root directory used by <see cref="Compute"/>.
	///
	/// <para>Set <c>DOTNET_PROJECTDATA_CACHE_DIR</c> to override the cache root
	/// exactly. Otherwise the cache is stored under the platform's user-local
	/// cache directory.</para>
	/// </summary>
	public static string GetCacheBaseDirectory()
	{
		string? envOverride = Environment.GetEnvironmentVariable(CacheBaseDirectoryEnvVar);
		if (!string.IsNullOrWhiteSpace(envOverride))
		{
			return envOverride!;
		}

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			if (!string.IsNullOrWhiteSpace(localAppData))
			{
				return Path.Combine(localAppData, "Microsoft", CacheDirectoryName);
			}
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (!string.IsNullOrWhiteSpace(home))
			{
				return Path.Combine(home, "Library", "Caches", CacheDirectoryName);
			}
		}
		else
		{
			string? xdgCacheHome = Environment.GetEnvironmentVariable(XdgCacheHomeEnvVar);
			if (!string.IsNullOrWhiteSpace(xdgCacheHome))
			{
				return Path.Combine(xdgCacheHome!, CacheDirectoryName);
			}

			string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			if (!string.IsNullOrWhiteSpace(home))
			{
				return Path.Combine(home, ".cache", CacheDirectoryName);
			}
		}

		throw new InvalidOperationException($"Unable to determine the ProjectData cache root. Set {CacheBaseDirectoryEnvVar}.");
	}

	/// <summary>The cache file extension (currently <c>.lscache</c>).</summary>
	public static string FileExtension => CacheFileExtension;
}
