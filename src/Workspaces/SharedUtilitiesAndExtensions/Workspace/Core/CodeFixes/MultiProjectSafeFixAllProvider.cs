// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// A FixAllProvider that can safely fix diagnostics across multiple projects by only applying fixes that are reported
/// in all the projects a linked document is found in.  This way, if a fix is only valid in some projects but not others,
/// it will not be applied during fix-all
/// </summary>
internal abstract class MultiProjectSafeFixAllProvider : FixAllProvider
{
    protected abstract void FixAll(SyntaxEditor editor, IEnumerable<TextSpan> commonSpans);

    public sealed override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
    {
        var cancellationToken = fixAllContext.CancellationToken;

        var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

        // Note: we can only do this if we're doing a fix-all in the solution level.  That's the only way we can see
        // the diagnostics for other linked documents.  If someone is just asking to fix in a project we'll only
        // know about that project and thus can't make the right decision.
        var filterBasedOnScope = fixAllContext.Scope == FixAllScope.Solution;

        // Map from a document to all linked documents it has (not including itself).
        using var _ = PooledDictionary<DocumentId, ImmutableArray<DocumentId>>.GetInstance(out var documentToLinkedDocuments);

        PopulateLinkedDocumentMap();
        var updatedSolution = await ProcessLinkedDocumentMapAsync().ConfigureAwait(false);

        return CodeAction.Create(
            fixAllContext.GetDefaultFixAllTitle(),
            (_, _) => Task.FromResult(updatedSolution),
            equivalenceKey: null,
            CodeActionPriority.Default
#if WORKSPACE
            , this.Cleanup
#endif
            );

        void PopulateLinkedDocumentMap()
        {
            var solution = fixAllContext.Solution;
            foreach (var (document, _) in documentToDiagnostics)
            {
                // Note: GetLinkedDocuments does not return the document it was called on.
                var linkedDocuments = document.GetLinkedDocumentIds();

                // Ignore any linked documents we already saw by processing another document in that linked set.
                if (linkedDocuments.Any(id => documentToLinkedDocuments.ContainsKey(id)))
                    continue;

                documentToLinkedDocuments[document.Id] = linkedDocuments;
            }
        }

        async Task<Solution> ProcessLinkedDocumentMapAsync()
        {
            var currentSolution = fixAllContext.Solution;

            foreach (var (documentId, linkedDocumentIds) in documentToLinkedDocuments)
            {
                // Now, for each group of linked documents, only remove the suppression operators we see in all documents.
                var document = fixAllContext.Solution.GetRequiredDocument(documentId);
                using var _ = PooledHashSet<TextSpan>.GetInstance(out var commonSpans);

                var diagnostics = documentToDiagnostics[document];

                // Start initially with all the spans in this document.
                commonSpans.UnionWith(GetDiagnosticSpans(diagnostics));

                // Now, only keep those spans that are also in all other linked documents.
                if (filterBasedOnScope)
                {
                    foreach (var linkedDocumentId in linkedDocumentIds)
                    {
                        var linkedDocument = fixAllContext.Solution.GetRequiredDocument(linkedDocumentId);
                        var linkedDiagnostics = documentToDiagnostics.TryGetValue(linkedDocument, out var result) ? result : [];

                        commonSpans.IntersectWith(GetDiagnosticSpans(linkedDiagnostics));
                    }
                }

                // Now process the common spans on this initial document.  Note: we don't need to bother updating
                // the linked documents since, by definition, they will get the same changes.  And the workspace
                // will automatically edit all linked files when making a change to only one of them.
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = FixAll(fixAllContext.Solution.Services, root, commonSpans);

                currentSolution = currentSolution.WithDocumentSyntaxRoot(documentId, newRoot);
            }

            return currentSolution;
        }

        static IEnumerable<TextSpan> GetDiagnosticSpans(ImmutableArray<Diagnostic> diagnostics)
            => diagnostics.Select(static d => d.AdditionalLocations[0].SourceSpan);
    }

    private SyntaxNode FixAll(SolutionServices services, SyntaxNode root, PooledHashSet<TextSpan> commonSpans)
    {
        var editor = new SyntaxEditor(root, services);
        FixAll(editor, commonSpans);
        return editor.GetChangedRoot();
    }
}
