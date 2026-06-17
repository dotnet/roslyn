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

        // Several diagnostics (jumps) can belong to the same pattern.  Rewrite each pattern only once, keyed by the
        // loop (goto cases) or the flag declaration (flag case).
        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var processedAnchors);

        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            if (node.FirstAncestorOrSelf<GotoStatementSyntax>() is { } gotoStatement)
            {
                FixGoto(editor, gotoStatement, semanticModel, processedAnchors, cancellationToken);
            }
            else if (diagnostic.AdditionalLocations is [var declarationLocation, ..] &&
                root.FindNode(declarationLocation.SourceSpan, getInnermostNodeForTie: true)
                    .FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is { } declaration)
            {
                FixFlag(editor, declaration, semanticModel, processedAnchors, cancellationToken);
            }
        }
    }

    private static void FixGoto(
        SyntaxEditor editor,
        GotoStatementSyntax gotoStatement,
        SemanticModel semanticModel,
        HashSet<SyntaxNode> processedAnchors,
        CancellationToken cancellationToken)
    {
        if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(
                gotoStatement, semanticModel, cancellationToken, out var loop, out var labelDeclaration, out var gotos))
        {
            if (processedAnchors.Add(loop))
                ConvertToLabeledBreak(editor, loop, labelDeclaration, gotos);
        }
        else if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(
                gotoStatement, semanticModel, cancellationToken, out loop, out labelDeclaration, out gotos))
        {
            if (processedAnchors.Add(loop))
                ConvertToLabeledContinue(editor, loop, labelDeclaration, gotos);
        }
    }

    private static void FixFlag(
        SyntaxEditor editor,
        LocalDeclarationStatementSyntax declaration,
        SemanticModel semanticModel,
        HashSet<SyntaxNode> processedAnchors,
        CancellationToken cancellationToken)
    {
        if (CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPattern(declaration, semanticModel, cancellationToken, out var pattern) &&
            processedAnchors.Add(declaration))
        {
            ConvertFlagPattern(editor, pattern!, CSharpUseLabeledJumpStatementsHelpers.GenerateLabelName(pattern!.Loop));
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

        editor.ReplaceNode(loop, CreateLabeledLoop(labelName, loop, newLoop));

        // The label after the loop is now redundant.  Drop it if it was only a 'goto' landing pad (an empty
        // statement); otherwise just strip the label off the statement it was attached to.
        if (labelDeclaration.Statement is EmptyStatementSyntax)
            editor.RemoveNode(labelDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
        else
            editor.ReplaceNode(labelDeclaration, labelDeclaration.Statement.WithLeadingTrivia(labelDeclaration.GetLeadingTrivia()));
    }

    private static void ConvertToLabeledContinue(
        SyntaxEditor editor,
        StatementSyntax loop,
        LabeledStatementSyntax labelDeclaration,
        ImmutableArray<GotoStatementSyntax> gotos)
    {
        // Reuse the existing trailing label as the loop's label.
        var labelName = labelDeclaration.Identifier.Text;

        // 'goto label;' -> 'continue label;' for every jump inside the loop.
        var newLoop = loop.ReplaceNodes(gotos, (original, _) => CreateContinue(labelName, original));

        // Remove the now-redundant trailing label from the loop body.
        var newBody = (BlockSyntax)CSharpUseLabeledJumpStatementsHelpers.GetLoopBody(newLoop)!;
        newLoop = newLoop.ReplaceNode(
            newBody, newBody.WithStatements(newBody.Statements.RemoveAt(newBody.Statements.Count - 1)));

        editor.ReplaceNode(loop, CreateLabeledLoop(labelName, loop, newLoop));
    }

    private static void ConvertFlagPattern(SyntaxEditor editor, FlagJumpPattern pattern, string labelName)
    {
        var loop = pattern.Loop;

        // Everything inside the loop (the inner breaks and the guard) is rewritten in a single rebuild so the loop can
        // then be wrapped in the label with one editor edit.  The flag declaration lives outside the loop and is
        // removed separately.
        var nodesToTrack = new List<SyntaxNode>(pattern.Sites.Length * 2 + 1);
        foreach (var (assignment, innerBreak) in pattern.Sites)
        {
            nodesToTrack.Add(assignment);
            nodesToTrack.Add(innerBreak);
        }

        nodesToTrack.Add(pattern.Guard);

        var newLoop = loop.TrackNodes(nodesToTrack);

        // 'break;' -> 'break label;' / 'continue label;'.
        newLoop = newLoop.ReplaceNodes(
            pattern.Sites.Select(site => newLoop.GetCurrentNode(site.Break)!),
            (original, _) => pattern.IsBreak
                ? CreateBreak(labelName, original)
                : CreateContinue(labelName, original));

        // Delete the 'flag = true;' assignments and the 'if (flag) ...;' guard.
        var nodesToRemove = pattern.Sites
            .Select(site => (SyntaxNode)newLoop.GetCurrentNode(site.Assignment)!)
            .Append(newLoop.GetCurrentNode(pattern.Guard)!);
        newLoop = newLoop.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia)!;

        editor.ReplaceNode(loop, CreateLabeledLoop(labelName, loop, newLoop));
        editor.RemoveNode(pattern.Declaration, SyntaxRemoveOptions.KeepNoTrivia);
    }

    /// <summary>
    /// Wraps <paramref name="newLoop"/> in <c>label: &lt;loop&gt;</c>, keeping the label on the same line as the loop
    /// and inheriting <paramref name="originalLoop"/>'s leading trivia (indentation).
    /// </summary>
    private static LabeledStatementSyntax CreateLabeledLoop(string labelName, StatementSyntax originalLoop, StatementSyntax newLoop)
        => SyntaxFactory.LabeledStatement(
            SyntaxFactory.Identifier(labelName).WithLeadingTrivia(originalLoop.GetLeadingTrivia()),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space),
            newLoop.WithoutLeadingTrivia());

#pragma warning disable RSEXPERIMENTAL006 // Labeled break/continue is a preview language feature.
    private static BreakStatementSyntax CreateBreak(string labelName, SyntaxNode original)
        => SyntaxFactory.BreakStatement(
            attributeLists: default,
            breakKeyword: SyntaxFactory.Token(SyntaxKind.BreakKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            name: SyntaxFactory.IdentifierName(labelName),
            semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithTriviaFrom(original);

    private static ContinueStatementSyntax CreateContinue(string labelName, SyntaxNode original)
        => SyntaxFactory.ContinueStatement(
            attributeLists: default,
            continueKeyword: SyntaxFactory.Token(SyntaxKind.ContinueKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            name: SyntaxFactory.IdentifierName(labelName),
            semicolonToken: SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            .WithTriviaFrom(original);
#pragma warning restore RSEXPERIMENTAL006
}
