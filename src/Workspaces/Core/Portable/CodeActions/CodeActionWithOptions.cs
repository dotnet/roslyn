// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// A <see cref="CodeAction"/> that can vary with user specified options.  Override one of <see
/// cref="ComputeOperationsAsync(object, CancellationToken)"/> or <see cref="ComputeOperationsAsync(object,
/// IProgress{CodeAnalysisProgress}, CancellationToken)"/> to actually compute the operations for this action.
/// </summary>
public abstract class CodeActionWithOptions : CodeAction
{
    /// <summary>
    /// Gets the options to use with this code action.
    /// This method is guaranteed to be called on the UI thread.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>An implementation specific object instance that holds options for applying the code action.</returns>
    public abstract object? GetOptions(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the <see cref="CodeActionOperation"/>'s for this <see cref="CodeAction"/> given the specified options.
    /// </summary>
    /// <param name="options">An object instance returned from a prior call to <see cref="GetOptions(CancellationToken)"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public Task<IEnumerable<CodeActionOperation>?> GetOperationsAsync(object? options, CancellationToken cancellationToken)
        => GetOperationsAsync(originalSolution: null!, options, CodeAnalysisProgress.None, cancellationToken);

    internal async Task<IEnumerable<CodeActionOperation>?> GetOperationsAsync(
        Solution originalSolution, object? options, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        if (options == null)
            return [];

        var operations = await this.ComputeOperationsAsync(options, progress, cancellationToken).ConfigureAwait(false);

        if (operations != null)
        {
            operations = await PostProcessAsync(originalSolution, operations, cancellationToken).ConfigureAwait(false);
        }

        return operations;
    }

    private protected sealed override async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
        Solution originalSolution, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        var options = this.GetOptions(cancellationToken);
        var operations = await this.GetOperationsAsync(originalSolution, options, progress, cancellationToken).ConfigureAwait(false);
        return operations.ToImmutableArrayOrEmpty();
    }

    /// <summary>
    /// Override this method to compute the operations that implement this <see cref="CodeAction"/>.
    /// </summary>
    /// <param name="options">An object instance returned from a call to <see cref="GetOptions(CancellationToken)"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    protected virtual Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
        => SpecializedTasks.EmptyEnumerable<CodeActionOperation>();

    /// <summary>
    /// Override this method to compute the operations that implement this <see cref="CodeAction"/>. Prefer
    /// overriding this method over <see cref="ComputeOperationsAsync(object, CancellationToken)"/> when computation
    /// is long running and progress should be shown to the user.
    /// </summary>
    protected virtual Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
        => ComputeOperationsAsync(options, cancellationToken);

    protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
        => SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
}
