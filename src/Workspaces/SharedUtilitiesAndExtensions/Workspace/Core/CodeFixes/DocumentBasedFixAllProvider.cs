// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

#pragma warning disable RS0005 // Do not use generic CodeAction.Create to create CodeAction

namespace Microsoft.CodeAnalysis.CodeStyle
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string CodeActionTitle
        {
            get;
        }

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            CodeAction fixAction;
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    fixAction = CodeAction.Create(
                        CodeActionTitle,
                        cancellationToken => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Project:
                    fixAction = CodeAction.Create(
                        CodeActionTitle,
                        cancellationToken => GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken), fixAllContext.Project),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Solution:
                    fixAction = CodeAction.Create(
                        CodeActionTitle,
                        cancellationToken => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Custom:
                default:
                    fixAction = null;
                    break;
            }

            return Task.FromResult(fixAction);
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
        protected abstract Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);

        private async Task<Document> GetDocumentFixesAsync(FixAllContext fixAllContext)
        {
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext, progressTrackerOpt: null).ConfigureAwait(false);
            if (!documentDiagnosticsToFix.TryGetValue(fixAllContext.Document, out var diagnostics))
            {
                return fixAllContext.Document;
            }

            var newRoot = await FixAllInDocumentAsync(fixAllContext, fixAllContext.Document, diagnostics).ConfigureAwait(false);
            if (newRoot == null)
            {
                return fixAllContext.Document;
            }

            return fixAllContext.Document.WithSyntaxRoot(newRoot);
        }

        private async Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext, ImmutableArray<Document> documents)
        {
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext, progressTrackerOpt: null).ConfigureAwait(false);

            var solution = fixAllContext.Solution;
            var newDocuments = new List<Task<SyntaxNode>>(documents.Length);
            foreach (var document in documents)
            {
                if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
                {
                    newDocuments.Add(document.GetSyntaxRootAsync(fixAllContext.CancellationToken));
                    continue;
                }

                newDocuments.Add(FixAllInDocumentAsync(fixAllContext, document, diagnostics));
            }

            for (var i = 0; i < documents.Length; i++)
            {
                var newDocumentRoot = await newDocuments[i].ConfigureAwait(false);
                if (newDocumentRoot == null)
                {
                    continue;
                }

                solution = solution.WithDocumentSyntaxRoot(documents[i].Id, newDocumentRoot);
            }

            return solution;
        }

        private Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext, Project project)
        {
            return GetSolutionFixesAsync(fixAllContext, project.Documents.ToImmutableArray());
        }

        private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
        {
            var documents = fixAllContext.Solution.Projects.SelectMany(i => i.Documents).ToImmutableArray();
            return GetSolutionFixesAsync(fixAllContext, documents);
        }
    }
}
