// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this type to provide fixes for source code problems.
    /// Remember to use <see cref="ExportCodeFixProviderAttribute"/> so the host environment can offer your fixes in a UI.
    /// </summary>
    public abstract class CodeFixProvider
    {
        /// <summary>
        /// A list of diagnostic IDs that this provider can provide fixes for.
        /// </summary>
        public abstract ImmutableArray<string> FixableDiagnosticIds { get; }

        /// <summary>
        /// Computes one or more fixes for the specified <see cref="CodeFixContext"/>.
        /// </summary>
        /// <param name="context">
        /// A <see cref="CodeFixContext"/> containing context information about the diagnostics to fix.
        /// The context must only contain diagnostics with a <see cref="Diagnostic.Id"/> included in the <see cref="FixableDiagnosticIds"/> for the current provider.
        /// </param>
        public abstract Task RegisterCodeFixesAsync(CodeFixContext context);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this code fix provider.
        /// Return null if the provider doesn't support fix all/multiple occurrences.
        /// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
        /// </summary>
        public virtual FixAllProvider? GetFixAllProvider()
            => null;

        /// <summary>
        /// What priority this provider should run at.
        /// </summary>
        internal CodeActionRequestPriorityInternal RequestPriorityInternal
        {
            get
            {
                var priority = ComputeRequestPriorityInternal();
                // Note: CodeActionRequestPriority.Lowest is reserved for IConfigurationFixProvider.
                Contract.ThrowIfFalse(priority is CodeActionRequestPriorityInternal.Low or CodeActionRequestPriorityInternal.Normal or CodeActionRequestPriorityInternal.High);
                return priority;
            }
        }

        /// <summary>
        /// <see langword="virtual"/> so that our own internal providers get privileged access to the <see
        /// cref="CodeActionRequestPriorityInternal.High"/> bucket.  We do not currently expose that publicly as caution
        /// against poorly behaving external analyzers impacting experiences like 'add using'
        /// </summary>
        private protected virtual CodeActionRequestPriorityInternal ComputeRequestPriorityInternal()
            => this.RequestPriority.ConvertToInternalPriority();

        /// <summary>
        /// Priority class this refactoring provider should run at. Returns <see
        /// cref="CodeActionRequestPriority.Medium"/> if not overridden.  Slower, or less relevant, providers should
        /// override this and return a lower value to not interfere with computation of normal priority providers.
        /// </summary>
        public virtual CodeActionRequestPriority RequestPriority
            => CodeActionRequestPriority.Medium;
    }
}
