// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    /// <summary>
    /// Worker is an utility class that can classify a list of tokens or a tree within a
    /// requested span The implementation is generic and can produce any kind of classification
    /// artifacts T T is normally either ClassificationSpan or a Tuple (for testing purposes) 
    /// and constructed via provided factory.
    /// </summary>
    internal ref partial struct Worker
    {
        private readonly TextSpan _textSpan;
        private readonly ArrayBuilder<ClassifiedSpan> _result;
        private readonly CancellationToken _cancellationToken;

        private Worker(TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _result = result;
            _textSpan = textSpan;
            _cancellationToken = cancellationToken;
        }

        internal static void CollectClassifiedSpans(
            IEnumerable<SyntaxToken> tokens, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var worker = new Worker(textSpan, result, cancellationToken);
            foreach (var tk in tokens)
            {
                worker.ClassifyToken(tk);
            }
        }

        internal static void CollectClassifiedSpans(
            SyntaxNode node, TextSpan textSpan, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var worker = new Worker(textSpan, result, cancellationToken);
            worker.ClassifyNode(node);
        }

        private void AddClassification(TextSpan span, string type)
        {
            if (ShouldAddSpan(span))
            {
                _result.Add(new ClassifiedSpan(type, span));
            }
        }

        private bool ShouldAddSpan(TextSpan span)
            => span.Length > 0 && _textSpan.OverlapsWith(span);

        private void AddClassification(SyntaxTrivia trivia, string type)
            => AddClassification(trivia.Span, type);

        private void AddClassification(SyntaxToken token, string type)
            => AddClassification(token.Span, type);

        private void ClassifyNodeOrToken(SyntaxNodeOrToken nodeOrToken)
        {
            Debug.Assert(nodeOrToken.IsNode || nodeOrToken.IsToken);

            if (nodeOrToken.IsToken)
            {
                ClassifyToken(nodeOrToken.AsToken());
                return;
            }

            ClassifyNode(nodeOrToken.AsNode()!);
        }

        private void ClassifyNode(SyntaxNode node)
        {
            foreach (var token in node.DescendantTokens(span: _textSpan, descendIntoTrivia: false))
            {
                _cancellationToken.ThrowIfCancellationRequested();
                ClassifyToken(token);
            }
        }

        private void ClassifyToken(SyntaxToken token)
        {
            var span = token.Span;
            if (ShouldAddSpan(span))
            {
                var type = ClassificationHelpers.GetClassification(token);

                if (type != null)
                {
                    AddClassification(span, type);

                    // Additionally classify static symbols
                    if (token.Kind() == SyntaxKind.IdentifierToken
                        && ClassificationHelpers.IsStaticallyDeclared(token))
                    {
                        AddClassification(span, ClassificationTypeNames.StaticSymbol);
                    }
                }
            }

            ClassifyTriviaList(token.LeadingTrivia);
            ClassifyTriviaList(token.TrailingTrivia);
        }

        private void ClassifyTriviaList(SyntaxTriviaList list)
        {
            if (list.Count == 0)
            {
                return;
            }

            // We may have long lists of trivia (for example a huge list of // comments after someone
            // comments out a file).  Try to skip as many as possible if they're not actually in the span
            // we care about classifying.
            var classificationSpanStart = _textSpan.Start;

            // First, skip all the trivia before the span we care about.
            var enumerator = list.GetEnumerator();
            while (true)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (!enumerator.MoveNext())
                {
                    // Reached the end of the trivia.  It was all before the span we want to classify.
                    // Stop immediately.
                    return;
                }

                if (enumerator.Current.FullSpan.End > classificationSpanStart)
                {
                    // Found trivia that is after the text span we're classifying.  
                    break;
                }
            }

            // Continue processing trivia from this point on until we get past the span we want to classify.
            do
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var trivia = enumerator.Current;
                if (trivia.SpanStart >= _textSpan.End)
                {
                    // reached trivia that is past what we are classifying.  Stop immediately.
                    return;
                }

                ClassifyTrivia(trivia, list);
            }
            while (enumerator.MoveNext());
        }

        private void ClassifyTrivia(SyntaxTrivia trivia, SyntaxTriviaList triviaList)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.ShebangDirectiveTrivia:
                    AddClassification(trivia, ClassificationTypeNames.Comment);
                    return;

                case SyntaxKind.DisabledTextTrivia:
                    ClassifyDisabledText(trivia, triviaList);
                    return;

                case SyntaxKind.SkippedTokensTrivia:
                    ClassifySkippedTokens((SkippedTokensTriviaSyntax)trivia.GetStructure()!);
                    return;

                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    ClassifyDocumentationComment((DocumentationCommentTriviaSyntax)trivia.GetStructure()!);
                    return;

                case SyntaxKind.DocumentationCommentExteriorTrivia:
                    AddClassification(trivia, ClassificationTypeNames.XmlDocCommentDelimiter);
                    return;

                case SyntaxKind.ConflictMarkerTrivia:
                    ClassifyConflictMarker(trivia);
                    return;

                case SyntaxKind.IfDirectiveTrivia:
                case SyntaxKind.ElifDirectiveTrivia:
                case SyntaxKind.ElseDirectiveTrivia:
                case SyntaxKind.EndIfDirectiveTrivia:
                case SyntaxKind.RegionDirectiveTrivia:
                case SyntaxKind.EndRegionDirectiveTrivia:
                case SyntaxKind.DefineDirectiveTrivia:
                case SyntaxKind.UndefDirectiveTrivia:
                case SyntaxKind.ErrorDirectiveTrivia:
                case SyntaxKind.WarningDirectiveTrivia:
                case SyntaxKind.LineDirectiveTrivia:
                case SyntaxKind.LineSpanDirectiveTrivia:
                case SyntaxKind.PragmaWarningDirectiveTrivia:
                case SyntaxKind.PragmaChecksumDirectiveTrivia:
                case SyntaxKind.ReferenceDirectiveTrivia:
                case SyntaxKind.LoadDirectiveTrivia:
                case SyntaxKind.NullableDirectiveTrivia:
                case SyntaxKind.BadDirectiveTrivia:
                    ClassifyPreprocessorDirective((DirectiveTriviaSyntax)trivia.GetStructure()!);
                    return;
            }
        }

        private void ClassifySkippedTokens(SkippedTokensTriviaSyntax skippedTokens)
        {
            if (!_textSpan.OverlapsWith(skippedTokens.Span))
            {
                return;
            }

            var tokens = skippedTokens.Tokens;
            foreach (var tk in tokens)
            {
                ClassifyToken(tk);
            }
        }

        private void ClassifyConflictMarker(SyntaxTrivia trivia)
            => AddClassification(trivia, ClassificationTypeNames.Comment);

        private void ClassifyDisabledText(SyntaxTrivia trivia, SyntaxTriviaList triviaList)
        {
            var index = triviaList.IndexOf(trivia);
            if (index >= 2 &&
                triviaList[index - 1].Kind() == SyntaxKind.EndOfLineTrivia &&
                triviaList[index - 2].Kind() == SyntaxKind.ConflictMarkerTrivia)
            {
                // for the ======== add a comment for the first line, and then lex all
                // subsequent lines up until the end of the conflict marker.
                foreach (var token in SyntaxFactory.ParseTokens(text: trivia.ToFullString(), initialTokenPosition: trivia.SpanStart))
                {
                    ClassifyToken(token);
                }
            }
            else
            {
                AddClassification(trivia, ClassificationTypeNames.ExcludedCode);
            }
        }
    }
}
