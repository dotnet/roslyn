// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests;
using Xunit.Abstractions;

namespace IdeCoreBenchmarks;

internal sealed class LanguageServerBenchmarkHost : AbstractLanguageServerMefHost
{
    internal LanguageServerBenchmarkHost()
        : base(NullTestOutputHelper.Instance)
    {
    }

    internal Task WarmCompositionCacheAsync()
        => WarmCompositionCacheCoreAsync();

    internal async Task<BenchmarkTestDaemon> CreateDaemonAsync()
    {
        var daemon = await CreateDaemonServerAsync();
        return new(
            () => CreateClientAsync(daemon),
            daemon.DisposeAsync);
    }

    private static TestServer Wrap(TestLspServer server)
        => new(server.OpenProjectsAsync, server.OpenSolutionAsync, server.DisposeAsync);

    private static async Task<TestServer> CreateClientAsync(TestDaemon daemon)
        => Wrap(await daemon.CreateClientAsync());

    internal sealed class TestServer : IAsyncDisposable
    {
        private readonly Func<ImmutableArray<string>, CancellationToken, Task> _openProjectsAsync;
        private readonly Func<string, CancellationToken, Task> _openSolutionAsync;
        private readonly Func<ValueTask> _disposeAsync;

        internal TestServer(
            Func<ImmutableArray<string>, CancellationToken, Task> openProjectsAsync,
            Func<string, CancellationToken, Task> openSolutionAsync,
            Func<ValueTask> disposeAsync)
        {
            _openProjectsAsync = openProjectsAsync;
            _openSolutionAsync = openSolutionAsync;
            _disposeAsync = disposeAsync;
        }

        internal Task OpenProjectsAsync(ImmutableArray<string> projectFilePaths, CancellationToken cancellationToken)
            => _openProjectsAsync(projectFilePaths, cancellationToken);

        internal Task OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken)
            => _openSolutionAsync(solutionFilePath, cancellationToken);

        public ValueTask DisposeAsync()
            => _disposeAsync();
    }

    internal sealed class BenchmarkTestDaemon : IAsyncDisposable
    {
        private readonly Func<Task<TestServer>> _createClientAsync;
        private readonly Func<ValueTask> _disposeAsync;

        internal BenchmarkTestDaemon(
            Func<Task<TestServer>> createClientAsync,
            Func<ValueTask> disposeAsync)
        {
            _createClientAsync = createClientAsync;
            _disposeAsync = disposeAsync;
        }

        internal async Task<TestServer> CreateClientAsync()
            => await _createClientAsync();

        public ValueTask DisposeAsync()
            => _disposeAsync();
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
