// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Text.Json;

namespace Microsoft.NET.ProjectData;

/// <summary>
/// Reads and writes the repo-scoped donor index used to bootstrap ProjectData caches across sibling worktrees.
/// </summary>
public static partial class ProjectDataDonorIndex
{
	private const int CurrentVersion = 2;
	private const string IndexDirectoryName = "dotnet-projectdata";
	private const string IndexFileName = "lscache-donor-index.json";
	private static readonly TimeSpan IndexLockTimeout = TimeSpan.FromSeconds(10);
	private static readonly object WriteGate = new();
	// Keep these semantics inside the source-linked donor implementation until Tasks targets net11.0
	// and can reference Microsoft.NET.ProjectData instead of compiling its own copy.
	internal static StringComparer PathComparer { get; } =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
	private static StringComparison PathComparison { get; } =
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

	public static bool TryRecordWrite(
		string projectFilePath,
		string cacheFilePath,
		ProjectDataDonorWriteOptions? options,
		out string? message)
	{
		message = null;
		if (string.IsNullOrEmpty(projectFilePath) || string.IsNullOrEmpty(cacheFilePath) || !File.Exists(cacheFilePath))
		{
			return false;
		}

		options ??= new ProjectDataDonorWriteOptions();
		if (!options.Enabled)
		{
			return false;
		}

		try
		{
			if (!TryResolveIndexContext(
				projectFilePath,
				options.WorkspaceRoot,
				options.IndexPath,
				out string workspaceRoot,
				out string indexPath))
			{
				return false;
			}

			lock (WriteGate)
			{
				using FileStream indexLock = AcquireIndexLock(indexPath);
				ProjectDataDonorIndexFile index = ReadIndexForWrite(indexPath, out string? recoveryMessage);
				ProjectDataDonorIndexEntry entry = CreateWrittenEntry(workspaceRoot, cacheFilePath);

				index.UpsertEntry(entry);
				WriteIndex(indexPath, index);
				OnIndexUpdated();
				message = recoveryMessage;
			}

			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or ArgumentException or NotSupportedException)
		{
			message = ex.Message;
			return false;
		}
	}

