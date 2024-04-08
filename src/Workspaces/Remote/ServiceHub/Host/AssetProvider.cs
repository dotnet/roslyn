// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// This service provide a way to get roslyn objects from checksum
/// </summary>
internal sealed partial class AssetProvider(Checksum solutionChecksum, SolutionAssetCache assetCache, IAssetSource assetSource, ISerializerService serializerService)
    : AbstractAssetProvider
{
    private static readonly SharedStopwatch s_start = SharedStopwatch.StartNew();

    private const int PooledChecksumArraySize = 256;
    private static readonly ObjectPool<Checksum[]> s_checksumPool = new(() => new Checksum[PooledChecksumArraySize], 16);

    private readonly Checksum _solutionChecksum = solutionChecksum;
    private readonly ISerializerService _serializerService = serializerService;
    private readonly SolutionAssetCache _assetCache = assetCache;
    private readonly IAssetSource _assetSource = assetSource;

    public override async ValueTask<T> GetAssetAsync<T>(
        AssetPath assetPath, Checksum checksum, CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(checksum == Checksum.Null);
        if (_assetCache.TryGetAsset<T>(checksum, out var asset))
            return asset;

        using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);
        checksums.Add(checksum);

        using var _2 = PooledDictionary<Checksum, T>.GetInstance(out var results);
        await this.SynchronizeAssetsAsync(assetPath, checksums, results, cancellationToken).ConfigureAwait(false);

        return results[checksum];
    }

    public override async ValueTask<ImmutableArray<(Checksum checksum, T asset)>> GetAssetsAsync<T>(
        AssetPath assetPath, HashSet<Checksum> checksums, CancellationToken cancellationToken)
    {
        using var _ = PooledDictionary<Checksum, T>.GetInstance(out var results);

        await this.SynchronizeAssetsAsync(assetPath, checksums, results, cancellationToken).ConfigureAwait(false);

        var result = new (Checksum checksum, T asset)[checksums.Count];
        var index = 0;
        foreach (var (checksum, assetObject) in results)
        {
            result[index] = (checksum, assetObject);
            index++;
        }

        return ImmutableCollectionsMarshal.AsImmutableArray(result);
    }

    public async ValueTask SynchronizeSolutionAssetsAsync(Checksum solutionChecksum, CancellationToken cancellationToken)
    {
        var timer = SharedStopwatch.StartNew();

        // this will pull in assets that belong to the given solution checksum to this remote host. this one is not
        // supposed to be used for functionality but only for perf. that is why it doesn't return anything. to get
        // actual data GetAssetAsync should be used. and that will return actual data and if there is any missing data
        // in cache, GetAssetAsync itself will bring that data in from data source (VS)

        // one can call this method to make cache hot for all assets that belong to the solution checksum so that
        // GetAssetAsync call will most likely cache hit. it is most likely since we might change cache heuristic in
        // future which make data to live a lot shorter in the cache, and the data might get expired before one actually
        // consume the data. 
        using (Logger.LogBlock(FunctionId.AssetService_SynchronizeSolutionAssetsAsync, Checksum.GetChecksumLogInfo, solutionChecksum, cancellationToken))
        {
            await SynchronizeSolutionAssetsWorkerAsync().ConfigureAwait(false);
        }

        // report telemetry to help correlate slow solution sync with UI delays
        var elapsed = timer.Elapsed;
        if (elapsed.TotalMilliseconds > 1000)
            Logger.Log(FunctionId.AssetService_Perf, KeyValueLogMessage.Create(map => map["SolutionSyncTime"] = elapsed.TotalMilliseconds));

        async ValueTask SynchronizeSolutionAssetsWorkerAsync()
        {
            using var _1 = PooledDictionary<Checksum, object>.GetInstance(out var checksumToObjects);

            // first, get top level solution state for the given solution checksum
            var compilationStateChecksums = await this.GetAssetAsync<SolutionCompilationStateChecksums>(
                assetPath: AssetPath.SolutionOnly, solutionChecksum, cancellationToken).ConfigureAwait(false);

            using var _2 = PooledHashSet<Checksum>.GetInstance(out var checksums);

            // second, get direct children of the solution compilation state.
            compilationStateChecksums.AddAllTo(checksums);
            await this.SynchronizeAssetsAsync<object>(assetPath: AssetPath.SolutionOnly, checksums, results: null, cancellationToken).ConfigureAwait(false);

            // third, get direct children of the solution state.
            var stateChecksums = await this.GetAssetAsync<SolutionStateChecksums>(
                assetPath: AssetPath.SolutionOnly, compilationStateChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

            // Ask for solutions and top-level projects as the solution checksums will contain the checksums for
            // the project states and we want to get that all in one batch.
            checksums.Clear();
            stateChecksums.AddAllTo(checksums);
            await this.SynchronizeAssetsAsync(assetPath: AssetPath.SolutionAndTopLevelProjectsOnly, checksums, checksumToObjects, cancellationToken).ConfigureAwait(false);

            // fourth, get all projects and documents in the solution 
            foreach (var (projectChecksum, _) in stateChecksums.Projects)
            {
                var projectStateChecksums = (ProjectStateChecksums)checksumToObjects[projectChecksum];
                await SynchronizeProjectAssetsAsync(projectStateChecksums, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async ValueTask SynchronizeProjectAssetsAsync(ProjectStateChecksums projectChecksums, CancellationToken cancellationToken)
    {
        // this will pull in assets that belong to the given project checksum to this remote host. this one is not
        // supposed to be used for functionality but only for perf. that is why it doesn't return anything. to get
        // actual data GetAssetAsync should be used. and that will return actual data and if there is any missing data
        // in cache, GetAssetAsync itself will bring that data in from data source (VS)

        // one can call this method to make cache hot for all assets that belong to the project checksum so that
        // GetAssetAsync call will most likely cache hit. it is most likely since we might change cache heuristic in
        // future which make data to live a lot shorter in the cache, and the data might get expired before one actually
        // consume the data. 
        using (Logger.LogBlock(FunctionId.AssetService_SynchronizeProjectAssetsAsync, Checksum.GetProjectChecksumsLogInfo, projectChecksums, cancellationToken))
        {
            await SynchronizeProjectAssetsWorkerAsync().ConfigureAwait(false);
        }

        async ValueTask SynchronizeProjectAssetsWorkerAsync()
        {
            // get children of project checksum objects at once
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            checksums.Add(projectChecksums.Info);
            checksums.Add(projectChecksums.CompilationOptions);
            checksums.Add(projectChecksums.ParseOptions);
            AddAll(checksums, projectChecksums.ProjectReferences);
            AddAll(checksums, projectChecksums.MetadataReferences);
            AddAll(checksums, projectChecksums.AnalyzerReferences);
            AddAll(checksums, projectChecksums.Documents.Checksums);
            AddAll(checksums, projectChecksums.AdditionalDocuments.Checksums);
            AddAll(checksums, projectChecksums.AnalyzerConfigDocuments.Checksums);

            // First synchronize all the top-level info about this project.
            await this.SynchronizeAssetsAsync<object>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums, results: null, cancellationToken).ConfigureAwait(false);

            checksums.Clear();

            // Then synchronize the info about all the documents within.
            await CollectChecksumChildrenAsync(checksums, projectChecksums.Documents).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(checksums, projectChecksums.AdditionalDocuments).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(checksums, projectChecksums.AnalyzerConfigDocuments).ConfigureAwait(false);

            await this.SynchronizeAssetsAsync<object>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums, results: null, cancellationToken).ConfigureAwait(false);
        }

        async ValueTask CollectChecksumChildrenAsync(HashSet<Checksum> checksums, ChecksumsAndIds<DocumentId> collection)
        {
            // This GetAssetsAsync call should be fast since they were just retrieved above.  There's a small chance
            // the asset-cache GC pass may have cleaned them up, but that should be exceedingly rare.
            var allDocChecksums = await this.GetAssetsAsync<DocumentStateChecksums>(
                AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), collection.Checksums, cancellationToken).ConfigureAwait(false);
            foreach (var docChecksums in allDocChecksums)
            {
                checksums.Add(docChecksums.Info);
                checksums.Add(docChecksums.Text);
            }
        }

        static void AddAll(HashSet<Checksum> checksums, ChecksumCollection checksumCollection)
        {
            foreach (var checksum in checksumCollection)
                checksums.Add(checksum);
        }
    }

    public async ValueTask SynchronizeAssetsAsync<T>(
        AssetPath assetPath, HashSet<Checksum> checksums, Dictionary<Checksum, T>? results, CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(checksums.Contains(Checksum.Null));
        if (checksums.Count == 0)
            return;

        using (Logger.LogBlock(FunctionId.AssetService_SynchronizeAssetsAsync, Checksum.GetChecksumsLogInfo, checksums, cancellationToken))
        {
            var missingChecksumsCount = 0;

            // Calculate the number of missing checksums upfront. Calculation is cheap and can help avoid extraneous allocations.
            foreach (var checksum in checksums)
            {
                if (!_assetCache.ContainsAsset(checksum))
                    missingChecksumsCount++;
            }

            var usePool = missingChecksumsCount <= PooledChecksumArraySize;
            var missingChecksums = usePool ? s_checksumPool.Allocate() : new Checksum[missingChecksumsCount];

            missingChecksumsCount = 0;
            foreach (var checksum in checksums)
            {
                if (_assetCache.TryGetAsset<T>(checksum, out var existing))
                {
                    AddResult(checksum, existing);
                }
                else
                {
                    if (missingChecksumsCount == missingChecksums.Length)
                    {
                        // This can happen if the asset cache has been modified by another thread during this method's execution.
                        var newMissingChecksums = new Checksum[missingChecksumsCount * 2];
                        Array.Copy(missingChecksums, newMissingChecksums, missingChecksumsCount);

                        if (usePool)
                        {
                            s_checksumPool.Free(missingChecksums);
                            usePool = false;
                        }

                        missingChecksums = newMissingChecksums;
                    }

                    missingChecksums[missingChecksumsCount] = checksum;
                    missingChecksumsCount++;
                }
            }

            if (missingChecksumsCount > 0)
            {
                var missingChecksumsMemory = new ReadOnlyMemory<Checksum>(missingChecksums, 0, missingChecksumsCount);

                var stopwatch = SharedStopwatch.StartNew();
                var missingAssets = await RequestAssetsAsync<T>(assetPath, missingChecksumsMemory, cancellationToken).ConfigureAwait(false);
                var time = stopwatch.Elapsed;
                var totalTime = s_start.Elapsed;

                IOUtilities.PerformIO(() =>
                {
                    lock (this)
                    {
                        File.AppendAllText(@"c:\temp\synclog.txt", $"{missingChecksumsCount},{checksums.Count},{time},{totalTime},{typeof(T).Name}\r\n");
                    }
                });

                Contract.ThrowIfTrue(missingChecksumsMemory.Length != missingAssets.Length);

                for (int i = 0, n = missingAssets.Length; i < n; i++)
                {
                    var missingChecksum = missingChecksums[i];
                    var missingAsset = missingAssets[i];

                    AddResult(missingChecksum, missingAsset);
                    _assetCache.GetOrAdd(missingChecksum, missingAsset);
                }
            }

            if (usePool)
                s_checksumPool.Free(missingChecksums);
        }

        return;

        void AddResult(Checksum checksum, T result)
        {
            if (results != null)
                results[checksum] = result;
        }
    }

    private async ValueTask<ImmutableArray<T>> RequestAssetsAsync<T>(
        AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, CancellationToken cancellationToken)
    {
#if NETCOREAPP
        Contract.ThrowIfTrue(checksums.Span.Contains(Checksum.Null));
#else
        Contract.ThrowIfTrue(checksums.Span.IndexOf(Checksum.Null) >= 0);
#endif

        if (checksums.Length == 0)
            return [];

        return await _assetSource.GetAssetsAsync<T>(_solutionChecksum, assetPath, checksums, _serializerService, cancellationToken).ConfigureAwait(false);
    }
}
