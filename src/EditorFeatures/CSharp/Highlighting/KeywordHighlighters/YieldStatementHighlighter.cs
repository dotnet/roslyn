// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.KeywordHighlighting.KeywordHighlighters
{
    [ExportHighlighter(LanguageNames.CSharp)]
    internal class YieldStatementHighlighter : AbstractKeywordHighlighter<YieldStatementSyntax>
    {
        [ImportingConstructor]
        public YieldStatementHighlighter()
        {
        }

        protected override IEnumerable<TextSpan> GetHighlights(
            YieldStatementSyntax yieldStatement, CancellationToken cancellationToken)
        {
            var parent = yieldStatement
                             .GetAncestorsOrThis<SyntaxNode>()
                             .FirstOrDefault(n => n.IsReturnableConstruct());

            if (parent == null)
            {
                return SpecializedCollections.EmptyEnumerable<TextSpan>();
            }

            var spans = new List<TextSpan>();

            HighlightRelatedKeywords(parent, spans);

            return spans;
        }

        /// <summary>
        /// Finds all returns that are children of this node, and adds the appropriate spans to the spans list.
        /// </summary>
        private void HighlightRelatedKeywords(SyntaxNode node, List<TextSpan> spans)
        {
            switch (node)
            {
                case YieldStatementSyntax statement:
                    spans.Add(
                        TextSpan.FromBounds(
                            statement.YieldKeyword.SpanStart,
                            statement.ReturnOrBreakKeyword.Span.End));

                    spans.Add(EmptySpan(statement.SemicolonToken.Span.End));
                    break;
                default:
                    foreach (var child in node.ChildNodes())
                    {
                        // Only recurse if we have anything to do
                        if (!child.IsReturnableConstruct())
                        {
                            HighlightRelatedKeywords(child, spans);
                        }
                    }
                    break;
            }
        }
    }
}
