// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string CodeActionTitle { get; }

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            return FixAllContextHelper.GetFixAllCodeActionAsync(
                fixAllContext,
                CodeActionTitle,
                async (context, document, diagnostics) =>
                {
                    // if we didn't get a new root back, just return null to indicate we had no work to do here.
                    var newRoot = await this.FixAllInDocumentAsync(context, document, diagnostics).ConfigureAwait(false);
                    if (newRoot == null)
                        return null;

                    return document.WithSyntaxRoot(newRoot);
                });
        }

        /// <summary>
        /// Fixes all occurrences of a diagnostic in a specific document.
        /// </summary>
        /// <param name="fixAllContext">The context for the Fix All operation.</param>
        /// <param name="document">The document to fix.</param>
        /// <param name="diagnostics">The diagnostics to fix in the document.</param>
        /// <returns>
        /// <para>The new <see cref="SyntaxNode"/> representing the root of the fixed document.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/>, if no changes were made to the document.</para>
        /// </returns>
        protected abstract Task<SyntaxNode?> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);
    }
}
