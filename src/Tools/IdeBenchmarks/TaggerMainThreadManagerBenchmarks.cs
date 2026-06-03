// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VSDiagnostics;
using Microsoft.VisualStudio.Threading;

namespace IdeBenchmarks;

/// <summary>
/// Measures the allocation overhead of <see cref="TaggerMainThreadManager.PerformWorkOnMainThreadAsync"/>
/// when called on the main thread (the fast path exercised during normal editor tag recomputation).
/// </summary>
[CPUUsageDiagnoser]
public class TaggerMainThreadManagerBenchmarks
{
    private TaggerMainThreadManager _manager = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create a JoinableTaskContext whose "main thread" is the current thread.
        // BenchmarkDotNet runs GlobalSetup and Benchmark methods on the same thread,
        // so IsOnMainThread will be true during benchmarks — exercising the fast path.
#pragma warning disable VSSDK005 // Use ThreadHelper.JoinableTaskContext singleton - N/A, used for benchmark code
        var joinableTaskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005
        var threadingContext = new BenchmarkThreadingContext(joinableTaskContext);
        _manager = new TaggerMainThreadManager(threadingContext, AsynchronousOperationListenerProvider.NullProvider);
    }

    [Benchmark]
    public async ValueTask<object?> PerformWorkOnMainThread_ReturnsNull()
    {
        // Action returns null — simulates the case where TryAddSpansToTag returns false
        // (e.g., during a layout pass). This is the cheapest possible call that still
        // exercises the full PerformWorkOnMainThreadAsync code path on the main thread.
        var result = await _manager.PerformWorkOnMainThreadAsync(static () => null, CancellationToken.None);
        return result;
    }

    [Benchmark]
    public async ValueTask<object?> PerformWorkOnMainThread_ReturnsValue()
    {
        // Action returns a value — simulates the normal success path where UI data is collected.
        var result = await _manager.PerformWorkOnMainThreadAsync(
            static () => (true, (Microsoft.VisualStudio.Text.SnapshotPoint?)null, default(Microsoft.CodeAnalysis.Collections.OneOrMany<Microsoft.VisualStudio.Text.SnapshotSpan>)),
            CancellationToken.None);
        return result;
    }

    /// <summary>
    /// Minimal <see cref="IThreadingContext"/> for benchmarking. The current thread becomes the "main thread".
    /// </summary>
    private sealed class BenchmarkThreadingContext(JoinableTaskContext joinableTaskContext) : IThreadingContext
    {
        public bool HasMainThread => true;
        public JoinableTaskContext JoinableTaskContext { get; } = joinableTaskContext;
        public JoinableTaskFactory JoinableTaskFactory { get; } = joinableTaskContext.Factory;
        public CancellationToken DisposalToken => CancellationToken.None;

        public JoinableTask RunWithShutdownBlockAsync(Func<CancellationToken, Task> func)
            => JoinableTaskFactory.RunAsync(() => func(CancellationToken.None));
    }
}
