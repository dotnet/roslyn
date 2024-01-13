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

namespace Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp), Shared]
    internal class SwitchStatementHighlighter : AbstractKeywordHighlighter<SwitchStatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SwitchStatementHighlighter()
        {
        }

        protected override void AddHighlights(
            SwitchStatementSyntax switchStatement, List<TextSpan> spans, CancellationToken cancellationToken)
        {
            spans.Add(switchStatement.SwitchKeyword.Span);

            foreach (var switchSection in switchStatement.Sections)
            {
                foreach (var label in switchSection.Labels)
                {
                    spans.Add(label.Keyword.Span);
                    spans.Add(EmptySpan(label.ColonToken.Span.End));
                }

                HighlightRelatedKeywords(switchSection, spans, highlightBreaks: true, highlightGotos: true);
            }
        }

        /// <summary>
        /// Finds all breaks and continues that are a child of this node, and adds the appropriate spans to the spans
        /// list.
        /// </summary>
        private static void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans,
            bool highlightBreaks, bool highlightGotos)
        {
            Debug.Assert(highlightBreaks || highlightGotos);

            if (highlightBreaks && node is BreakStatementSyntax breakStatement)
            {
                spans.Add(breakStatement.BreakKeyword.Span);
                spans.Add(EmptySpan(breakStatement.SemicolonToken.Span.End));
            }
            else if (highlightGotos && node is GotoStatementSyntax gotoStatement)
            {
                // We only want to highlight 'goto case' and 'goto default', not plain old goto statements,
                // but if the label is missing, we do highlight 'goto' assuming it's more likely that
                // the user is in the middle of typing 'goto case' or 'goto default'.
                if (gotoStatement.Kind() is SyntaxKind.GotoCaseStatement or SyntaxKind.GotoDefaultStatement ||
                    gotoStatement.Expression.IsMissing)
                {
                    var start = gotoStatement.GotoKeyword.SpanStart;
                    var end = !gotoStatement.CaseOrDefaultKeyword.IsKind(SyntaxKind.None)
                        ? gotoStatement.CaseOrDefaultKeyword.Span.End
                        : gotoStatement.GotoKeyword.Span.End;

                    spans.Add(TextSpan.FromBounds(start, end));
                    spans.Add(EmptySpan(gotoStatement.SemicolonToken.Span.End));
                }
            }
            else
            {
                foreach (var childNodeOrToken in node.ChildNodesAndTokens())
                {
                    if (childNodeOrToken.IsToken)
                        continue;

                    var child = childNodeOrToken.AsNode();
                    var highlightBreaksForChild = highlightBreaks && !child.IsBreakableConstruct();
                    var highlightGotosForChild = highlightGotos && !child.IsKind(SyntaxKind.SwitchStatement);

                    // Only recurse if we have anything to do
                    if (highlightBreaksForChild || highlightGotosForChild)
                    {
                        HighlightRelatedKeywords(child, spans, highlightBreaksForChild, highlightGotosForChild);
                    }
                }
            }
        }
    }
}
