// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        public async Task<IEnumerable<CodeActionOperation>?> GetOperationsAsync(object? options, CancellationToken cancellationToken)
        {
            if (options == null)
            {
                return SpecializedCollections.EmptyEnumerable<CodeActionOperation>();
            }

            var operations = await this.ComputeOperationsAsync(options, cancellationToken).ConfigureAwait(false);

            if (operations != null)
            {
                operations = await this.PostProcessAsync(operations, cancellationToken).ConfigureAwait(false);
            }

            return operations;
        }

        internal override async Task<ImmutableArray<CodeActionOperation>> GetOperationsCoreAsync(
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            var options = this.GetOptions(cancellationToken);
            return (await this.GetOperationsAsync(options, cancellationToken).ConfigureAwait(false)).ToImmutableArrayOrEmpty();
        }

        /// <summary>
        /// Override this method to compute the operations that implement this <see cref="CodeAction"/>.
        /// </summary>
        /// <param name="options">An object instance returned from a call to <see cref="GetOptions(CancellationToken)"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        protected abstract Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(object options, CancellationToken cancellationToken);

        protected override Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            => SpecializedTasks.EmptyEnumerable<CodeActionOperation>();
    }
}
