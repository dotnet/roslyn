// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseLabeledJumpStatements), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseLabeledJumpStatementsCodeFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseLabeledJumpStatementDiagnosticId];

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_labeled_break_or_continue, nameof(CSharpAnalyzersResources.Use_labeled_break_or_continue), diagnostic);

        return Task.CompletedTask;
    }

    protected override async Task FixAllAsync(
        Document document,
        ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = editor.OriginalRoot;

        // Multiple diagnostics (jumps) can belong to the same loop pattern.  Rewrite each loop only once.
        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var processedLoops);

        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var gotoStatement = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<GotoStatementSyntax>();
            if (gotoStatement is null)
                continue;

            if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(
                    gotoStatement, semanticModel, cancellationToken, out var loop, out var labelDeclaration, out var gotos) &&
                processedLoops.Add(loop))
            {
                ConvertToLabeledBreak(editor, loop, labelDeclaration, gotos);
            }
        }
    }

    private static void ConvertToLabeledBreak(
        SyntaxEditor editor,
        StatementSyntax loop,
        LabeledStatementSyntax labelDeclaration,
        ImmutableArray<GotoStatementSyntax> gotos)
    {
        // Reuse the existing label that sat after the loop as the loop's label.
        var labelName = labelDeclaration.Identifier.Text;

        // 'goto label;' -> 'break label;' for every jump inside the loop.
        var newLoop = loop.ReplaceNodes(gotos, (original, _) => CreateBreak(labelName, original));

        // Move the label onto the loop, keeping them on the same line: 'label: <loop>'.
        var labeledLoop = SyntaxFactory.LabeledStatement(
            SyntaxFactory.Identifier(labelName).WithLeadingTrivia(loop.GetLeadingTrivia()),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space),
            newLoop.WithoutLeadingTrivia());

        editor.ReplaceNode(loop, labeledLoop);

        // The label after the loop is now redundant.  Drop it if it was only a 'goto' landing pad (an empty
        // statement); otherwise just strip the label off the statement it was attached to.
        if (labelDeclaration.Statement is EmptyStatementSyntax)
            editor.RemoveNode(labelDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        else
            editor.ReplaceNode(labelDeclaration, labelDeclaration.Statement.WithLeadingTrivia(labelDeclaration.GetLeadingTrivia()));
    }

#pragma warning disable RSEXPERIMENTAL006 // Labeled break/continue is a preview language feature.
    private static BreakStatementSyntax CreateBreak(string labelName, GotoStatementSyntax gotoStatement)
        => SyntaxFactory.BreakStatement(
            attributeLists: default,
            breakKeyword: SyntaxFactory.Token(SyntaxKind.BreakKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            name: SyntaxFactory.IdentifierName(labelName),
            semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithTriviaFrom(gotoStatement);
#pragma warning restore RSEXPERIMENTAL006
}
