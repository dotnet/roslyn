// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Highlighting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.KeywordHighlighting.KeywordHighlighters;

[ExportHighlighter(LanguageNames.CSharp), Shared]
internal sealed class ReturnStatementHighlighter : AbstractKeywordHighlighter<ReturnStatementSyntax>
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ReturnStatementHighlighter()
    {
    }

    protected override void AddHighlights(
        ReturnStatementSyntax returnStatement, List<TextSpan> spans, CancellationToken cancellationToken)
    {
        var parent = returnStatement
                         .GetAncestorsOrThis<SyntaxNode>()
                         .FirstOrDefault(n => n.IsReturnableConstructOrTopLevelCompilationUnit());

        if (parent == null)
        {
            return;
        }

        HighlightRelatedKeywords(parent, spans);
    }

    /// <summary>
    /// Finds all returns that are children of this node, and adds the appropriate spans to the spans list.
    /// </summary>
    private static void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
    {
        switch (node)
        {
            case ReturnStatementSyntax statement:
                spans.Add(statement.ReturnKeyword.Span);
                spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
                break;
            default:
                foreach (var child in node.ChildNodesAndTokens())
                {
                    if (child.IsToken)
                        continue;

                    // Only recurse if we have anything to do
                    if (!child.AsNode().IsReturnableConstruct())
                        HighlightRelatedKeywords(child.AsNode(), spans);
                }

                break;
        }
    }
}
