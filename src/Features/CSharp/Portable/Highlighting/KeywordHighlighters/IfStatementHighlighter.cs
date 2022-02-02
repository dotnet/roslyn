// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.KeywordHighlighting
{
    [ExportHighlighter(LanguageNames.CSharp), Shared]
    internal class IfStatementHighlighter : AbstractKeywordHighlighter<IfStatementSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public IfStatementHighlighter()
        {
        }

        protected override void AddHighlights(
            IfStatementSyntax ifStatement, List<TextSpan> highlights, CancellationToken cancellationToken)
        {
            if (ifStatement.Parent.Kind() != SyntaxKind.ElseClause)
            {
                ComputeSpans(ifStatement, highlights);
            }
        }

        private static void ComputeSpans(
            IfStatementSyntax ifStatement, List<TextSpan> highlights)
        {
            highlights.Add(ifStatement.IfKeyword.Span);

            // Loop to get all the else if parts
            while (ifStatement != null && ifStatement.Else != null)
            {
                // Check for 'else if' scenario' (the statement in the else clause is an if statement)
                var elseKeyword = ifStatement.Else.ElseKeyword;

                if (ifStatement.Else.Statement is IfStatementSyntax elseIfStatement)
                {
                    if (OnlySpacesBetween(elseKeyword, elseIfStatement.IfKeyword))
                    {
                        // Highlight both else and if tokens if they are on the same line
                        highlights.Add(TextSpan.FromBounds(
                            elseKeyword.SpanStart,
                            elseIfStatement.IfKeyword.Span.End));
                    }
                    else
                    {
                        // Highlight the else and if tokens separately
                        highlights.Add(elseKeyword.Span);
                        highlights.Add(elseIfStatement.IfKeyword.Span);
                    }

                    // Continue the enumeration looking for more else blocks
                    ifStatement = elseIfStatement;
                }
                else
                {
                    // Highlight just the else and we're done
                    highlights.Add(elseKeyword.Span);
                    break;
                }
            }
        }

        public static bool OnlySpacesBetween(SyntaxToken first, SyntaxToken second)
        {
            return first.TrailingTrivia.AsString().All(c => c == ' ') &&
                   second.LeadingTrivia.AsString().All(c => c == ' ');
        }
    }
}
