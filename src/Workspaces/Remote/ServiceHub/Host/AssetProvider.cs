// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
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
    private const string s_logFile = @"c:\temp\sync\synclog.txt";
    private static readonly SharedStopwatch s_start = SharedStopwatch.StartNew();

    static AssetProvider()
    {
        IOUtilities.PerformIO(() => File.Delete(s_logFile));
    }

    private const int PooledChecksumArraySize = 1024;
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

        using var _2 = ArrayBuilder<T>.GetInstance(1, out var builder);
        await this.SynchronizeAssetsAsync<T, ArrayBuilder<T>>(
            assetPath, checksums,
            static (_, asset, builder) => builder.Add(asset),
            builder, cancellationToken).ConfigureAwait(false);

        Contract.ThrowIfTrue(builder.Count != 1);

        return builder[0];
    }

    public override async ValueTask GetAssetsAsync<T, TArg>(
        AssetPath assetPath, HashSet<Checksum> checksums, Action<Checksum, T, TArg>? callback, TArg? arg, CancellationToken cancellationToken) where TArg : default
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
                assetPath: AssetPath.SolutionAndTopLevelProjectsOnly,
                checksums,
                static (checksum, asset, checksumToObjects) => checksumToObjects.Add(checksum, asset),
                arg: checksumToObjects, cancellationToken).ConfigureAwait(false);

            using var _3 = ArrayBuilder<ProjectStateChecksums>.GetInstance(out var allProjectStateChecksums);

            // fourth, get all projects and documents in the solution 
            foreach (var (projectChecksum, _) in stateChecksums.Projects)
            {
                var projectStateChecksums = (ProjectStateChecksums)checksumToObjects[projectChecksum];
                allProjectStateChecksums.Add(projectStateChecksums);
            }

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
            await SynchronizeProjectAssetsWorkerAsync().ConfigureAwait(false);
        }

        async ValueTask SynchronizeProjectAssetsWorkerAsync()
        {
            using var _ = ArrayBuilder<Task>.GetInstance(out var tasks);

            // Make parallel requests for all the project data across all projects at once.
            tasks.Add(SynchronizeProjectAssetAsync<ProjectInfo.ProjectAttributes>(static p => p.Info, cancellationToken));
            tasks.Add(SynchronizeProjectAssetAsync<CompilationOptions>(static p => p.CompilationOptions, cancellationToken));
            tasks.Add(SynchronizeProjectAssetAsync<ParseOptions>(static p => p.ParseOptions, cancellationToken));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<ProjectReference>(static p => p.ProjectReferences, cancellationToken));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<MetadataReference>(static p => p.MetadataReferences, cancellationToken));
            tasks.Add(SynchronizeProjectAssetCollectionAsync<AnalyzerReference>(static p => p.AnalyzerReferences, cancellationToken));

            // Then sync each project's documents in parallel with each other.
            foreach (var projectChecksums in allProjectChecksums)
                tasks.Add(SynchronizeProjectDocumentsAsync(projectChecksums, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        static void AddAll(HashSet<Checksum> checksums, ChecksumCollection checksumCollection)
        {
            foreach (var checksum in checksumCollection)
                checksums.Add(checksum);
        }

        async Task SynchronizeProjectAssetAsync<TAsset>(Func<ProjectStateChecksums, Checksum> getChecksum, CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            foreach (var projectChecksums in allProjectChecksums)
                checksums.Add(getChecksum(projectChecksums));

            await this.SynchronizeAssetsAsync<TAsset, VoidResult>(
                AssetPath.SolutionAndTopLevelProjectsOnly, checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);
        }

        async Task SynchronizeProjectAssetCollectionAsync<TAsset>(Func<ProjectStateChecksums, ChecksumCollection> getChecksums, CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var _ = PooledHashSet<Checksum>.GetInstance(out var checksums);

            foreach (var projectChecksums in allProjectChecksums)
                AddAll(checksums, getChecksums(projectChecksums));

            await this.SynchronizeAssetsAsync<TAsset, VoidResult>(
                AssetPath.SolutionAndTopLevelProjectsOnly, checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);
        }

        async Task SynchronizeProjectDocumentsAsync(ProjectStateChecksums projectChecksums, CancellationToken cancellationToken)
        {
            await Task.Yield();
            using var _1 = PooledHashSet<Checksum>.GetInstance(out var checksums);

            AddAll(checksums, projectChecksums.Documents.Checksums);
            AddAll(checksums, projectChecksums.AdditionalDocuments.Checksums);
            AddAll(checksums, projectChecksums.AnalyzerConfigDocuments.Checksums);

            await this.SynchronizeAssetsAsync<DocumentStateChecksums, VoidResult>(
                AssetPath.SolutionAndTopLevelProjectsOnly, checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);

            // First, fetch all the DocumentStateChecksums for all the documents in the project.
            using var _2 = ArrayBuilder<DocumentStateChecksums>.GetInstance(out var allDocumentStateChecksums);
            await this.SynchronizeAssetsAsync<DocumentStateChecksums, ArrayBuilder<DocumentStateChecksums>>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums,
                static (_, documentStateChecksums, allDocumentStateChecksums) => allDocumentStateChecksums.Add(documentStateChecksums),
                allDocumentStateChecksums,
                cancellationToken).ConfigureAwait(false);

            // Now go and fetch the info and text for all of those documents.
            checksums.Clear();
            foreach (var docChecksums in allDocumentStateChecksums)
            {
                checksums.Add(docChecksums.Info);
                checksums.Add(docChecksums.Text);
            }

            await this.SynchronizeAssetsAsync<object, VoidResult>(
                assetPath: AssetPath.ProjectAndDocuments(projectChecksums.ProjectId), checksums, callback: null, arg: default, cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask SynchronizeAssetsAsync<T, TArg>(
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

                    var stopwatch = SharedStopwatch.StartNew();

                    await RequestAssetsAsync(
                        assetPath, missingChecksumsMemory,
                        static (
                            int index,
                            T missingAsset,
                            (AssetProvider assetProvider, Checksum[] missingChecksums, Action<Checksum, T, TArg>? callback, TArg? arg) tuple) =>
                        {
                            var missingChecksum = tuple.missingChecksums[index];

                            tuple.callback?.Invoke(missingChecksum, missingAsset, tuple.arg!);
                            tuple.assetProvider._assetCache.GetOrAdd(missingChecksum, missingAsset!);
                        },
                        (this, missingChecksums, callback, arg),
                        cancellationToken).ConfigureAwait(false);

                    var time = stopwatch.Elapsed;
                    var totalTime = s_start.Elapsed;

                    IOUtilities.PerformIO(() =>
                    {
                        lock (this)
                        {
                            File.AppendAllText(s_logFile, $"{missingChecksumsCount},{checksums.Count},{time},{totalTime},{typeof(T).Name}\r\n");
                        }
                    });
                }
            }
            finally
            {
                if (usePool)
                    s_checksumPool.Free(missingChecksums);
            }
        }

        return;
    }

    private async ValueTask RequestAssetsAsync<T, TArg>(
        AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, Action<int, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
    {
#if NETCOREAPP
        Contract.ThrowIfTrue(checksums.Span.Contains(Checksum.Null));
#else
        Contract.ThrowIfTrue(checksums.Span.IndexOf(Checksum.Null) >= 0);
#endif

        if (checksums.Length == 0)
            return;

        await _assetSource.GetAssetsAsync(_solutionChecksum, assetPath, checksums, _serializerService, callback, arg, cancellationToken).ConfigureAwait(false);
    }
}
