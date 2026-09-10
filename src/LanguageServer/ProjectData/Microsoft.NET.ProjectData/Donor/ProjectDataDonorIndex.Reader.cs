// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Microsoft.NET.ProjectData;

public static partial class ProjectDataDonorIndex
{
	private const string CacheFileExtension = ".lscache";
	private const int MaximumCachedContexts = 64;
	private const int GitMetadataCandidateLimit = 5;
	private static readonly ConcurrentDictionary<string, SelectionCacheEntry> SelectionCache = new(PathComparer);
	private static readonly ConcurrentDictionary<string, WorkspaceMetadataCacheEntry> WorkspaceMetadataCache = new(PathComparer);

	internal static int SelectionCacheCount => SelectionCache.Count;
	internal static int WorkspaceMetadataCacheCount => WorkspaceMetadataCache.Count;

	public static IEnumerable<ProjectDataDonorCandidate> EnumerateDonorCandidates(
		string recipientProjectFilePath,
		ProjectDataDonorOptions? options,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		GitQueryContext gitQueryContext = new(cancellationToken);
		foreach (ProjectDataDonorCandidate candidate in EnumerateDonorCandidatesCore(recipientProjectFilePath, options, gitQueryContext))
		{
			cancellationToken.ThrowIfCancellationRequested();
			yield return candidate;
		}

		cancellationToken.ThrowIfCancellationRequested();
	}

	internal static IEnumerable<ProjectDataDonorCandidate> EnumerateDonorCandidatesCore(
		string recipientProjectFilePath,
		ProjectDataDonorOptions? options,
		GitQueryContext gitQueryContext)
	{
		if (string.IsNullOrEmpty(recipientProjectFilePath))
		{
			yield break;
		}

		options ??= ProjectDataDonorOptions.Default;
		if (!options.Enabled)
		{
			yield break;
		}

		if (!TryResolveIndexContext(
			recipientProjectFilePath,
			options.WorkspaceRoot,
			options.IndexPath,
			out string? workspaceRoot,
			out string? indexPath))
		{
			yield break;
		}

		if (!TryGetRelativePath(workspaceRoot, recipientProjectFilePath, out string? relativeProjectPath))
		{
			yield break;
		}

		bool probeUserFolderCache = true;
		ProjectDataDonorSelection selection = GetSelection(indexPath, workspaceRoot, options, gitQueryContext);
		foreach (ProjectDataDonorIndexEntry entry in selection.RankedEntries)
		{
			if (PathsEqual(entry.Path, workspaceRoot))
			{
				continue;
			}

			string donorProjectFilePath = Path.GetFullPath(Path.Combine(entry.Path, relativeProjectPath));
			string donorProjectFolderCachePath = donorProjectFilePath + CacheFileExtension;
			if (File.Exists(donorProjectFolderCachePath))
			{
				yield return new ProjectDataDonorCandidate(donorProjectFolderCachePath, entry.Path);
			}

			if (probeUserFolderCache)
			{
				if (!UserFolderCachePath.TryCompute(donorProjectFilePath, out string donorUserFolderCachePath))
				{
					options.TraceWarning(
						"[donor] User-folder cache root is unavailable; continuing with project-folder donors for recipient {0}.",
						recipientProjectFilePath);
					probeUserFolderCache = false;
				}
				else if (File.Exists(donorUserFolderCachePath))
				{
					yield return new ProjectDataDonorCandidate(donorUserFolderCachePath, entry.Path);
				}
			}
		}
	}

	static partial void OnIndexUpdated()
		=> SelectionCache.Clear();
}
