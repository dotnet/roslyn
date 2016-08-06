// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class LoopHighlighter : AbstractKeywordHighlighter<SyntaxNode>
    {
        protected override IEnumerable<TextSpan> GetHighlights(
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var loop = node.GetAncestorsOrThis<SyntaxNode>()
                           .FirstOrDefault(ancestor => ancestor.IsContinuableConstruct());

            if (loop != null)
            {
                return KeywordHighlightsForLoop(loop);
            }

            return SpecializedCollections.EmptyEnumerable<TextSpan>();
        }

        private IEnumerable<TextSpan> KeywordHighlightsForLoop(SyntaxNode loopNode)
        {
            var spans = new List<TextSpan>();

            switch (loopNode.Kind())
            {
                case SyntaxKind.DoStatement:
                    HighlightDoStatement((DoStatementSyntax)loopNode, spans);
                    break;
                case SyntaxKind.ForStatement:
                    HighlightForStatement((ForStatementSyntax)loopNode, spans);
                    break;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachComponentStatement:
                    HighlightForEachStatement((CommonForEachStatementSyntax)loopNode, spans);
                    break;

                case SyntaxKind.WhileStatement:
                    HighlightWhileStatement((WhileStatementSyntax)loopNode, spans);
                    break;
            }

            HighlightRelatedKeywords(loopNode, spans, true, true);

            return spans;
        }

        private void HighlightDoStatement(DoStatementSyntax statement, List<TextSpan> spans)
        {
            spans.Add(statement.DoKeyword.Span);
            spans.Add(statement.WhileKeyword.Span);
            spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
        }

        private void HighlightForStatement(ForStatementSyntax statement, List<TextSpan> spans)
        {
            spans.Add(statement.ForKeyword.Span);
        }

        private void HighlightForEachStatement(CommonForEachStatementSyntax statement, List<TextSpan> spans)
        {
            spans.Add(statement.ForEachKeyword.Span);
        }

        private void HighlightWhileStatement(WhileStatementSyntax statement, List<TextSpan> spans)
        {
            spans.Add(statement.WhileKeyword.Span);
        }

        /// <summary>
        /// Finds all breaks and continues that are a child of this node, and adds the appropriate spans to the spans list.
        /// </summary>
        private void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans, bool highlightBreaks, bool highlightContinues)
        {
            Debug.Assert(highlightBreaks || highlightContinues);

            if (highlightBreaks && node is BreakStatementSyntax)
            {
                var statement = (BreakStatementSyntax)node;
                spans.Add(statement.BreakKeyword.Span);
                spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
            }
            else if (highlightContinues && node is ContinueStatementSyntax)
            {
                var statement = (ContinueStatementSyntax)node;
                spans.Add(statement.ContinueKeyword.Span);
                spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
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
}
