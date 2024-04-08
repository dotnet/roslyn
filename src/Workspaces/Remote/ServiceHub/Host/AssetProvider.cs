// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// This service provide a way to get roslyn objects from checksum
/// </summary>
internal sealed partial class AssetProvider(Checksum solutionChecksum, SolutionAssetCache assetCache, IAssetSource assetSource, ISerializerService serializerService)
    : AbstractAssetProvider
{
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

        var called = false;
        T? result = default;
        await this.SynchronizeAssetsAsync<T, VoidResult>(assetPath, checksums, (_, _, asset, _) =>
        {
            Contract.ThrowIfTrue(called);
            called = true;
            result = asset;
        }, default, cancellationToken).ConfigureAwait(false);
        Contract.ThrowIfNull((object?)result);

        return result;
    }

    public override async ValueTask GetAssetsAsync<T, TArg>(
        AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, int, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
    {
        await this.SynchronizeAssetsAsync(assetPath, checksums, callback, arg, cancellationToken).ConfigureAwait(false);
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
            // first, get top level solution state for the given solution checksum
            var compilationStateChecksums = await this.GetAssetAsync<SolutionCompilationStateChecksums>(
                assetPath: AssetPath.SolutionOnly, solutionChecksum, cancellationToken).ConfigureAwait(false);

            using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);

            // second, get direct children of the solution compilation state.
            compilationStateChecksums.AddAllTo(checksums);
            await this.SynchronizeAssetsAsync<object, VoidResult>(assetPath: AssetPath.SolutionOnly, checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);

            // third, get direct children of the solution state.
            var stateChecksums = await this.GetAssetAsync<SolutionStateChecksums>(
                assetPath: AssetPath.SolutionOnly, compilationStateChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

            // Ask for solutions and top-level projects as the solution checksums will contain the checksums for
            // the project states and we want to get that all in one batch.
            checksums.Clear();
            stateChecksums.AddAllTo(checksums);

            using var _2 = PooledDictionary<Checksum, object>.GetInstance(out var checksumToObjects);

            await this.SynchronizeAssetsAsync<object, Dictionary<Checksum, object>>(
                assetPath: AssetPath.SolutionAndTopLevelProjectsOnly, checksums,
                static (checksum, index, asset, checksumToObjects) => checksumToObjects.Add(checksum, asset),
                arg: checksumToObjects, cancellationToken).ConfigureAwait(false);

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
            await this.SynchronizeAssetsAsync<object, VoidResult>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);

            checksums.Clear();

            // Then synchronize the info about all the documents within.
            await CollectChecksumChildrenAsync(checksums, projectChecksums.Documents).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(checksums, projectChecksums.AdditionalDocuments).ConfigureAwait(false);
            await CollectChecksumChildrenAsync(checksums, projectChecksums.AnalyzerConfigDocuments).ConfigureAwait(false);

            await this.SynchronizeAssetsAsync<object, VoidResult>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);
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

    public async ValueTask SynchronizeAssetsAsync<T, TArg>(
        AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, int, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken)
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

                var indexExpectation = 0;
                await RequestAssetsAsync(assetPath, missingChecksumsMemory, static (int index, T missingAsset) =>
                {
                    Contract.ThrowIfTrue(indexExpectation != index);

                    var missingChecksum = missingChecksums[index];

                    AddResult(missingChecksum, missingAsset);
                    _assetCache.GetOrAdd(missingChecksum, missingAsset!);

                    indexExpectation++;
                }, cancellationToken).ConfigureAwait(false);

                Contract.ThrowIfTrue(indexExpectation != missingChecksumsCount);
            }

            if (usePool)
                s_checksumPool.Free(missingChecksums);
        }

        return;

        void AddResult(Checksum checksum, T result)
        {
            callback?.Invoke(checksum, result);
        }
    }

    private async ValueTask RequestAssetsAsync<T, TArg>(
        AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, Action<int, T, TArg> callback, CancellationToken cancellationToken)
    {
#if NETCOREAPP
        Contract.ThrowIfTrue(checksums.Span.Contains(Checksum.Null));
#else
        Contract.ThrowIfTrue(checksums.Span.IndexOf(Checksum.Null) >= 0);
#endif

        if (checksums.Length == 0)
            return;

        await _assetSource.GetAssetsAsync(_solutionChecksum, assetPath, checksums, _serializerService, callback, cancellationToken).ConfigureAwait(false);
    }
}
