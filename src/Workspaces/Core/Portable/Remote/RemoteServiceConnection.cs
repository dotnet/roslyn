// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SourceGeneration;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Abstracts a connection to a service implementing type <typeparamref name="TService"/>.
/// </summary>
/// <typeparam name="TService">Remote interface type of the service.</typeparam>
internal abstract class RemoteServiceConnection<TService> : IDisposable
    where TService : class
{
    public abstract void Dispose();

    // no solution, no callback

    public abstract ValueTask<bool> TryInvokeAsync(
        Func<TService, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Func<TService, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    // no solution, callback

    public abstract ValueTask<bool> TryInvokeAsync(
        Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    // solution, no callback

    public abstract ValueTask<bool> TryInvokeAsync(
        SolutionCompilationState solution,
        Func<TService, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<bool> TryInvokeOnlyToGenerateSourceAsync(
        SolutionState solution,
        Func<IRemoteSourceGenerationService, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        SolutionCompilationState solution,
        Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Specialized entry point used by the solution itself to call over to the OOP process to generate source-generator
    /// documents.  This entry point differs in that it doesn't want to sync over the entire solution (including
    /// generated docs), since the point of it is to generate those docs in the first place.  As such, it takes only a
    /// <see cref="SolutionState"/> object, representing just the actual primordial state of the solution.
    /// </summary>
    public abstract ValueTask<Optional<TResult>> TryInvokeOnlyToGenerateSourceAsync<TResult>(
        SolutionState solution,
        Func<IRemoteSourceGenerationService, Checksum, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    public ValueTask<bool> TryInvokeAsync(
        Solution solution,
        Func<TService, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(solution.CompilationState, invocation, cancellationToken);

    public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Solution solution,
        Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(solution.CompilationState, invocation, cancellationToken);

    // project, no callback

    public abstract ValueTask<bool> TryInvokeAsync(
        SolutionCompilationState solution,
        ProjectId projectId,
        Func<TService, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        SolutionCompilationState solution,
        ProjectId projectId,
        Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    public ValueTask<bool> TryInvokeAsync(
        Project project,
        Func<TService, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(project.Solution.CompilationState, project.Id, invocation, cancellationToken);

    public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Project project,
        Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(project.Solution.CompilationState, project.Id, invocation, cancellationToken);

    // solution, callback

    public abstract ValueTask<bool> TryInvokeAsync(
        SolutionCompilationState solution,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        SolutionCompilationState solution,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    public ValueTask<bool> TryInvokeAsync(
        Solution solution,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(solution.CompilationState, invocation, cancellationToken);

    public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Solution solution,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(solution.CompilationState, invocation, cancellationToken);

    // project, callback

    public abstract ValueTask<bool> TryInvokeAsync(
        SolutionCompilationState solution,
        ProjectId projectId,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public abstract ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        SolutionCompilationState solution,
        ProjectId projectId,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken);

    public ValueTask<bool> TryInvokeAsync(
        Project project,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(project.Solution.CompilationState, project.Id, invocation, cancellationToken);

    public ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(
        Project project,
        Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(project.Solution.CompilationState, project.Id, invocation, cancellationToken);

    // multiple solution, no callback

    public abstract ValueTask<bool> TryInvokeAsync(
        SolutionCompilationState solution1,
        SolutionCompilationState solution2,
        Func<TService, Checksum, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken);

    public ValueTask<bool> TryInvokeAsync(
        Solution solution1,
        Solution solution2,
        Func<TService, Checksum, Checksum, CancellationToken, ValueTask> invocation,
        CancellationToken cancellationToken)
        => TryInvokeAsync(solution1.CompilationState, solution2.CompilationState, invocation, cancellationToken);
}
