// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseLabeledJumpStatements;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseLabeledJumpStatements), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpUseLabeledJumpStatementsCodeFixProvider()
    : ForkingSyntaxEditorBasedCodeFixProvider<StatementSyntax>
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.UseLabeledJumpStatementDiagnosticId];

    protected override (string title, string equivalenceKey) GetTitleAndEquivalenceKey(CodeFixContext context)
        => (CSharpAnalyzersResources.Use_labeled_jump_statement, nameof(CSharpAnalyzersResources.Use_labeled_jump_statement));

    protected override async Task FixAsync(
        Document document,
        SyntaxEditor editor,
        StatementSyntax diagnosticNode,
        ImmutableDictionary<string, string?> properties,
        CancellationToken cancellationToken)
    {
        // Each invocation fixes one diagnostic's entire pattern (all jumps to the same label / all flag sites).  The
        // forking base re-derives on the tree produced by prior fixes, so nested loops compose, and once a pattern's
        // jumps are converted the remaining diagnostics for it resolve to nodes that no longer exist and are skipped.
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (diagnosticNode is GotoStatementSyntax gotoStatement)
        {
            if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(
                    gotoStatement, semanticModel, cancellationToken, out var loop, out var labelDeclaration, out var gotos))
            {
                ConvertGotos(editor, loop, labelDeclaration, gotos, isBreak: true);
            }
            else if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(
                    gotoStatement, semanticModel, cancellationToken, out loop, out labelDeclaration, out gotos))
            {
                ConvertGotos(editor, loop, labelDeclaration, gotos, isBreak: false);
            }
        }
        else if (diagnosticNode is BreakStatementSyntax breakStatement)
        {
            if (CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPatternFromInnerBreak(
                breakStatement, semanticModel, cancellationToken, out var pattern))
            {
                ConvertFlagPattern(editor, semanticModel, pattern);
            }
        }
    }

    private static void ConvertGotos(
        SyntaxEditor editor,
        StatementSyntax loop,
        LabeledStatementSyntax labelDeclaration,
        ImmutableArray<GotoStatementSyntax> gotos,
        bool isBreak)
    {
        // Reuse the existing label (the goto target's name), or one already on the loop from a prior fix.
        var labelName = GetReusableLabelName(loop) ?? labelDeclaration.Identifier.Text;

        var newLoop = loop.ReplaceNodes(gotos, (original, _) => CreateJump(labelName, original, isBreak));

        if (isBreak)
        {
            ReplaceLoop(editor, loop, labelName, newLoop);

            // The label after the loop is now redundant.  If it sat on an empty statement, remove it entirely;
            // otherwise keep the statement but strip the now-unused label off it.
            if (labelDeclaration.Statement is EmptyStatementSyntax)
                editor.RemoveNode(labelDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
            else
                editor.ReplaceNode(labelDeclaration, labelDeclaration.Statement.WithLeadingTrivia(labelDeclaration.GetLeadingTrivia()));
        }
        else
        {
            // The continue label is the empty last statement of the body; remove it as we relabel the loop.
            var body = (BlockSyntax)newLoop.GetEmbeddedStatement()!;
            newLoop = newLoop.ReplaceNode(body, body.WithStatements(body.Statements.RemoveAt(body.Statements.Count - 1)));

            ReplaceLoop(editor, loop, labelName, newLoop);
        }
    }

    private static void ConvertFlagPattern(SyntaxEditor editor, SemanticModel semanticModel, FlagJumpPattern pattern)
    {
        var loop = pattern.LoopStatement;
        var labelName = GetReusableLabelName(loop) ?? SynthesizeLabelName(semanticModel, loop);

        // Rewrite the inner breaks and delete the now-dead flag assignments and guard inside the loop in one rebuild.
        var newLoop = loop.TrackNodes(pattern.AssignmentAndBreakSites
            .Select(static site => (SyntaxNode)site.Break)
            .Concat(pattern.AssignmentAndBreakSites.Select(static site => (SyntaxNode)site.Assignment))
            .Concat(pattern.GuardStatements.Select(static guard => (SyntaxNode)guard)));

        newLoop = newLoop.ReplaceNodes(
            pattern.AssignmentAndBreakSites.Select(site => newLoop.GetCurrentNode(site.Break)!),
            (original, _) => CreateJump(labelName, original, pattern.IsBreak));

        newLoop = newLoop.RemoveNodes(
            pattern.AssignmentAndBreakSites.Select(site => (SyntaxNode)newLoop.GetCurrentNode(site.Assignment)!)
                .Concat(pattern.GuardStatements.Select(guard => (SyntaxNode)newLoop.GetCurrentNode(guard)!)),
            SyntaxRemoveOptions.KeepNoTrivia)!;

        ReplaceLoop(editor, loop, labelName, newLoop);
        editor.RemoveNode(pattern.LocalDeclarationStatement, SyntaxRemoveOptions.KeepNoTrivia);
    }

    private static string? GetReusableLabelName(StatementSyntax loop)
        => (loop.Parent as LabeledStatementSyntax)?.Identifier.Text;

    /// <summary>
    /// Synthesizes a label for <paramref name="loop"/> (<c>loop_i</c>/<c>loop_x</c> from a for/foreach variable, else
    /// <c>outer</c>), uniquified against the labels in scope at the loop.
    /// </summary>
    private static string SynthesizeLabelName(SemanticModel semanticModel, StatementSyntax loop)
    {
        var baseName = loop switch
        {
            ForStatementSyntax { Declaration.Variables: [var variable, ..] } => "loop_" + variable.Identifier.ValueText,
            ForEachStatementSyntax forEachStatement => "loop_" + forEachStatement.Identifier.ValueText,
            _ => "outer",
        };

        using var _ = PooledHashSet<string>.GetInstance(out var inScope);
        foreach (var label in semanticModel.LookupLabels(loop.SpanStart))
            inScope.Add(label.Name);

        return NameGenerator.GenerateUniqueName(baseName, name => !inScope.Contains(name));
    }

    /// <summary>
    /// Wraps the loop in <c>label: &lt;loop&gt;</c>, unless a prior fix already labeled it (then just updates it).
    /// </summary>
    private static void ReplaceLoop(SyntaxEditor editor, StatementSyntax loop, string labelName, StatementSyntax newLoop)
        => editor.ReplaceNode(loop, loop.Parent is LabeledStatementSyntax ? newLoop : CreateLabeledLoop(labelName, loop, newLoop));

    private static LabeledStatementSyntax CreateLabeledLoop(string labelName, StatementSyntax originalLoop, StatementSyntax newLoop)
        => SyntaxFactory.LabeledStatement(
            SyntaxFactory.Identifier(labelName).WithLeadingTrivia(originalLoop.GetLeadingTrivia()),
            SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space),
            newLoop.WithoutLeadingTrivia());

#pragma warning disable RSEXPERIMENTAL006 // Labeled break/continue is a preview language feature.
    private static StatementSyntax CreateJump(string labelName, SyntaxNode original, bool isBreak)
    {
        var name = SyntaxFactory.IdentifierName(labelName);
        return (isBreak
            ? (StatementSyntax)SyntaxFactory.BreakStatement(name)
            : SyntaxFactory.ContinueStatement(name)).WithTriviaFrom(original);
    }
#pragma warning restore RSEXPERIMENTAL006
}
