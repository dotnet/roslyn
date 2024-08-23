// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// This service provide a way to get roslyn objects from checksum
/// </summary>
internal sealed partial class AssetProvider(
    Checksum solutionChecksum,
    SolutionAssetCache assetCache,
    IAssetSource assetSource,
    SolutionServices solutionServices)
    : AbstractAssetProvider
{
    private const int PooledChecksumArraySize = 1024;
    private static readonly ObjectPool<Checksum[]> s_checksumPool = new(() => new Checksum[PooledChecksumArraySize], 16);

    private readonly Checksum _solutionChecksum = solutionChecksum;
    private readonly SolutionServices _solutionServices = solutionServices;
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

        using var _2 = ArrayBuilder<T>.GetInstance(1, out var builder);
        await this.GetAssetHelper<T>().GetAssetsAsync(
            assetPath, checksums,
            static (_, asset, builder) => builder.Add(asset),
            builder, cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfTrue(builder.Count != 1);

        return builder[0];
    }

    public override async Task GetAssetsAsync<T, TArg>(
        AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken) where TArg : default
    {
        await this.SynchronizeAssetsAsync(assetPath, checksums, callback, arg, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// This is the function called when we are <em>not</em> doing an incremental update, but are instead doing a bulk
    /// full sync.
    /// </summary>
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
                AssetPathKind.SolutionCompilationStateChecksums, solutionChecksum, cancellationToken).ConfigureAwait(false);

            // then grab the information we want off of the compilation state object.
            var solutionStateChecksum = await this.GetAssetAsync<SolutionStateChecksums>(
                AssetPathKind.SolutionStateChecksums, compilationStateChecksums.SolutionState, cancellationToken).ConfigureAwait(false);

            // Then grab the data we need off of hte state checksums
            using var _1 = ArrayBuilder<Task>.GetInstance(out var tasks);
            tasks.Add(this.GetAssetAsync<SourceGeneratorExecutionVersionMap>(
                AssetPathKind.SolutionSourceGeneratorExecutionVersionMap, compilationStateChecksums.SourceGeneratorExecutionVersionMap, cancellationToken).AsTask());
            tasks.Add(this.GetAssetAsync<SolutionInfo.SolutionAttributes>(
                AssetPathKind.SolutionAttributes, solutionStateChecksum.Attributes, cancellationToken).AsTask());
            tasks.Add(this.GetAssetsAsync<AnalyzerReference>(
                AssetPathKind.SolutionAnalyzerReferences, solutionStateChecksum.AnalyzerReferences, cancellationToken));
            tasks.Add(this.GetAssetAsync<ImmutableDictionary<string, StructuredAnalyzerConfigOptions>>(
                AssetPathKind.SolutionFallbackAnalyzerOptions, solutionStateChecksum.FallbackAnalyzerOptions, cancellationToken).AsTask());

            await Task.WhenAll(tasks).ConfigureAwait(false);

            using var _3 = ArrayBuilder<ProjectStateChecksums>.GetInstance(out var allProjectStateChecksums);
            await this.GetAssetHelper<ProjectStateChecksums>().GetAssetsAsync(
                AssetPathKind.ProjectStateChecksums,
                solutionStateChecksum.Projects.Checksums,
                static (_, asset, allProjectStateChecksums) => allProjectStateChecksums.Add(asset),
                arg: allProjectStateChecksums, cancellationToken).ConfigureAwait(false);

            // fourth, get all projects and documents in the solution 
            await SynchronizeProjectAssetsAsync(allProjectStateChecksums, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask SynchronizeProjectAssetsAsync(
        ArrayBuilder<ProjectStateChecksums> allProjectChecksums, CancellationToken cancellationToken)
    {
        // this will pull in assets that belong to the given project checksum to this remote host. this one is not
        // supposed to be used for functionality but only for perf. that is why it doesn't return anything. to get
        // actual data GetAssetAsync should be used. and that will return actual data and if there is any missing data
        // in cache, GetAssetAsync itself will bring that data in from data source (VS)

        // one can call this method to make cache hot for all assets that belong to the project checksum so that
        // GetAssetAsync call will most likely cache hit. it is most likely since we might change cache heuristic in
        // future which make data to live a lot shorter in the cache, and the data might get expired before one actually
        // consume the data. 
        using (Logger.LogBlock(FunctionId.AssetService_SynchronizeProjectAssetsAsync, message: null, cancellationToken))
        {
            // It's common to have two usage patterns of SynchronizeProjectAssetsAsync. Bulk syncing the majority of the
            // solution over, or just syncing a single project (or small set of projects) in response to a small change
            // (like a user edit).  For the bulk case, we want to make sure we're doing as few round trips as possible,
            // getting as much of the data we can in each call.  For the single project case though, we don't want to
            // have the host have to search the entire solution graph for data we know it contained within just that
            // project.
            //
            // So, we split up our strategy here based on how many projects we're syncing.  If it's 4 or less, we just
            // sync each project individually, passing the data to the host so it can limit its search to just that
            // project.  If it's more than that, we do it in bulk, knowing that as we're searching for a ton of
            // data, it's fine for the host to do a full pass for each of the data types we're looking for.
            if (allProjectChecksums.Count <= 4)
            {
                // Still sync the N projects in parallel.
                using var _ = ArrayBuilder<Task>.GetInstance(allProjectChecksums.Count, out var tasks);
                foreach (var singleProjectChecksums in allProjectChecksums)
                {
                    // Make a fresh singleton array, containing just this project checksum, and pass into the helper
                    // below. That way we can have just a single helper for actually doing the syncing, regardless of if
                    // we are are doing a single project or multiple.
                    ArrayBuilder<ProjectStateChecksums>.GetInstance(capacity: 1, out var tempBuffer);
                    tempBuffer.Add(singleProjectChecksums);

                    // We want to synchronize the assets just for this project.  So we can pass the ProjectId as a hint
                    // to limit the search on the host side.
                    tasks.Add(SynchronizeProjectAssetsWorkerAsync(
                        tempBuffer, singleProjectChecksums.ProjectId, freeArrayBuilder: true, cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            else
            {
                // We want to synchronize all assets in bulk.  Because of this, we can't narrow the search on the host
                // side to a particular ProjectId.
                await SynchronizeProjectAssetsWorkerAsync(
                    allProjectChecksums,
                    projectId: null,
                    freeArrayBuilder: false, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task SynchronizeProjectAssetsWorkerAsync(
        ArrayBuilder<ProjectStateChecksums> allProjectChecksums, ProjectId? projectId, bool freeArrayBuilder, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Yield();

            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            // Make parallel requests for all the project data across all projects at once. For each request, pass
            // in the appropriate info to let the search avoid looking at data unnecessarily.
            tasks.Add(SynchronizeProjectAssetAsync<ProjectInfo.ProjectAttributes>(new(AssetPathKind.ProjectAttributes, projectId), static p => p.Info));
            tasks.Add(SynchronizeProjectAssetAsync<CompilationOptions>(new(AssetPathKind.ProjectCompilationOptions, projectId), static p => p.CompilationOptions));
            tasks.Add(SynchronizeProjectAssetAsync<ParseOptions>(new(AssetPathKind.ProjectParseOptions, projectId), static p => p.ParseOptions));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<ProjectReference>(new(AssetPathKind.ProjectProjectReferences, projectId), static p => p.ProjectReferences));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<MetadataReference>(new(AssetPathKind.ProjectMetadataReferences, projectId), static p => p.MetadataReferences));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<AnalyzerReference>(new(AssetPathKind.ProjectAnalyzerReferences, projectId), static p => p.AnalyzerReferences));

            // Then sync each project's documents in parallel with each other.
            foreach (var projectChecksums in allProjectChecksums)
                tasks.Add(SynchronizeProjectDocumentsAsync(projectChecksums, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        finally
        {
            if (freeArrayBuilder)
                allProjectChecksums.Free();
        }

        return;

        Task SynchronizeProjectAssetAsync<TAsset>(AssetPath assetPath, Func<ProjectStateChecksums, Checksum> getChecksum)
            => SynchronizeProjectAssetOrCollectionAsync<TAsset, Func<ProjectStateChecksums, Checksum>>(
                assetPath,
                static (projectStateChecksums, checksums, getChecksum) => checksums.Add(getChecksum(projectStateChecksums)),
                getChecksum);

        Task SynchronizeProjectAssetCollectionAsync<TAsset>(AssetPath assetPath, Func<ProjectStateChecksums, ChecksumCollection> getChecksums)
            => SynchronizeProjectAssetOrCollectionAsync<TAsset, Func<ProjectStateChecksums, ChecksumCollection>>(
                assetPath,
                static (projectStateChecksums, checksums, getChecksums) => getChecksums(projectStateChecksums).AddAllTo(checksums),
                getChecksums);

        async Task SynchronizeProjectAssetOrCollectionAsync<TAsset, TArg>(
            AssetPath assetPath, Action<ProjectStateChecksums, HashSet<Checksum>, TArg> addAllChecksums, TArg arg)
        {
            await Task.Yield();
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            foreach (var projectChecksums in allProjectChecksums)
                addAllChecksums(projectChecksums, checksums, arg);

            await this.GetAssetsAsync<TAsset>(assetPath, checksums, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask SynchronizeAssetsAsync<T, TArg>(
        AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken)
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

            try
            {
                missingChecksumsCount = 0;
                foreach (var checksum in checksums)
                {
                    if (_assetCache.TryGetAsset<T>(checksum, out var existing))
                    {
                        callback?.Invoke(checksum, existing, arg!);
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
                    Contract.ThrowIfTrue(missingChecksumsMemory.Length == 0);

#if NET
                    Contract.ThrowIfTrue(missingChecksumsMemory.Span.Contains(Checksum.Null));
#else
                    Contract.ThrowIfTrue(missingChecksumsMemory.Span.IndexOf(Checksum.Null) >= 0);
#endif

                    var serializerService = this._solutionServices.GetRequiredService<ISerializerService>();
                    await _assetSource.GetAssetsAsync(
                        _solutionChecksum, assetPath, missingChecksumsMemory, serializerService,
                        static (
                            Checksum missingChecksum,
                            T missingAsset,
                            (AssetProvider assetProvider, Checksum[] missingChecksums, Action<Checksum, T, TArg>? callback, TArg? arg) tuple) =>
                        {
                            // Add to cache, returning the existing asset if it was added by another thread.
                            missingAsset = (T)tuple.assetProvider._assetCache.GetOrAdd(missingChecksum, missingAsset!);

                            // Let our caller know about the asset if they're asking for it.
                            tuple.callback?.Invoke(missingChecksum, missingAsset, tuple.arg!);
                        },
                        (this, missingChecksums, callback, arg),
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                if (usePool)
                    s_checksumPool.Free(missingChecksums);
            }
        }
    }
}
