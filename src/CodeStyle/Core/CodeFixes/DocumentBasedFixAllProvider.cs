// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string GetCodeActionTitle(FixAllContext fixAllContext);

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            CodeAction fixAction;
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    fixAction = CodeAction.Create(
                        GetCodeActionTitle(fixAllContext),
                        cancellationToken => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(cancellationToken)),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Project:
                    fixAction = CodeAction.Create(
                        GetCodeActionTitle(fixAllContext),
                        cancellationToken => GetProjectFixesAsync(fixAllContext.WithCancellationToken(cancellationToken), fixAllContext.Project),
                        nameof(DocumentBasedFixAllProvider));
                    break;

                case FixAllScope.Solution:
                    fixAction = CodeAction.Create(
                        GetCodeActionTitle(fixAllContext),
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
        /// <returns>New fixed <see cref="Document"/> or original document if no changes were made to the document.</returns>
        protected abstract Task<Document> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics);

        private async Task<Document> GetDocumentFixesAsync(FixAllContext fixAllContext)
        {
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext, progressTrackerOpt: null, (document, cancellationToken) => false).ConfigureAwait(false);
            if (!documentDiagnosticsToFix.TryGetValue(fixAllContext.Document, out var diagnostics))
            {
                return fixAllContext.Document;
            }

            return await FixAllInDocumentAsync(fixAllContext, fixAllContext.Document, diagnostics).ConfigureAwait(false);
        }

        private async Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext, ImmutableArray<Document> documents)
        {
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext, progressTrackerOpt: null, (document, cancellationToken) => false).ConfigureAwait(false);

            var solution = fixAllContext.Solution;
            var newDocuments = new Dictionary<DocumentId, Task<Document>>(documents.Length);
            foreach (var document in documents)
            {
                if (documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
                {
                    newDocuments.Add(document.Id, FixAllInDocumentAsync(fixAllContext, document, diagnostics));
                }
            }

            foreach (var documentIdAndTask in newDocuments)
            {
                var newDocument = await documentIdAndTask.Value.ConfigureAwait(false);
                var newDocumentRoot = await newDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                solution = solution.WithDocumentSyntaxRoot(documentIdAndTask.Key, newDocumentRoot);
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
