// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Structure
{
    internal sealed class DisabledTextTriviaStructureProvider : AbstractSyntaxTriviaStructureProvider
    {
        public override void CollectBlockSpans(
            SyntaxTrivia trivia,
            ref TemporaryArray<BlockSpan> spans,
            BlockStructureOptions options,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(trivia.SyntaxTree);
            CollectBlockSpans(trivia.SyntaxTree, trivia, ref spans, cancellationToken);
        }

        public static void CollectBlockSpans(
            SyntaxTree syntaxTree, SyntaxTrivia trivia,
            ref TemporaryArray<BlockSpan> spans, CancellationToken cancellationToken)
        {
            // We'll always be leading trivia of some token.
            var startPos = trivia.FullSpan.Start;

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

            var endTrivia = GetCorrespondingEndTrivia(trivia, parentTriviaList, indexInParent);
            var endPos = GetEndPositionExludingLastNewLine(syntaxTree, endTrivia, cancellationToken);

            var span = TextSpan.FromBounds(startPos, endPos);
            spans.Add(new BlockSpan(
                isCollapsible: true,
                textSpan: span,
                type: BlockTypes.PreprocessorRegion,
                bannerText: CSharpStructureHelpers.Ellipsis,
                autoCollapse: true));
        }

        private static int GetEndPositionExludingLastNewLine(SyntaxTree syntaxTree, SyntaxTrivia trivia, CancellationToken cancellationToken)
        {
            var endPos = trivia.FullSpan.End;
            var text = syntaxTree.GetText(cancellationToken);
            return endPos >= 2 && text[endPos - 1] == '\n' && text[endPos - 2] == '\r' ? endPos - 2 :
                   endPos >= 1 && SyntaxFacts.IsNewLine(text[endPos - 1]) ? endPos - 1 : endPos;
        }

        private static SyntaxTrivia GetCorrespondingEndTrivia(
            SyntaxTrivia trivia, SyntaxTriviaList triviaList, int index)
        {
            // Look through our parent token's trivia, to extend the span to the end of the last
            // disabled trivia.
            //
            // The issue is that if there are other pre-processor directives (like #regions or
            // #lines) mixed in the disabled code, they will be interleaved.  Keep walking past
            // them to the next thing that will actually end a disabled block. When we encounter
            // one, we must also consider which opening block they end. In case of nested pre-processor
            // directives, the inner most end block should match the inner most open block and so on.

            var nestedIfDirectiveTrivia = 0;
            for (var i = index; i < triviaList.Count; i++)
            {
                var currentTrivia = triviaList[i];
                switch (currentTrivia.Kind())
                {
                    case SyntaxKind.IfDirectiveTrivia:
                        // Hit a nested #if directive.  Keep track of this so we can ensure
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

                        // Found an #endif corresponding to our original #if/#elif/#else region we
                        // started with. Mark up to the trivia before this as the range to collapse.
                        return triviaList[i - 1];

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
                        // region we started with. Mark up to the trivia before this as the range
                        // to collapse.
                        return triviaList[i - 1];
                }
            }

            // Couldn't find a future trivia to collapse up to.  Just collapse the original 
            // disabled text trivia we started with.
            return trivia;
        }
    }
}
