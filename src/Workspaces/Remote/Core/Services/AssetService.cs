// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// This service provide a way to get roslyn objects from checksum
    /// 
    /// TODO: change this service to workspace service
    /// </summary>
    internal class AssetService
    {
        private const int CleanupInterval = 3; // 3 minutes
        private const int PurgeAfter = 30; // 30 minutes

        private static readonly TimeSpan s_purgeAfterTimeSpan = TimeSpan.FromMinutes(PurgeAfter);
        private static readonly TimeSpan s_cleanupIntervalTimeSpan = TimeSpan.FromMinutes(CleanupInterval);

        // PREVIEW: unfortunately, I need dummy workspace since workspace services can be workspace specific
        private static readonly Serializer s_serializer = new Serializer(new AdhocWorkspace(RoslynServices.HostServices, workspaceKind: "dummy").Services);

        private readonly ConcurrentDictionary<int, AssetSource> _assetSources =
            new ConcurrentDictionary<int, AssetSource>(concurrencyLevel: 4, capacity: 10);

        private readonly ConcurrentDictionary<Checksum, Entry> _assets =
            new ConcurrentDictionary<Checksum, Entry>(concurrencyLevel: 4, capacity: 10);

        public AssetService()
        {
            Task.Run(CleanAssetsAsync, CancellationToken.None);
        }

        private async Task CleanAssetsAsync()
        {
            while (true)
            {
                CleanAssets();

                await Task.Delay(s_cleanupIntervalTimeSpan).ConfigureAwait(false);
            }
        }

        private void CleanAssets()
        {
            var current = DateTime.UtcNow;

            using (Logger.LogBlock(FunctionId.AssetService_CleanAssets, CancellationToken.None))
            {
                foreach (var kvp in _assets.ToArray())
                {
                    if (current - kvp.Value.LastAccessed <= s_purgeAfterTimeSpan)
                    {
                        continue;
                    }

                    // If it fails, we'll just leave it in the asset pool.
                    Entry entry;
                    _assets.TryRemove(kvp.Key, out entry);
                }
            }
        }

        public T Deserialize<T>(string kind, ObjectReader reader, CancellationToken cancellationToken)
        {
            return s_serializer.Deserialize<T>(kind, reader, cancellationToken);
        }

        public void RegisterAssetSource(int serviceId, AssetSource assetSource)
        {
            Contract.ThrowIfFalse(_assetSources.TryAdd(serviceId, assetSource));
        }

        public void UnregisterAssetSource(int serviceId)
        {
            AssetSource dummy;
            _assetSources.TryRemove(serviceId, out dummy);
        }

        public async Task<T> GetAssetAsync<T>(Checksum checksum, CancellationToken cancellationToken)
        {
            T asset;
            if (TryGetAsset(checksum, out asset))
            {
                return asset;
            }

            using (Logger.LogBlock(FunctionId.AssetService_GetAssetAsync, GetChecksumLogInfo, checksum, cancellationToken))
            {
                // TODO: what happen if service doesn't come back. timeout?
                var value = await RequestAssetAsync(checksum, cancellationToken).ConfigureAwait(false);

                _assets.TryAdd(checksum, new Entry(value));

                return (T)value;
            }
        }

        public async Task SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeSolutionAssetsAsync, GetChecksumLogInfo, solutionChecksum, cancellationToken))
            {
                var collector = new ChecksumSynchronizer(this);
                await collector.SynchronizeAsync(solutionChecksum, cancellationToken).ConfigureAwait(false);
            }
        }

        private bool Exists(Checksum checksum)
        {
            object unused;
            return TryGetAsset(checksum, out unused);
        }

        private bool TryGetAsset<T>(Checksum checksum, out T value)
        {
            value = default(T);
            using (Logger.LogBlock(FunctionId.AssetService_TryGetAsset, GetChecksumLogInfo, checksum, CancellationToken.None))
            {
                Entry entry;
                if (!_assets.TryGetValue(checksum, out entry))
                {
                    return false;
                }

                // Update timestamp
                Update(checksum, entry);

                value = (T)entry.Object;
                return true;
            }
        }

        private async Task SynchronizeAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.AssetService_SynchronizeAssetsAsync, GetChecksumsLogInfo, checksums, cancellationToken))
            {
                var values = await RequestAssetsAsync(checksums, cancellationToken).ConfigureAwait(false);

                foreach (var tuple in values)
                {
                    _assets.TryAdd(tuple.Item1, new Entry(tuple.Item2));
                }
            }
        }

        private void Update(Checksum checksum, Entry entry)
        {
            // entry is reference type. we update it directly. 
            // we don't care about race.
            entry.LastAccessed = DateTime.UtcNow;
        }

        private async Task<object> RequestAssetAsync(Checksum checksum, CancellationToken cancellationToken)
        {
            using (var pooledObject = SharedPools.Default<HashSet<Checksum>>().GetPooledObject())
            {
                pooledObject.Object.Add(checksum);

                var tuple = await RequestAssetsAsync(pooledObject.Object, cancellationToken).ConfigureAwait(false);
                return tuple[0].Item2;
            }
        }

        private async Task<IList<ValueTuple<Checksum, object>>> RequestAssetsAsync(ISet<Checksum> checksums, CancellationToken cancellationToken)
        {
            if (checksums.Count == 0)
            {
                return SpecializedCollections.EmptyList<ValueTuple<Checksum, object>>();
            }

            // the service doesn't care which asset source it uses to get the asset. if there are multiple
            // channel created (multiple caller to code analysis service), we will have multiple asset sources
            // 
            // but, there must be one that knows about the asset with the checksum that is pinned for this
            //      particular call
            foreach (var kv in _assetSources.ToArray())
            {
                var serviceId = kv.Key;
                var source = kv.Value;

                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // ask one of asset source for data
                    return await source.RequestAssetsAsync(serviceId, checksums, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsExpected(ex))
                {
                    // cancellation could be from either caller or asset side when cancelled. we only throw cancellation if
                    // caller side is cancelled. otherwise, we move to next asset source
                    cancellationToken.ThrowIfCancellationRequested();

                    // connection to the asset source has closed.
                    // move to next asset source
                    continue;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private bool IsExpected(Exception ex)
        {
            // these exception can happen if either operation is cancelled, connection to asset source is closed
            return ex is OperationCanceledException || ex is IOException || ex is ObjectDisposedException;
        }

        private static string GetChecksumLogInfo(Checksum checksum)
        {
            return checksum.ToString();
        }

        private static string GetChecksumsLogInfo(IEnumerable<Checksum> checksums)
        {
            return string.Join("|", checksums.Select(c => c.ToString()));
        }

        private class Entry
        {
            // mutable field
            public DateTime LastAccessed;

            // this can't change for same checksum
            public readonly object Object;

            public Entry(object @object)
            {
                LastAccessed = DateTime.UtcNow;
                Object = @object;
            }
        }

        private struct ChecksumSynchronizer
        {
            private readonly AssetService _assetService;

            public ChecksumSynchronizer(AssetService assetService)
            {
                _assetService = assetService;
            }

            public async Task SynchronizeAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
            {
                // get children of solution checksum object at once
                var solutionChecksums = new HashSet<Checksum>();
                var solutionChecksumObject = await _assetService.GetAssetAsync<SolutionChecksumObject>(solutionChecksum, cancellationToken).ConfigureAwait(false);

                AppendIfNeeded(solutionChecksums, solutionChecksumObject.Info);
                AppendIfNeeded(solutionChecksums, solutionChecksumObject.Projects);

                await _assetService.SynchronizeAssetsAsync(solutionChecksums, cancellationToken).ConfigureAwait(false);

                // get children of project checksum objects at once
                var projectChecksums = new HashSet<Checksum>();
                foreach (var projectChecksum in solutionChecksumObject.Projects)
                {
                    var projectChecksumObject = await _assetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);

                    AppendIfNeeded(projectChecksums, projectChecksumObject.Info);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.CompilationOptions);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.ParseOptions);

                    AppendIfNeeded(projectChecksums, projectChecksumObject.ProjectReferences);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.MetadataReferences);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.AnalyzerReferences);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.Documents);
                    AppendIfNeeded(projectChecksums, projectChecksumObject.AdditionalDocuments);
                }

                await _assetService.SynchronizeAssetsAsync(projectChecksums, cancellationToken).ConfigureAwait(false);

                // get children of document checksum objects at once
                var documentChecksums = new HashSet<Checksum>();
                foreach (var projectChecksum in solutionChecksumObject.Projects)
                {
                    var projectChecksumObject = await _assetService.GetAssetAsync<ProjectChecksumObject>(projectChecksum, cancellationToken).ConfigureAwait(false);

                    foreach (var checksum in projectChecksumObject.Documents)
                    {
                        var documentChecksumObject = await _assetService.GetAssetAsync<DocumentChecksumObject>(checksum, cancellationToken).ConfigureAwait(false);

                        AppendIfNeeded(documentChecksums, documentChecksumObject.Info);
                        AppendIfNeeded(documentChecksums, documentChecksumObject.Text);
                    }

                    foreach (var checksum in projectChecksumObject.AdditionalDocuments)
                    {
                        var documentChecksumObject = await _assetService.GetAssetAsync<DocumentChecksumObject>(checksum, cancellationToken).ConfigureAwait(false);

                        AppendIfNeeded(documentChecksums, documentChecksumObject.Info);
                        AppendIfNeeded(documentChecksums, documentChecksumObject.Text);
                    }
                }

                await _assetService.SynchronizeAssetsAsync(documentChecksums, cancellationToken).ConfigureAwait(false);
            }

            private void AppendIfNeeded(HashSet<Checksum> checksums, IEnumerable<Checksum> collection)
            {
                foreach (var checksum in collection)
                {
                    AppendIfNeeded(checksums, checksum);
                }
            }

            private void AppendIfNeeded(HashSet<Checksum> checksums, Checksum info)
            {
                if (!_assetService.Exists(info))
                {
                    checksums.Add(info);
                }
            }
        }
    }
}

