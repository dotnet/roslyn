// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class NoOpRemoteServiceConnection<T> : RemoteServiceConnection<T>
    where T : class
{
    public static readonly NoOpRemoteServiceConnection<T> Instance = new();

    private NoOpRemoteServiceConnection()
    {
    }

    public override void Dispose()
    {
    }

    public override ValueTask<bool> TryInvokeAsync(Func<T, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<T, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(Func<T, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<T, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(SolutionCompilationState compilationState, Func<T, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionCompilationState compilationState, Func<T, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(SolutionCompilationState compilationState, ProjectId projectId, Func<T, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionCompilationState compilationState, ProjectId projectId, Func<T, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(SolutionCompilationState compilationState, Func<T, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionCompilationState compilationState, Func<T, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(SolutionCompilationState compilationState, ProjectId projectId, Func<T, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionCompilationState compilationState, ProjectId projectId, Func<T, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        => default;

    public override ValueTask<bool> TryInvokeAsync(SolutionCompilationState compilationState1, SolutionCompilationState compilationState2, Func<T, Checksum, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        => default;
}
