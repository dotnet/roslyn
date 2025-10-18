// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(CodeAction.Create(
            AnalyzersResources.Remove_unnecessary_suppression,
            cancellationToken => FixSingleDocumentAsync(context.Document, context.Diagnostics, cancellationToken),
            nameof(AnalyzersResources.Remove_unnecessary_suppression)),
            context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> FixSingleDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Keep track of the unnecessary spans in all linked documents.  If we're trying to fix something that would
        // cause a problem in another linked document, then we'll still fix it (since the analyzer did report it), we'll
        // just report a warning on it letting the user know in the preview window.
        using var _1 = ArrayBuilder<HashSet<TextSpan>>.GetInstance(out var linkedSpansArray);
        foreach (var linkedDocument in document.GetLinkedDocuments())
        {
            using var _2 = ArrayBuilder<PostfixUnaryExpressionSyntax>.GetInstance(out var unnecessaryNodes);

            var linkedSemanticModel = await linkedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            UnnecessaryNullableWarningSuppressionsUtilities.AddUnnecessaryNodes(
                linkedSemanticModel, unnecessaryNodes, cancellationToken);

            linkedSpansArray.Add([.. unnecessaryNodes.Select(n => n.Span)]);
        }

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        FixAllInDocument(
            editor,
            diagnostics.Select(static d => d.AdditionalLocations[0].SourceSpan),
            expr =>
            {
                // If we don't see this span in any of the linked documents, then that means analyzing that document
                // didn't reveal this suppression as unnecessary.  Therefore, we need to add a warning annotation
                // message to inform the user that removing this suppression may cause warnings in other projects.
                foreach (var linkedSpans in linkedSpansArray)
                {
                    if (!linkedSpans.Contains(expr.Span))
                        return WarningAnnotation.Create(CodeFixesResources.May_be_neccessary_in_other_project_this_document_is_linked_in);
                }

                return null;
            });

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static void FixAllInDocument(
        SyntaxEditor editor,
        IEnumerable<TextSpan> spans,
        Func<PostfixUnaryExpressionSyntax, SyntaxAnnotation?>? getAnnotation)
    {
        var root = editor.OriginalRoot;

        foreach (var span in spans.OrderByDescending(d => d.Start))
        {
            if (root.FindNode(span, getInnermostNodeForTie: true) is PostfixUnaryExpressionSyntax unaryExpression)
            {
                var annotation = getAnnotation?.Invoke(unaryExpression);
                editor.ReplaceNode(
                    unaryExpression,
                    (current, _) =>
                    {
                        var result = ((PostfixUnaryExpressionSyntax)current).Operand.WithTriviaFrom(current);
                        return annotation != null
                            ? result.WithAdditionalAnnotations(annotation)
                            : result;
                    });
            }
        }
    }

    public override FixAllProvider? GetFixAllProvider()
        => new RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider();

    /// <summary>
    /// Fix-all for removing unnecessary `!` operators works in a fairly specialized fashion.  The core problem is that
    /// it's normal to have situations where a `!` operator is unnecessary in one linked document in one project, but
    /// necessary in another.  Consider something as mundane as `string.IsNullOrEmpty(s)`.  In projects that reference a
    /// modern, annotated, BCL, the nullable attributes on this method will allow the compiler to determine that `s` is
    /// non-null after the call, allowing superfluous `!` operators to be removed.  However, in projects that reference
    /// an unannotated BCL, no such determination can be made, and the `!` on a following statement may be necessary.
    ///
    /// To deal with this, we consider all linked documents together.  If a `!` operator is unnecessary in *all* linked
    /// documents, then we can remove it.  Otherwise, we must keep it.
    /// </summary>
    private sealed class RemoveUnnecessaryNullableWarningSuppressionsFixAllProvider : MultiProjectSafeFixAllProvider
    {
#if !CODE_STYLE
        internal override CodeActionCleanup Cleanup => CodeActionCleanup.SyntaxOnly;
#endif

        protected override void FixAll(SyntaxEditor editor, IEnumerable<TextSpan> commonSpans)
            => FixAllInDocument(editor, commonSpans, getAnnotation: null);
    }
}
