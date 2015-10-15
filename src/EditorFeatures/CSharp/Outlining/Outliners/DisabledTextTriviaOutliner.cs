// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Outlining;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Outlining
{
    internal class DisabledTextTriviaOutliner : AbstractSyntaxTriviaOutliner
    {
        public override void CollectOutliningSpans(
            Document document,
            SyntaxTrivia trivia,
            List<OutliningSpan> spans,
            CancellationToken cancellationToken)
        {
            CollectOutliningSpans(trivia.SyntaxTree, trivia, spans, cancellationToken);
        }

        public void CollectOutliningSpans(SyntaxTree syntaxTree, SyntaxTrivia trivia, List<OutliningSpan> spans, CancellationToken cancellationToken)
        {
            // We'll always be leading trivia of some token.
            var startPos = trivia.FullSpan.Start;
            var endPos = trivia.FullSpan.End;

            // Look through our parent token's trivia, to:
            // 1. See if we're the first disabled trivia attached to the token.
            // 2. To extend the span to the end of the last disabled trivia.
            //
            // The issue is that if there are other pre-processor directives (like #regions or
            // #lines) mixed in the disabled code, they will be interleaved.  Keep walking past
            // them to the next thing that will actually end a disabled block. When we encounter
            // one, we must also consider which opening block they end. In case of nested pre-processor
            // directives, the inner most end block should match the inner most open block and so on.
            var parentTriviaList = trivia.Token.LeadingTrivia;
            var indexInParent = parentTriviaList.IndexOf(trivia);

            // Note: in some error cases (for example when all future tokens end up being skipped)
            // the parser may end up attaching pre-processor directives as trailing trivia to a 
            // preceding token.
            if (indexInParent < 0)
            {
                parentTriviaList = trivia.Token.TrailingTrivia;
                indexInParent = parentTriviaList.IndexOf(trivia);
            }

            if (indexInParent <= 0 ||
                (!parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.IfDirectiveTrivia) &&
                 !parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.ElifDirectiveTrivia) &&
                 !parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.ElseDirectiveTrivia)))
            {
                return;
            }

            var nestedIfDirectiveTrivia = 0;
            for (int i = indexInParent; i < parentTriviaList.Count; i++)
            {
                if (parentTriviaList[i].IsKind(SyntaxKind.IfDirectiveTrivia))
                {
                    nestedIfDirectiveTrivia++;
                }

                if (parentTriviaList[i].IsKind(SyntaxKind.EndIfDirectiveTrivia) ||
                    parentTriviaList[i].IsKind(SyntaxKind.ElifDirectiveTrivia) ||
                    parentTriviaList[i].IsKind(SyntaxKind.ElseDirectiveTrivia))
                {
                    if (nestedIfDirectiveTrivia > 0)
                    {
                        nestedIfDirectiveTrivia--;
                    }
                    else
                    {
                        endPos = parentTriviaList[i - 1].FullSpan.End;
                        break;
                    }
                }
            }

            // Now, exclude the last newline if there is one.
            var text = syntaxTree.GetText(cancellationToken);
            if (endPos > 1 && text[endPos - 1] == '\n' && text[endPos - 2] == '\r')
            {
                endPos -= 2;
            }
            else if (endPos > 0 && SyntaxFacts.IsNewLine(text[endPos - 1]))
            {
                endPos--;
            }

            var span = TextSpan.FromBounds(startPos, endPos);
            spans.Add(new OutliningSpan(
                span,
                bannerText: CSharpOutliningHelpers.Ellipsis,
                autoCollapse: true));
        }
    }
}
