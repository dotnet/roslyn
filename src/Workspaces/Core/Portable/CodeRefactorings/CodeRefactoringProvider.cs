// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Inherit this type to provide source code refactorings.
    /// Remember to use <see cref="ExportCodeRefactoringProviderAttribute"/> so the host environment can offer your refactorings in a UI.
    /// </summary>
    public abstract class CodeRefactoringProvider
    {
        /// <summary>
        /// Computes one or more refactorings for the specified <see cref="CodeRefactoringContext"/>.
        /// </summary>
        public abstract Task ComputeRefactoringsAsync(CodeRefactoringContext context);

        /// <summary>
        /// Gets an optional <see cref="FixAllProvider"/> that can apply multiple occurrences of code refactoring(s)
        /// registered by this code refactoring provider across the supported <see cref="CodeFixes.FixAllScope"/>s.
        /// Return null if the provider doesn't support fix all operation.
        /// </summary>
        /// <remarks>
        /// TODO: Make public, tracked with https://github.com/dotnet/roslyn/issues/60703
        /// </remarks>
        internal virtual FixAllProvider? GetFixAllProvider()
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
        /// cref="CodeActionRequestPriority.Default"/> if not overridden.  Slower, or less relevant, providers should
        /// override this and return a lower value to not interfere with computation of normal priority providers.
        /// </summary>
        public virtual CodeActionRequestPriority RequestPriority
            => CodeActionRequestPriority.Default;
    }
}