	private static FileStream AcquireIndexLock(string indexPath)
	{
		string lockPath = indexPath + ".lock";
		Directory.CreateDirectory(Path.GetDirectoryName(lockPath)!);
		System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
		while (true)
		{
			try
			{
				return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, bufferSize: 1);
			}
			catch (IOException) when (stopwatch.Elapsed < IndexLockTimeout)
			{
				Thread.Sleep(25);
			}
			catch (IOException ex)
			{
				throw new IOException($"Timed out waiting for exclusive access to donor index '{indexPath}'.", ex);
			}
		}
	}

	internal static string? TryResolveDefaultIndexPath(string projectFilePath)
	{
		return TryFindRepositoryInfo(projectFilePath, out _, out string? gitCommonDirectory)
			? Path.Combine(gitCommonDirectory, IndexDirectoryName, IndexFileName)
			: null;
	}

	private static bool TryResolveIndexContext(
		string projectFilePath,
		string? workspaceRootOverride,
		string? indexPathOverride,
		out string workspaceRoot,
		out string indexPath)
	{
		workspaceRoot = string.Empty;
		indexPath = string.Empty;

		string? candidateWorkspaceRoot = workspaceRootOverride is { Length: > 0 }
			? NormalizePath(workspaceRootOverride)
			: null;
		string? candidateIndexPath = indexPathOverride is { Length: > 0 }
			? Path.GetFullPath(indexPathOverride)
			: null;

		if (candidateWorkspaceRoot is null || candidateIndexPath is null)
		{
			if (!TryFindRepositoryInfo(projectFilePath, out string? discoveredWorkspaceRoot, out string? gitCommonDirectory))
			{
				return false;
			}

			candidateWorkspaceRoot ??= discoveredWorkspaceRoot;
			candidateIndexPath ??= Path.Combine(gitCommonDirectory, IndexDirectoryName, IndexFileName);
		}

		workspaceRoot = NormalizePath(candidateWorkspaceRoot);
		indexPath = candidateIndexPath;
		return true;
	}

	private static bool TryGetRelativePath(string root, string fullPath, out string relativePath)
	{
		relativePath = string.Empty;
		string normalizedRoot = EnsureTrailingDirectorySeparator(Path.GetFullPath(root));
		string normalizedFullPath = Path.GetFullPath(fullPath);
		if (!normalizedFullPath.StartsWith(normalizedRoot, PathComparison))
		{
			return false;
		}

		relativePath = normalizedFullPath.Substring(normalizedRoot.Length);
		return relativePath.Length > 0 && !relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
	}

	private static string EnsureTrailingDirectorySeparator(string path)
	{
		if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
			path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal))
		{
			return path;
		}

		return path + Path.DirectorySeparatorChar;
	}

	private static bool PathsEqual(string left, string right)
		=> string.Equals(NormalizePath(left), NormalizePath(right), PathComparison);

	private static string NormalizePath(string path)
	{
		string fullPath = Path.GetFullPath(path);
		string root = Path.GetPathRoot(fullPath) ?? string.Empty;
		return fullPath.Length > root.Length
			? fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
			: fullPath;
	}

	private static ProjectDataDonorIndexEntry CreateWrittenEntry(
		string workspaceRoot,
		string cacheFilePath)
	{
		return new ProjectDataDonorIndexEntry
		{
			Path = NormalizePath(workspaceRoot),
			NewestMtimeMs = ToUnixTimeMilliseconds(File.GetLastWriteTimeUtc(cacheFilePath)),
			UpdatedUtc = DateTimeOffset.UtcNow,
		};
	}

	private static bool TryFindRepositoryInfo(string projectFilePath, out string workspaceRoot, out string gitCommonDirectory)
	{
		workspaceRoot = string.Empty;
		gitCommonDirectory = string.Empty;

		string? current = Path.GetDirectoryName(Path.GetFullPath(projectFilePath));
		while (!string.IsNullOrEmpty(current))
		{
			string currentDirectory = current!;
			string gitPath = Path.Combine(currentDirectory, ".git");
			if (Directory.Exists(gitPath))
			{
				workspaceRoot = currentDirectory;
				gitCommonDirectory = gitPath;
				return true;
			}

			if (File.Exists(gitPath) && TryReadGitFile(gitPath, out string gitDirectory))
			{
				workspaceRoot = currentDirectory;
				gitCommonDirectory = ResolveCommonGitDirectory(gitDirectory);
				return true;
			}

			current = Directory.GetParent(current)?.FullName;
		}

		return false;
	}

	private static bool TryReadGitFile(string gitFilePath, out string gitDirectory)
	{
		gitDirectory = string.Empty;
		try
		{
			string content = File.ReadAllText(gitFilePath).Trim();
			const string Prefix = "gitdir:";
			if (!content.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			string value = content.Substring(Prefix.Length).Trim();
			gitDirectory = Path.IsPathRooted(value)
				? Path.GetFullPath(value)
				: Path.GetFullPath(Path.Combine(Path.GetDirectoryName(gitFilePath)!, value));
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return false;
		}
	}

	private static string ResolveCommonGitDirectory(string gitDirectory)
	{
		string commonDirFilePath = Path.Combine(gitDirectory, "commondir");
		try
		{
			if (File.Exists(commonDirFilePath))
			{
				string commonDir = File.ReadAllText(commonDirFilePath).Trim();
				return Path.IsPathRooted(commonDir)
					? Path.GetFullPath(commonDir)
					: Path.GetFullPath(Path.Combine(gitDirectory, commonDir));
			}
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
		}

		return gitDirectory;
	}

	private static long ToUnixTimeMilliseconds(DateTime dateTime)
		=> new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)).ToUnixTimeMilliseconds();

	static partial void OnIndexUpdated();
}
