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
        internal CodeActionRequestPriority RequestPriority
        {
            get
            {
                var priority = ComputeRequestPriority();
                // Note: CodeActionRequestPriority.Lowest is reserved for IConfigurationFixProvider.
                Contract.ThrowIfFalse(priority is CodeActionRequestPriority.Low or CodeActionRequestPriority.Normal or CodeActionRequestPriority.High);
                return priority;
            }
        }

        private protected virtual CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.Normal;
    }
}
