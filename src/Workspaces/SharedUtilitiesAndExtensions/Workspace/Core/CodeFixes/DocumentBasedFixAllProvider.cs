// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string CodeActionTitle { get; }

        protected virtual string GetCodeActionTitle(FixAllContext context)
            => CodeActionTitle;

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var title = GetCodeActionTitle(fixAllContext);
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Project:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetProjectFixesAsync(fixAllContext.WithCancellationToken(c), fixAllContext.Project),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Solution:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Custom:
                default:
                    return Task.FromResult<CodeAction?>(null);
            }
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

        private async Task<Document> GetDocumentFixesAsync(FixAllContext fixAllContext)
        {
            RoslynDebug.AssertNotNull(fixAllContext.Document);

            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
            if (!documentDiagnosticsToFix.TryGetValue(fixAllContext.Document, out var diagnostics))
                return fixAllContext.Document;

            var newRoot = await FixAllInDocumentAsync(fixAllContext, fixAllContext.Document, diagnostics).ConfigureAwait(false);
            if (newRoot == null)
                return fixAllContext.Document;

            return fixAllContext.Document.WithSyntaxRoot(newRoot);
        }

        private Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext, Project project)
            => FixAllContextHelper.FixAllInSolutionAsync(fixAllContext, ImmutableArray.Create(project.Id), GetFixAllInDocumentFunction());

        private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
            => FixAllContextHelper.FixAllInSolutionAsync(fixAllContext, GetFixAllInDocumentFunction());

        private Func<FixAllContext, Document, ImmutableArray<Diagnostic>, Task<Document?>> GetFixAllInDocumentFunction()
        {
            return async (context, document, diagnostics) =>
            {
                var newRoot = await this.FixAllInDocumentAsync(context, document, diagnostics).ConfigureAwait(false);
                if (newRoot == null)
                    return null;

                return document.WithSyntaxRoot(newRoot);
            };
        }
    }
}
