// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;

[ExportHighlighter(LanguageNames.CSharp), Shared]
internal class LoopHighlighter : AbstractKeywordHighlighter
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public LoopHighlighter()
    {
    }

    protected override bool IsHighlightableNode(SyntaxNode node)
        => node.IsContinuableConstruct();

    protected override void AddHighlightsForNode(
        SyntaxNode node, List<TextSpan> spans, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case DoStatementSyntax doStatement:
                HighlightDoStatement(doStatement, spans);
                break;
            case ForStatementSyntax forStatement:
                HighlightForStatement(forStatement, spans);
                break;
            case CommonForEachStatementSyntax forEachStatement:
                HighlightForEachStatement(forEachStatement, spans);
                break;
            case WhileStatementSyntax whileStatement:
                HighlightWhileStatement(whileStatement, spans);
                break;
        }

        HighlightRelatedKeywords(node, spans, highlightBreaks: true, highlightContinues: true);
    }

    private static void HighlightDoStatement(DoStatementSyntax statement, List<TextSpan> spans)
    {
        spans.Add(statement.DoKeyword.Span);
        spans.Add(statement.WhileKeyword.Span);
        spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
    }

    private static void HighlightForStatement(ForStatementSyntax statement, List<TextSpan> spans)
        => spans.Add(statement.ForKeyword.Span);

    private static void HighlightForEachStatement(CommonForEachStatementSyntax statement, List<TextSpan> spans)
        => spans.Add(statement.ForEachKeyword.Span);

    private static void HighlightWhileStatement(WhileStatementSyntax statement, List<TextSpan> spans)
        => spans.Add(statement.WhileKeyword.Span);

    /// <summary>
    /// Finds all breaks and continues that are a child of this node, and adds the appropriate spans to the spans list.
    /// </summary>
    private static void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans,
        bool highlightBreaks, bool highlightContinues)
    {
        Debug.Assert(highlightBreaks || highlightContinues);

        if (highlightBreaks && node is BreakStatementSyntax breakStatement)
        {
            spans.Add(breakStatement.BreakKeyword.Span);
            spans.Add(EmptySpan(breakStatement.SemicolonToken.Span.End));
        }
        else if (highlightContinues && node is ContinueStatementSyntax continueStatement)
        {
            spans.Add(continueStatement.ContinueKeyword.Span);
            spans.Add(EmptySpan(continueStatement.SemicolonToken.Span.End));
        }
        else
        {
            foreach (var child in node.ChildNodes())
            {
                var highlightBreaksForChild = highlightBreaks && !child.IsBreakableConstruct();
                var highlightContinuesForChild = highlightContinues && !child.IsContinuableConstruct();

                // Only recurse if we have anything to do
                if (highlightBreaksForChild || highlightContinuesForChild)
                {
                    HighlightRelatedKeywords(child, spans, highlightBreaksForChild, highlightContinuesForChild);
                }
            }
        }
    }
}
