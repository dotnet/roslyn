// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// A <see cref="CodeAction"/> that can vary with user specified options.
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
            => GetOperationsAsync(originalSolution: null!, options, NullProgress<CodeActionProgress>.Instance, cancellationToken);

        internal async Task<IEnumerable<CodeActionOperation>?> GetOperationsAsync(
            Solution originalSolution, object? options, IProgress<CodeActionProgress> progress, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }

            var operations = await this.ComputeOperationsAsync(options, progress, cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                operations = await this.PostProcessAsync(originalSolution, operations, cancellationToken).ConfigureAwait(false);
            }

            return operations;
        }

        private protected sealed override async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
            Solution originalSolution, IProgress<CodeActionProgress> progress, CancellationToken cancellationToken)
        {
            var options = this.GetOptions(cancellationToken);
            var operations = await this.GetOperationsAsync(originalSolution, options, progress, cancellationToken).ConfigureAwait(false);
            return operations.ToImmutableArrayOrEmpty();
        }

        /// <summary>
        /// Override this method to compute the operations that implement this <see cref="CodeAction"/>.  Override <see
        /// cref="ComputeOperationsAsync(object, IProgress{CodeActionProgress}, CancellationToken)"/> to report progress
        /// progress while computing the operations.
        /// </summary>
        /// <param name="options">An object instance returned from a call to <see cref="GetOptions(CancellationToken)"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        protected virtual Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken)
            => SpecializedTasks.EmptyEnumerable<CodeActionOperation>();

        /// <summary>
        /// Override this method to compute the operations that implement this <see cref="CodeAction"/>.
        /// </summary>
        protected virtual Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, IProgress<CodeActionProgress> progress, CancellationToken cancellationToken)
            => ComputeOperationsAsync(options, cancellationToken);

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
    }
}
