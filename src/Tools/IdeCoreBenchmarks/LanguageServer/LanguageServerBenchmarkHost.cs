// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Xunit.Abstractions;

namespace IdeCoreBenchmarks;

internal sealed class LanguageServerBenchmarkHost : AbstractLanguageServerMefHost
{
    private const string CollectMetadataCacheStatisticsEnvironmentVariable = "ROSLYN_BENCHMARK_COLLECT_SHARED_METADATA_CACHE_STATISTICS";

    internal LanguageServerBenchmarkHost()
        : base(NullTestOutputHelper.Instance)
    {
    }

    internal Task WarmCompositionCacheAsync()
        => WarmCompositionCacheCoreAsync();

    internal async Task<BenchmarkTestDaemon> CreateDaemonAsync(bool useSharedMetadataCache)
    {
        var collectStatistics = Environment.GetEnvironmentVariable(CollectMetadataCacheStatisticsEnvironmentVariable) == "1";
        var daemon = await CreateDaemonServerAsync(
            useSharedMetadataCache: useSharedMetadataCache,
            collectSharedMetadataCacheStatistics: collectStatistics);
        if (collectStatistics)
            _ = GetSharedMetadataCacheStatistics(daemon);

        Func<MetadataCacheStatistics?> getStatistics = collectStatistics
            ? () => GetSharedMetadataCacheStatistics(daemon)
            : static () => null;
        return new(
            () => CreateClientAsync(daemon),
            getStatistics,
            daemon.DisposeAsync);
    }

    private static MetadataCacheStatistics? GetSharedMetadataCacheStatistics(TestDaemon daemon)
    {
        var statistics = daemon.GetSharedMetadataCacheStatistics();
        return statistics is { } value
            ? new MetadataCacheStatistics(
                value.RequestCount,
                value.HitCount,
                value.MissCount,
                value.MetadataLoadCount,
                value.FailedLoadCount,
                value.DuplicateLoadCount,
                value.NonCacheableLoadCount,
                value.ChangedDuringLoadCount,
                value.DeadEntryRemovalCount,
                value.EntryCount)
            : null;
    }

    private static TestServer Wrap(TestLspServer server)
        => new(server.OpenSolutionAsync, server.DisposeAsync);

    private static async Task<TestServer> CreateClientAsync(TestDaemon daemon)
        => Wrap(await daemon.CreateClientAsync());

    internal sealed class TestServer : IAsyncDisposable
    {
        private readonly Func<string, CancellationToken, Task> _openSolutionAsync;
        private readonly Func<ValueTask> _disposeAsync;

        internal TestServer(
            Func<string, CancellationToken, Task> openSolutionAsync,
            Func<ValueTask> disposeAsync)
        {
            _openSolutionAsync = openSolutionAsync;
            _disposeAsync = disposeAsync;
        }

        internal Task OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken)
            => _openSolutionAsync(solutionFilePath, cancellationToken);

        public ValueTask DisposeAsync()
            => _disposeAsync();
    }

    internal sealed class BenchmarkTestDaemon : IAsyncDisposable
    {
        private readonly Func<Task<TestServer>> _createClientAsync;
        private readonly Func<MetadataCacheStatistics?> _getSharedMetadataCacheStatistics;
        private readonly Func<ValueTask> _disposeAsync;

        internal BenchmarkTestDaemon(
            Func<Task<TestServer>> createClientAsync,
            Func<MetadataCacheStatistics?> getSharedMetadataCacheStatistics,
            Func<ValueTask> disposeAsync)
        {
            _createClientAsync = createClientAsync;
            _getSharedMetadataCacheStatistics = getSharedMetadataCacheStatistics;
            _disposeAsync = disposeAsync;
        }

        internal async Task<TestServer> CreateClientAsync()
            => await _createClientAsync();

        internal MetadataCacheStatistics? GetSharedMetadataCacheStatistics()
            => _getSharedMetadataCacheStatistics();

        public ValueTask DisposeAsync()
            => _disposeAsync();
    }

    internal readonly record struct MetadataCacheStatistics(
        long RequestCount,
        long HitCount,
        long MissCount,
        long MetadataLoadCount,
        long FailedLoadCount,
        long DuplicateLoadCount,
        long NonCacheableLoadCount,
        long ChangedDuringLoadCount,
        long DeadEntryRemovalCount,
        int EntryCount)
    {
        internal MetadataCacheStatistics Subtract(MetadataCacheStatistics earlier)
            => new(
                RequestCount - earlier.RequestCount,
                HitCount - earlier.HitCount,
                MissCount - earlier.MissCount,
                MetadataLoadCount - earlier.MetadataLoadCount,
                FailedLoadCount - earlier.FailedLoadCount,
                DuplicateLoadCount - earlier.DuplicateLoadCount,
                NonCacheableLoadCount - earlier.NonCacheableLoadCount,
                ChangedDuringLoadCount - earlier.ChangedDuringLoadCount,
                DeadEntryRemovalCount - earlier.DeadEntryRemovalCount,
                EntryCount);
    }

    private sealed class NullTestOutputHelper : ITestOutputHelper
    {
        public static NullTestOutputHelper Instance { get; } = new();

        public void WriteLine(string message)
        {
        }

        public void WriteLine(string format, params object[] args)
        {
        }
    }
}
