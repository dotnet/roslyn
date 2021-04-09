// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable warnings

namespace Analyzer.Utilities
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;

    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string CodeActionTitle { get; }

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            CodeAction? fixAction;
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    fixAction = CodeAction.Create(
                        this.CodeActionTitle,
                        cancellationToken => this.GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Project:
                    fixAction = CodeAction.Create(
                        this.CodeActionTitle,
                        cancellationToken => this.GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken), fixAllContext.Project),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Solution:
                    fixAction = CodeAction.Create(
                        this.CodeActionTitle,
                        cancellationToken => this.GetSolutionFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
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
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
            if (!documentDiagnosticsToFix.TryGetValue(fixAllContext.Document, out var diagnostics))
            {
                return fixAllContext.Document;
            }

            var newRoot = await this.FixAllInDocumentAsync(fixAllContext, fixAllContext.Document, diagnostics).ConfigureAwait(false);
            if (newRoot == null)
            {
                return fixAllContext.Document;
            }

            return fixAllContext.Document.WithSyntaxRoot(newRoot);
        }

        private async Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext, ImmutableArray<Document> documents)
        {
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

            Solution solution = fixAllContext.Solution;
            List<Task<SyntaxNode>> newDocuments = new List<Task<SyntaxNode>>(documents.Length);
            foreach (var document in documents)
            {
                if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
                {
                    newDocuments.Add(document.GetSyntaxRootAsync(fixAllContext.CancellationToken));
                    continue;
                }

                newDocuments.Add(this.FixAllInDocumentAsync(fixAllContext, document, diagnostics));
            }

            for (int i = 0; i < documents.Length; i++)
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
            return this.GetSolutionFixesAsync(fixAllContext, project.Documents.ToImmutableArray());
        }

        private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
        {
            ImmutableArray<Document> documents = fixAllContext.Solution.Projects.SelectMany(i => i.Documents).ToImmutableArray();
            return this.GetSolutionFixesAsync(fixAllContext, documents);
        }
    }
}
