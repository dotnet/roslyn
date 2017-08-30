// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal class DisabledTextTriviaStructureProvider : AbstractSyntaxTriviaStructureProvider
    {
        public override void CollectBlockSpans(
            Document document,
            SyntaxTrivia trivia,
            ArrayBuilder<BlockSpan> spans,
            CancellationToken cancellationToken)
        {
            CollectBlockSpans(trivia.SyntaxTree, trivia, spans, cancellationToken);
        }

        public void CollectBlockSpans(
            SyntaxTree syntaxTree, SyntaxTrivia trivia,
            ArrayBuilder<BlockSpan> spans, CancellationToken cancellationToken)
        {
            // We'll always be leading trivia of some token.
            var startPos = trivia.FullSpan.Start;

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

            if (indexInParent <= 0)
            {
                return;
            }

            if (!parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.IfDirectiveTrivia) &&
                !parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.ElifDirectiveTrivia) &&
                !parentTriviaList[indexInParent - 1].IsKind(SyntaxKind.ElseDirectiveTrivia))
            {
                return;
            }

            var endPos = GetEndPositionIncludingLastNewLine(trivia, parentTriviaList, indexInParent);

            // Now, exclude the last newline if there is one.
            endPos = GetEndPositionExludingLastNewLine(syntaxTree, endPos, cancellationToken);

            var span = TextSpan.FromBounds(startPos, endPos);
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                type: BlockTypes.PreprocessorRegion,
                bannerText: CSharpStructureHelpers.Ellipsis,
                autoCollapse: true));
        }

        private static int GetEndPositionExludingLastNewLine(SyntaxTree syntaxTree, int endPos, CancellationToken cancellationToken)
        {
            var text = syntaxTree.GetText(cancellationToken);
            return endPos >= 2 && text[endPos - 1] == '\n' && text[endPos - 2] == '\r' ? endPos - 2 :
                   endPos >= 1 && SyntaxFacts.IsNewLine(text[endPos - 1])              ? endPos - 1 : endPos;
        }

        private int GetEndPositionIncludingLastNewLine(
            SyntaxTrivia trivia, SyntaxTriviaList triviaList, int index)
        {
            var nestedIfDirectiveTrivia = 0;
            for (var i = index; i < triviaList.Count; i++)
            {
                var currentTrivia = triviaList[i];
                switch (currentTrivia.Kind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        // Hit a nested if directive.  Keep track of this so we can ensure
                        // that our actual disabled region reached the right end point.
                        nestedIfDirectiveTrivia++;
                        continue;

                    case SyntaxKind.EndIfDirectiveTrivia:
                        if (nestedIfDirectiveTrivia > 0)
                        {
                            // This #endif corresponded to a nested #if, pop our stack
                            // and keep searching.
                            nestedIfDirectiveTrivia--;
                            continue;
                        }

                        // Found an #endif corresponding to our original #if/#elif/#else
                        // region we started with. Mark this range as the range to collapse.
                        return triviaList[i - 1].FullSpan.End;

                    case SyntaxKind.ElseDirectiveTrivia:
                    case SyntaxKind.ElifDirectiveTrivia:
                        if (nestedIfDirectiveTrivia > 0)
                        {
                            // This #else/#elif corresponded to a nested #if, ignore as
                            // they're not relevant to the original construct we started 
                            // on.
                            continue;
                        }

                        // We found the next #else/#elif corresponding to our original #if/#elif/#else
                        // region we started with. Mark this range as the range to collapse.
                        return triviaList[i - 1].FullSpan.End;
                }
            }

            // Couldn't find an end.  Just mark to the end of the disabled text.
            return trivia.FullSpan.End;
        }
    }
}
