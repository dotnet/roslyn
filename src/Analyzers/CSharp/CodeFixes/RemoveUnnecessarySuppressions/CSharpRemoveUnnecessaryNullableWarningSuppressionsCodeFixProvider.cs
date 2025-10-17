// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider()
    : CodeFixProvider
    // : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        RegisterCodeFix(context, AnalyzersResources.Remove_unnecessary_suppression, nameof(AnalyzersResources.Remove_unnecessary_suppression));
        return Task.CompletedTask;
    }

    protected override Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics.OrderByDescending(d => d.Location.SourceSpan.Start))
        {
            if (diagnostic.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken) is PostfixUnaryExpressionSyntax unaryExpression)
            {
                editor.ReplaceNode(
                    unaryExpression,
                    (current, _) => ((PostfixUnaryExpressionSyntax)current).Operand.WithTriviaFrom(current));
            }
        }

        return Task.CompletedTask;
    }

    public override FixAllProvider? GetFixAllProvider()
        => new RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider();

    private sealed class RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider : FixAllProvider
    {
#if !CODE_STYLE
        internal override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;
#endif

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            // Fix-all for removing unnecessary `!` operators works in a fairly specialized fashion.  The core problem
            // is that it's normal to have situations where a `!` operator is unnecessary in one linked document in one
            // project, but necessary in another.  Consider something as mundane as `string.IsNullOrEmpty(s)`.  In
            // projects that reference a modern, annotated, BCL, the nullable attributes on this method will allow the
            // compiler to determine that `s` is non-null after the call, allowing superfluous `!` operators to be
            // removed.  However, in projects that reference an unannotated BCL, no such determination can be made, and
            // the `!` on a following statement may be necessary.
            //
            // To deal with this, we consider all linked documents together.  If a `!` operator is unnecessary in *all*
            // linked documents, then we can remove it.  Otherwise, we must keep it.

            var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);

            // Map from a document to all linked documents (not including itself).
            using var _ = PooledDictionary<DocumentId, ImmutableArray<DocumentId>>.GetInstance(out var documentToLinkedDocuments);
            try
            {
                PopulateLinkedDocumentMap();
                var updatedSolution = ProcessLinkedDocumentMap();

                return CodeAction.Create(
                    fixAllContext.GetDefaultFixAllTitle(),
                    (_, _) => Task.FromResult(updatedSolution),
                    equivalenceKey: null,
                    CodeActionPriority.Default
#if !CODE_STYLE
                    , CodeActionCleanup.SyntaxOnly
#endif
                    );
            }
            finally
            {
                foreach (var (_, set) in documentToLinkedDocuments)
                    set.Free();
            }

            Solution ProcessLinkedDocumentMap()
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
                    foreach (var linkedDocumentId in linkedDocumentIds)
                    {
                        var linkedDocument = fixAllContext.Solution.GetRequiredDocument(linkedDocumentId);
                        var linkedDiagnostics = documentToDiagnostics.TryGetValue(linkedDocument, out var result) ? result : [];

                        commonSpans.IntersectWith(GetDiagnosticSpans(linkedDiagnostics));
                    }


                }

                return currentSolution;

                static IEnumerable<TextSpan> GetDiagnosticSpans(ImmutableArray<Diagnostic> diagnostics)
                    => diagnostics.Select(static d => d.AdditionalLocations[0].SourceSpan);
            }

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
        }
    }
}
