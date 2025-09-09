// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.NewLines.ConsecutiveStatementPlacement;

internal abstract class AbstractConsecutiveStatementPlacementDiagnosticAnalyzer<TExecutableStatementSyntax>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TExecutableStatementSyntax : SyntaxNode
{
    private readonly ISyntaxFacts _syntaxFacts;

    protected AbstractConsecutiveStatementPlacementDiagnosticAnalyzer(ISyntaxFacts syntaxFacts)
        : base(IDEDiagnosticIds.ConsecutiveStatementPlacementDiagnosticId,
               EnforceOnBuildValues.ConsecutiveStatementPlacement,
               CodeStyleOptions2.AllowStatementImmediatelyAfterBlock,
               new LocalizableResourceString(
                   nameof(AnalyzersResources.Blank_line_required_between_block_and_subsequent_statement), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
        _syntaxFacts = syntaxFacts;
    }

    protected abstract bool IsBlockLikeStatement(SyntaxNode node);
    protected abstract Location GetDiagnosticLocation(SyntaxNode block);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
            context.RegisterSyntaxTreeAction(treeContext => AnalyzeTree(treeContext, context.Compilation.Options)));

    private void AnalyzeTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
    {
        var option = context.GetAnalyzerOptions().AllowStatementImmediatelyAfterBlock;
        if (option.Value || ShouldSkipAnalysis(context, compilationOptions, option.Notification))
            return;

        Recurse(context, option.Notification, context.GetAnalysisRoot(findInTrivia: false), context.CancellationToken);
    }

    private void Recurse(SyntaxTreeAnalysisContext context, NotificationOption2 notificationOption, SyntaxNode node, CancellationToken cancellationToken)
    {
        if (node.ContainsDiagnostics && node.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return;

        if (IsBlockLikeStatement(node))
            ProcessBlockLikeStatement(context, notificationOption, node);

        foreach (var child in node.ChildNodesAndTokens())
        {
            if (!context.ShouldAnalyzeSpan(child.FullSpan))
                continue;

            if (child.AsNode(out var childNode))
                Recurse(context, notificationOption, childNode, cancellationToken);
        }
    }

    private void ProcessBlockLikeStatement(SyntaxTreeAnalysisContext context, NotificationOption2 notificationOption, SyntaxNode block)
    {
        // Don't examine broken blocks.
        var endToken = block.GetLastToken();
        if (endToken.IsMissing)
            return;

        // If the close brace itself doesn't have a newline, then ignore this.  This is a case of series of
        // statements on the same line.
        if (!endToken.TrailingTrivia.Any())
            return;

        if (!_syntaxFacts.IsEndOfLineTrivia(endToken.TrailingTrivia.Last()))
            return;

        // Grab whatever comes after the close brace.  If it's not the start of a statement, ignore it.
        var nextToken = endToken.GetNextToken();
        var nextTokenContainingStatement = nextToken.Parent?.FirstAncestorOrSelf<TExecutableStatementSyntax>();
        if (nextTokenContainingStatement == null)
            return;

        if (nextToken != nextTokenContainingStatement.GetFirstToken())
            return;

        // There has to be at least a blank line between the end of the block and the start of the next statement.

        foreach (var trivia in nextToken.LeadingTrivia)
        {
            // If there's a blank line between the brace and the next token, we're all set.
            if (_syntaxFacts.IsEndOfLineTrivia(trivia))
                return;

            if (_syntaxFacts.IsWhitespaceTrivia(trivia))
                continue;

            // got something that wasn't whitespace.  Bail out as we don't want to place any restrictions on this code.
            return;
        }

        context.ReportDiagnostic(DiagnosticHelper.Create(
            this.Descriptor,
            GetDiagnosticLocation(block),
            notificationOption,
            context.Options,
            additionalLocations: [nextToken.GetLocation()],
            properties: null));
    }
}
