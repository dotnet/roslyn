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
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_labeled_jump_statement, nameof(CSharpAnalyzersResources.Use_labeled_jump_statement), diagnostic);

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

        // A loop/switch can be the target of several jumps -- even a mix of labeled break and labeled continue, or a
        // goto pattern plus a flag pattern.  The harness also applies fixes one diagnostic at a time, so each fix must
        // be self-contained and order-independent.  We therefore key off the targeted construct: the first time we see
        // it, we re-derive *all* of its jumps from the construct itself and relabel it exactly once.
        using var _ = PooledHashSet<SyntaxNode>.GetInstance(out var processedLoops);

        foreach (var diagnostic in diagnostics)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryGetTargetLoop(root, diagnostic, semanticModel, cancellationToken, out var loop) &&
                processedLoops.Add(loop))
            {
                ApplyRewrite(editor, loop, CollectLoopRewrite(loop, semanticModel, cancellationToken));
            }
        }
    }

    private static bool TryGetTargetLoop(
        SyntaxNode root,
        Diagnostic diagnostic,
        SemanticModel semanticModel,
        CancellationToken cancellationToken,
        out StatementSyntax loop)
    {
        var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (node.FirstAncestorOrSelf<GotoStatementSyntax>() is { } gotoStatement)
        {
            return CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(gotoStatement, semanticModel, cancellationToken, out loop, out _, out _)
                || CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(gotoStatement, semanticModel, cancellationToken, out loop, out _, out _);
        }

        if (diagnostic.AdditionalLocations is [var declarationLocation, ..] &&
            root.FindNode(declarationLocation.SourceSpan, getInnermostNodeForTie: true)
                .FirstAncestorOrSelf<LocalDeclarationStatementSyntax>() is { } declaration &&
            CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPattern(declaration, semanticModel, cancellationToken, out var pattern))
        {
            loop = pattern.Loop;
            return true;
        }

        loop = null!;
        return false;
    }

    /// <summary>
    /// Re-derives every jump that targets <paramref name="loop"/> (labeled break, labeled continue, and flag
    /// patterns), so the construct can be relabeled in one self-contained, order-independent edit.
    /// </summary>
    private static LoopRewrite CollectLoopRewrite(StatementSyntax loop, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var rewrite = new LoopRewrite();
        using var _1 = PooledHashSet<SyntaxNode>.GetInstance(out var seenAnchors);

        foreach (var gotoStatement in loop.DescendantNodes().OfType<GotoStatementSyntax>())
        {
            if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoBreakPattern(
                    gotoStatement, semanticModel, cancellationToken, out var breakLoop, out var breakLabel, out var breakGotos) &&
                breakLoop == loop &&
                seenAnchors.Add(breakLabel))
            {
                foreach (var jump in breakGotos)
                    rewrite.Jumps.Add((jump, IsBreak: true));

                rewrite.OuterLabels.Add(breakLabel);
                rewrite.ReuseNames.Add((breakLabel.Identifier.Text, breakLabel.SpanStart));
            }
            else if (CSharpUseLabeledJumpStatementsHelpers.TryGetGotoContinuePattern(
                    gotoStatement, semanticModel, cancellationToken, out var continueLoop, out var continueLabel, out var continueGotos) &&
                continueLoop == loop &&
                seenAnchors.Add(continueLabel))
            {
                foreach (var jump in continueGotos)
                    rewrite.Jumps.Add((jump, IsBreak: false));

                rewrite.InnerRemovals.Add(continueLabel);
                rewrite.ReuseNames.Add((continueLabel.Identifier.Text, continueLabel.SpanStart));
            }
        }

        foreach (var breakStatement in loop.DescendantNodes().OfType<BreakStatementSyntax>())
        {
            if (CSharpUseLabeledJumpStatementsHelpers.TryGetFlagPatternFromInnerBreak(
                    breakStatement, semanticModel, cancellationToken, out var pattern) &&
                pattern.Loop == loop &&
                seenAnchors.Add(pattern.Declaration))
            {
                foreach (var (assignment, innerBreak) in pattern.Sites)
                {
                    rewrite.Jumps.Add((innerBreak, pattern.IsBreak));
                    rewrite.InnerRemovals.Add(assignment);
                }

                rewrite.InnerRemovals.Add(pattern.Guard);
                rewrite.OuterRemovals.Add(pattern.Declaration);
            }
        }

        return rewrite;
    }

    private static void ApplyRewrite(SyntaxEditor editor, StatementSyntax loop, LoopRewrite rewrite)
    {
        // If the loop is already labeled (the user wrote a label), reuse it.  Otherwise reuse the lexically-first
        // existing label among the patterns being merged, or synthesize a name (flag-only case).
        var existingLabel = loop.Parent as LabeledStatementSyntax;
        var labelName = existingLabel?.Identifier.Text ?? rewrite.GetLabelName(loop);

        // Rebuild the loop in one shot: rewrite every inner jump and delete every inner piece of now-dead code.
        var newLoop = loop.TrackNodes(rewrite.Jumps.Select(j => j.Jump).Concat(rewrite.InnerRemovals));

        var isBreakByCurrentNode = rewrite.Jumps.ToDictionary(j => newLoop.GetCurrentNode(j.Jump)!, j => j.IsBreak);
        newLoop = newLoop.ReplaceNodes(
            isBreakByCurrentNode.Keys,
            (original, _) => isBreakByCurrentNode[original]
                ? CreateBreak(labelName, original)
                : CreateContinue(labelName, original));

        newLoop = newLoop.RemoveNodes(
            rewrite.InnerRemovals.Select(n => newLoop.GetCurrentNode(n)!), SyntaxRemoveOptions.KeepNoTrivia)!;

        // Wrap the loop in the shared label, unless it already carries one (then just update it in place).
        editor.ReplaceNode(loop, existingLabel is null ? CreateLabeledLoop(labelName, loop, newLoop) : newLoop);

        // Outer cleanup: the (now redundant) label(s) that sat after the loop, and any flag declarations before it.
        foreach (var labelDeclaration in rewrite.OuterLabels)
        {
            if (labelDeclaration.Statement is EmptyStatementSyntax)
                editor.RemoveNode(labelDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
            else
                editor.ReplaceNode(labelDeclaration, labelDeclaration.Statement.WithLeadingTrivia(labelDeclaration.GetLeadingTrivia()));
        }

        foreach (var declaration in rewrite.OuterRemovals)
            editor.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);
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
        => SyntaxFactory.BreakStatement(SyntaxFactory.IdentifierName(labelName)).WithTriviaFrom(original);

    private static ContinueStatementSyntax CreateContinue(string labelName, SyntaxNode original)
        => SyntaxFactory.ContinueStatement(SyntaxFactory.IdentifierName(labelName)).WithTriviaFrom(original);
#pragma warning restore RSEXPERIMENTAL006

    /// <summary>
    /// Accumulates everything that must happen to a single loop/switch so it is relabeled exactly once.
    /// </summary>
    private sealed class LoopRewrite
    {
        /// <summary>Jumps (gotos / inner flag breaks) inside the loop to rewrite, and whether each becomes a break.</summary>
        public readonly List<(SyntaxNode Jump, bool IsBreak)> Jumps = [];

        /// <summary>Dead code inside the loop to delete (flag assignments/guards, the trailing continue label).</summary>
        public readonly List<SyntaxNode> InnerRemovals = [];

        /// <summary>Labels that sat after the loop (removed if an empty pad, otherwise just un-labeled).</summary>
        public readonly List<LabeledStatementSyntax> OuterLabels = [];

        /// <summary>Dead code before the loop to delete (flag declarations).</summary>
        public readonly List<SyntaxNode> OuterRemovals = [];

        /// <summary>Existing label names (and their source positions) that may be reused for the loop.</summary>
        public readonly List<(string Name, int Position)> ReuseNames = [];

        /// <summary>
        /// Reuses the lexically-first existing label, or synthesizes a name when the loop had no labels (flag case).
        /// </summary>
        public string GetLabelName(StatementSyntax loop)
            => ReuseNames.Count > 0
                ? ReuseNames.OrderBy(n => n.Position).First().Name
                : CSharpUseLabeledJumpStatementsHelpers.GenerateLabelName(loop);
    }
}
