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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryNullableWarningSuppressions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider() : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression];

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        context.RegisterCodeFix(CodeAction.Create(
            AnalyzersResources.Remove_unnecessary_suppression,
            cancellationToken => FixAllAsync(context.Document, context.Diagnostics, cancellationToken),
            nameof(AnalyzersResources.Remove_unnecessary_suppression)),
            context.Diagnostics);

        return Task.CompletedTask;
    }

    private static async Task<Document> FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var editor = new SyntaxEditor(root, document.Project.Solution.Services);

        FixAll(editor, diagnostics.Select(static d => d.AdditionalLocations[0].SourceSpan));

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static void FixAll(
        SyntaxEditor editor,
        IEnumerable<TextSpan> spans)
    {
        var root = editor.OriginalRoot;

        foreach (var span in spans.OrderByDescending(d => d.Start))
        {
            if (root.FindNode(span, getInnermostNodeForTie: true) is PostfixUnaryExpressionSyntax unaryExpression)
            {
                editor.ReplaceNode(
                    unaryExpression,
                    (current, _) => ((PostfixUnaryExpressionSyntax)current).Operand.WithTriviaFrom(current));
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
            => CSharpRemoveUnnecessaryNullableWarningSuppressionsCodeFixProvider.FixAll(editor, commonSpans);
    }
}
