// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Text.Json;

namespace Microsoft.NET.ProjectData;

public static partial class ProjectDataDonorIndex
{
	private static ProjectDataDonorSelection GetSelection(
		string indexPath,
		string workspaceRoot,
		ProjectDataDonorOptions options,
		GitQueryContext gitQueryContext)
	{
		FileInfo indexInfo = new(indexPath);
		if (!indexInfo.Exists)
		{
			return ProjectDataDonorSelection.Empty;
		}

		string cacheKey = string.Join(
			"|",
			Path.GetFullPath(indexPath),
			Path.GetFullPath(workspaceRoot),
			options.GitDistanceTopK.ToString());
		// Re-enrich candidates when a successful cache write updates the index. Checking every
		// candidate's Git state for every recipient project makes solution load scale as O(projects * candidates).
		string fingerprint = string.Join(
			"|",
			indexInfo.Exists ? indexInfo.LastWriteTimeUtc.Ticks.ToString() : "missing",
			indexInfo.Exists ? indexInfo.Length.ToString() : "0",
			GetRecipientMetadataFingerprint(workspaceRoot, gitQueryContext));

		while (true)
		{
			if (SelectionCache.TryGetValue(cacheKey, out SelectionCacheEntry? cached) &&
				string.Equals(cached.Fingerprint, fingerprint, StringComparison.Ordinal))
			{
				ProjectDataDonorSelectionResult cachedResult = cached.Selection.Value;
				if (!cachedResult.WasCancelled)
				{
					return cachedResult.Selection;
				}

				RemoveCacheEntry(SelectionCache, cacheKey, cached);
				if (gitQueryContext.GetRemainingMilliseconds() == 0)
				{
					return cachedResult.Selection;
				}

				continue;
			}

			ProjectDataDonorIndexFile index;
			try
			{
				index = ReadIndex(indexPath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
			{
				if (gitQueryContext.GetRemainingMilliseconds() == 0 && gitQueryContext.WasCancelled)
				{
					return ProjectDataDonorSelection.Empty;
				}

				options.TraceWarning("[donor] Failed to read donor index {0}: {1}", indexPath, ex.Message);
				return ProjectDataDonorSelection.Empty;
			}

			if (index.Version != CurrentVersion)
			{
				return ProjectDataDonorSelection.Empty;
			}

			List<ProjectDataDonorIndexEntry> allCandidates = index.Entries
				.Where(entry =>
					!string.IsNullOrEmpty(entry.Path) &&
					Directory.Exists(entry.Path) &&
					!PathsEqual(entry.Path, workspaceRoot))
				.DistinctByPath()
				.OrderByDescending(TimestampScore)
				.ToList();
			if (allCandidates.Count == 0)
			{
				return ProjectDataDonorSelection.Empty;
			}

			List<ProjectDataDonorIndexEntry> candidates = allCandidates
				.Take(GitMetadataCandidateLimit)
				.ToList();
			List<ProjectDataDonorIndexEntry> fallbackCandidates = allCandidates
				.Skip(GitMetadataCandidateLimit)
				.ToList();
			SelectionCacheEntry created = new(
				fingerprint,
				new Lazy<ProjectDataDonorSelectionResult>(
					() =>
					{
						ProjectDataDonorSelection selection = CreateSelection(
							candidates,
							fallbackCandidates,
							workspaceRoot,
							options,
							gitQueryContext);
						return new ProjectDataDonorSelectionResult(selection, gitQueryContext.WasCancelled);
					},
					isThreadSafe: true));
			bool stored = cached is null
				? SelectionCache.TryAdd(cacheKey, created)
				: SelectionCache.TryUpdate(cacheKey, created, cached);
			if (stored)
			{
				ProjectDataDonorSelectionResult result = created.Selection.Value;
				if (result.WasCancelled)
				{
					RemoveCacheEntry(SelectionCache, cacheKey, created);
				}
				else
				{
					TrimCache(SelectionCache, cacheKey);
				}

				return result.Selection;
			}
		}
	}

	private static ProjectDataDonorIndexEntry GetWorkspaceMetadata(
		string workspaceRoot,
		GitQueryContext gitQueryContext)
	{
		string cacheKey = Path.GetFullPath(workspaceRoot);
		while (true)
		{
			WorkspaceMetadataCache.TryGetValue(cacheKey, out WorkspaceMetadataCacheEntry? cached);
			string workspaceMetadataFingerprint = GetRecipientMetadataFingerprint(workspaceRoot, gitQueryContext);

			if (cached is not null &&
				string.Equals(cached.Fingerprint, workspaceMetadataFingerprint, StringComparison.Ordinal))
			{
				ProjectDataDonorMetadataResult cachedResult = cached.Metadata.Value;
				if (!cachedResult.WasInterrupted)
				{
					return cachedResult.Metadata;
				}

				RemoveCacheEntry(WorkspaceMetadataCache, cacheKey, cached);
				if (gitQueryContext.GetRemainingMilliseconds() == 0)
				{
					return cachedResult.Metadata;
				}

				continue;
			}

			WorkspaceMetadataCacheEntry created = new(
				workspaceMetadataFingerprint,
				new Lazy<ProjectDataDonorMetadataResult>(
					() =>
					{
						ProjectDataDonorIndexEntry metadata = new()
						{
							Path = cacheKey,
							Head = RunGit(gitQueryContext, workspaceRoot, "rev-parse", "HEAD"),
						};
						return new ProjectDataDonorMetadataResult(metadata, gitQueryContext.WasInterrupted);
					},
					isThreadSafe: true));
			bool stored = cached is null
				? WorkspaceMetadataCache.TryAdd(cacheKey, created)
				: WorkspaceMetadataCache.TryUpdate(cacheKey, created, cached);
			if (!stored)
			{
				continue;
			}

			ProjectDataDonorMetadataResult createdResult = created.Metadata.Value;
			if (createdResult.WasInterrupted)
			{
				RemoveCacheEntry(WorkspaceMetadataCache, cacheKey, created);
				return createdResult.Metadata;
			}

			string finalFingerprint = GetRecipientMetadataFingerprint(workspaceRoot, gitQueryContext);
			if (!string.Equals(workspaceMetadataFingerprint, finalFingerprint, StringComparison.Ordinal))
			{
				RemoveCacheEntry(WorkspaceMetadataCache, cacheKey, created);
				if (gitQueryContext.GetRemainingMilliseconds() == 0)
				{
					return createdResult.Metadata;
				}

				continue;
			}

			TrimCache(WorkspaceMetadataCache, cacheKey);
			return createdResult.Metadata;
		}
	}

	private static void TrimCache<T>(ConcurrentDictionary<string, T> cache, string retainedKey)
	{
		int entriesToRemove = cache.Count - MaximumCachedContexts;
		if (entriesToRemove <= 0)
		{
			return;
		}

		foreach (string key in cache.Keys)
		{
			if (!PathComparer.Equals(key, retainedKey) &&
				cache.TryRemove(key, out _) &&
				--entriesToRemove == 0)
			{
				return;
			}
		}
	}

	private static void RemoveCacheEntry<T>(ConcurrentDictionary<string, T> cache, string key, T entry)
		where T : class
		=> ((ICollection<KeyValuePair<string, T>>)cache).Remove(new KeyValuePair<string, T>(key, entry));

	private static ProjectDataDonorSelection CreateSelection(
		List<ProjectDataDonorIndexEntry> candidates,
		List<ProjectDataDonorIndexEntry> fallbackCandidates,
		string workspaceRoot,
		ProjectDataDonorOptions options,
		GitQueryContext gitQueryContext)
	{
		ProjectDataDonorIndexEntry recipient = GetWorkspaceMetadata(
			workspaceRoot,
			gitQueryContext);
		List<ProjectDataDonorIndexEntry> rankedCandidates = new(candidates.Count);
		foreach (ProjectDataDonorIndexEntry storedCandidate in candidates)
		{
			ProjectDataDonorIndexEntry liveMetadata = GetWorkspaceMetadata(
				storedCandidate.Path,
				gitQueryContext);
			ProjectDataDonorIndexEntry candidate = CloneEntry(storedCandidate);
			candidate.Head = liveMetadata.Head;
			rankedCandidates.Add(candidate);
		}

		rankedCandidates.Sort((left, right) => CompareIndexEntries(left, right, recipient));

		if (rankedCandidates.Count > 0 &&
			options.GitDistanceTopK > 0 &&
			!IsExactHead(rankedCandidates[0], recipient))
		{
			rankedCandidates = RefineTopKWithGitDistance(
				rankedCandidates,
				recipient,
				workspaceRoot,
				options.GitDistanceTopK,
				gitQueryContext);
		}

		if (gitQueryContext.TimedOut)
		{
			options.TraceWarning(
				"[donor] Git ranking exceeded its {0} ms budget for recipient workspace {1}; caching freshness ordering for {2} candidate worktrees until the recipient HEAD or donor index changes.",
				gitQueryContext.TimeoutMilliseconds,
				workspaceRoot,
				candidates.Count + fallbackCandidates.Count);
			return CreateFreshnessSelection(candidates, fallbackCandidates);
		}

		foreach (ProjectDataDonorIndexEntry fallbackCandidate in fallbackCandidates)
		{
			ProjectDataDonorIndexEntry candidate = CloneEntry(fallbackCandidate);
			candidate.Head = null;
			rankedCandidates.Add(candidate);
		}

		return new ProjectDataDonorSelection(rankedCandidates);
	}

	private static ProjectDataDonorIndexEntry CloneEntry(ProjectDataDonorIndexEntry entry)
		=> new()
		{
			Path = entry.Path,
			Head = entry.Head,
			NewestMtimeMs = entry.NewestMtimeMs,
			UpdatedUtc = entry.UpdatedUtc,
		};

	private static ProjectDataDonorSelection CreateFreshnessSelection(
		List<ProjectDataDonorIndexEntry> candidates,
		List<ProjectDataDonorIndexEntry> fallbackCandidates)
	{
		List<ProjectDataDonorIndexEntry> freshnessCandidates = new(candidates.Count + fallbackCandidates.Count);
		foreach (ProjectDataDonorIndexEntry storedCandidate in candidates)
		{
			ProjectDataDonorIndexEntry candidate = CloneEntry(storedCandidate);
			candidate.Head = null;
			freshnessCandidates.Add(candidate);
		}

		foreach (ProjectDataDonorIndexEntry storedCandidate in fallbackCandidates)
		{
			ProjectDataDonorIndexEntry candidate = CloneEntry(storedCandidate);
			candidate.Head = null;
			freshnessCandidates.Add(candidate);
		}

		return new ProjectDataDonorSelection(freshnessCandidates);
	}

	private static List<ProjectDataDonorIndexEntry> RefineTopKWithGitDistance(
		List<ProjectDataDonorIndexEntry> rankedEntries,
		ProjectDataDonorIndexEntry recipient,
		string workspaceRoot,
		int topK,
		GitQueryContext gitQueryContext)
	{
		int candidateCount = Math.Min(topK, rankedEntries.Count);
		List<ProjectDataDonorDistance> distances = new(candidateCount);
		for (int i = 0; i < candidateCount; i++)
		{
			ProjectDataDonorIndexEntry entry = rankedEntries[i];
			distances.Add(new ProjectDataDonorDistance(entry, GitDistance(workspaceRoot, recipient.Head, entry.Head, gitQueryContext)));
		}

		distances.Sort((left, right) =>
		{
			int distanceComparison = left.Distance.CompareTo(right.Distance);
			return distanceComparison != 0
				? distanceComparison
				: CompareIndexEntries(left.Entry, right.Entry, recipient);
		});

		List<ProjectDataDonorIndexEntry> refined = [.. distances.Select(static distance => distance.Entry)];
		HashSet<string> seen = new(refined.Select(static entry => NormalizePath(entry.Path)), PathComparer);
		foreach (ProjectDataDonorIndexEntry entry in rankedEntries)
		{
			if (seen.Add(NormalizePath(entry.Path)))
			{
				refined.Add(entry);
			}
		}

		return refined;
	}

	private static int CompareIndexEntries(
		ProjectDataDonorIndexEntry left,
		ProjectDataDonorIndexEntry right,
		ProjectDataDonorIndexEntry recipient)
	{
		int exactHeadComparison = IsExactHead(right, recipient).CompareTo(IsExactHead(left, recipient));
		return exactHeadComparison != 0
			? exactHeadComparison
			: TimestampScore(right).CompareTo(TimestampScore(left));
	}

	private static bool IsExactHead(
		ProjectDataDonorIndexEntry entry,
		ProjectDataDonorIndexEntry recipient)
		=> !string.IsNullOrEmpty(entry.Head) &&
			!string.IsNullOrEmpty(recipient.Head) &&
			string.Equals(entry.Head, recipient.Head, StringComparison.Ordinal);

	private static long TimestampScore(ProjectDataDonorIndexEntry entry)
	{
		if (entry.NewestMtimeMs.HasValue)
		{
			return entry.NewestMtimeMs.Value;
		}

		return entry.UpdatedUtc.HasValue
			? entry.UpdatedUtc.Value.ToUnixTimeMilliseconds()
			: 0;
	}

	private static IEnumerable<ProjectDataDonorIndexEntry> DistinctByPath(this IEnumerable<ProjectDataDonorIndexEntry> entries)
	{
		HashSet<string> seen = new(PathComparer);
		foreach (ProjectDataDonorIndexEntry entry in entries)
		{
			if (seen.Add(NormalizePath(entry.Path)))
			{
				yield return entry;
			}
		}
	}

	private sealed class ProjectDataDonorSelection
	{
		public static ProjectDataDonorSelection Empty { get; } = new([]);

		public ProjectDataDonorSelection(IReadOnlyList<ProjectDataDonorIndexEntry> rankedEntries)
		{
			this.RankedEntries = rankedEntries;
		}

		public IReadOnlyList<ProjectDataDonorIndexEntry> RankedEntries { get; }
	}

	private sealed class SelectionCacheEntry
	{
		public SelectionCacheEntry(
			string fingerprint,
			Lazy<ProjectDataDonorSelectionResult> selection)
		{
			this.Fingerprint = fingerprint;
			this.Selection = selection;
		}

		public string Fingerprint { get; }
		public Lazy<ProjectDataDonorSelectionResult> Selection { get; }
	}

	private sealed class WorkspaceMetadataCacheEntry
	{
		public WorkspaceMetadataCacheEntry(
			string fingerprint,
			Lazy<ProjectDataDonorMetadataResult> metadata)
		{
			this.Fingerprint = fingerprint;
			this.Metadata = metadata;
		}

		public string Fingerprint { get; }
		public Lazy<ProjectDataDonorMetadataResult> Metadata { get; }
	}

	private readonly struct ProjectDataDonorSelectionResult(
		ProjectDataDonorSelection selection,
		bool wasCancelled)
	{
		public ProjectDataDonorSelection Selection { get; } = selection;
		public bool WasCancelled { get; } = wasCancelled;
	}

	private readonly struct ProjectDataDonorMetadataResult(
		ProjectDataDonorIndexEntry metadata,
		bool wasInterrupted)
	{
		public ProjectDataDonorIndexEntry Metadata { get; } = metadata;
		public bool WasInterrupted { get; } = wasInterrupted;
	}

	private readonly struct ProjectDataDonorDistance
	{
		public ProjectDataDonorDistance(ProjectDataDonorIndexEntry entry, int distance)
		{
			this.Entry = entry;
			this.Distance = distance;
		}

		public ProjectDataDonorIndexEntry Entry { get; }
		public int Distance { get; }
	}
}
