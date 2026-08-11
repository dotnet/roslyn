// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal sealed class OnDemandProjectLoadOperation
{
    private readonly Task<OnDemandProjectLoadResult> _projectCompletion;
    private readonly Func<Task<OnDemandProjectLoadResult>>? _dependencyCompletionFactory;
    private readonly object _gate = new();
    private Task<OnDemandProjectLoadResult>? _dependencyCompletion;

    public static OnDemandProjectLoadOperation Completed { get; } = new(Task.FromResult(OnDemandProjectLoadResult.Empty), dependencyCompletionFactory: null);

    public OnDemandProjectLoadOperation(
        Task<OnDemandProjectLoadResult> projectCompletion,
        Func<Task<OnDemandProjectLoadResult>>? dependencyCompletionFactory)
    {
        _projectCompletion = projectCompletion;
        _dependencyCompletionFactory = dependencyCompletionFactory;
    }

    public OnDemandProjectLoadOperation(Task completion)
        : this(CompleteAsync(completion), dependencyCompletionFactory: null)
    {
    }

    public Task<OnDemandProjectLoadResult> WaitAsync(CancellationToken cancellationToken)
        => WaitAsync(LspSolutionContextPreference.Project, cancellationToken);

    public Task<OnDemandProjectLoadResult> WaitAsync(LspSolutionContextPreference preference, CancellationToken cancellationToken)
    {
        var completion = preference == LspSolutionContextPreference.ProjectAndDependencies
            ? GetDependencyCompletionAsync()
            : _projectCompletion;
        return completion.WithCancellation(cancellationToken);
    }

    private Task<OnDemandProjectLoadResult> GetDependencyCompletionAsync()
    {
        lock (_gate)
            return _dependencyCompletion ??= _dependencyCompletionFactory?.Invoke() ?? _projectCompletion;
    }

    private static async Task<OnDemandProjectLoadResult> CompleteAsync(Task completion)
    {
        await completion.ConfigureAwait(false);
        return OnDemandProjectLoadResult.Empty;
    }
}
