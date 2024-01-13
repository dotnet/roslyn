// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Implement this type to provide fixes for source code problems.
    /// Remember to use <see cref="ExportCodeFixProviderAttribute"/> so the host environment can offer your fixes in a UI.
    /// </summary>
    public abstract class CodeFixProvider
    {
        private protected ImmutableArray<string> CustomTags = ImmutableArray<string>.Empty;

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
        /// Computes the <see cref="CodeActionRequestPriority"/> group this provider should be considered to run at. Legal values
        /// this can be must be between <see cref="CodeActionRequestPriority.Low"/> and <see cref="CodeActionPriority.High"/>.
        /// </summary>
        /// <remarks>
        /// Values outside of this range will be clamped to be within that range.  Requests for <see
        /// cref="CodeActionRequestPriority.High"/> may be downgraded to <see cref="CodeActionRequestPriority.Default"/> as they
        /// poorly behaving high-priority providers can cause a negative user experience.
        /// </remarks>
        protected virtual CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.Default;

        /// <summary>
        /// Priority class this refactoring provider should run at. Returns <see
        /// cref="CodeActionRequestPriority.Default"/> if not overridden.  Slower, or less relevant, providers should
        /// override this and return a lower value to not interfere with computation of normal priority providers.
        /// </summary>
        public CodeActionRequestPriority RequestPriority
        {
            get
            {
                var priority = ComputeRequestPriority();
                Debug.Assert(priority is CodeActionRequestPriority.Low or CodeActionRequestPriority.Default or CodeActionRequestPriority.High, "Provider returned invalid priority");
                return priority.Clamp(this.CustomTags);
            }
        }
    }
}
