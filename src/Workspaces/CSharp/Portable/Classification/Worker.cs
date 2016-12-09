// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    /// <summary>
    /// Worker is an utility class that can classify a list of tokens or a tree within a
    /// requested span The implementation is generic and can produce any kind of classification
    /// artifacts T T is normally either ClassificationSpan or a Tuple (for testing purposes) 
    /// and constructed via provided factory.
    /// </summary>
    internal partial class Worker
    {
        private readonly TextSpan _textSpan;
        private readonly List<ClassifiedSpan> _result;
        private readonly CancellationToken _cancellationToken;

        private Worker(TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            _result = result;
            _textSpan = textSpan;
            _cancellationToken = cancellationToken;
        }

        internal static void CollectClassifiedSpans(
            IEnumerable<SyntaxToken> tokens, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var worker = new Worker(textSpan, result, cancellationToken);
            foreach (var tk in tokens)
            {
                worker.ClassifyToken(tk);
            }
        }

        internal static void CollectClassifiedSpans(SyntaxNode
            node, TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
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
        {
            return span.Length > 0 && _textSpan.OverlapsWith(span);
        }

        private void AddClassification(SyntaxTrivia trivia, string type)
        {
            AddClassification(trivia.Span, type);
        }

        private void AddClassification(SyntaxToken token, string type)
        {
            AddClassification(token.Span, type);
        }

        private void ClassifyNodeOrToken(SyntaxNodeOrToken nodeOrToken)
        {
            if (nodeOrToken.IsToken)
            {
                ClassifyToken(nodeOrToken.AsToken());
                return;
            }

            ClassifyNode(nodeOrToken.AsNode());
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
            var classificationSpanEnd = _textSpan.End;

            // First, skip all the trivia before the span we care about.
            var enumerator = list.GetEnumerator();
            while (true)
            {
                if (!enumerator.MoveNext())
                {
                    // Reached the end of the trivia.  It was all before the text span we care about
                    // Stop immediately.
                    return;
                }

                var trivia = enumerator.Current;
                if (trivia.FullSpan.End <= classificationSpanStart)
                {
                    // Trivia is entirely before the span we're classifying, ignore and move to the next.
                    continue;
                }
                else
                {
                    // Found trivia that is after the text span we're classifying.  
                    break;
                }
            }

            // Continue processing trivia from this point on until we get past the 
            do
            {
                var trivia = enumerator.Current;
                if (trivia.SpanStart >= _textSpan.End)
                {
                    // reached trivia that is past what we are classifying.  Stop immediately.
                    return;
                }

                ClassifyTrivia(trivia);
            }
            while (enumerator.MoveNext());
        }

        private void ClassifyTrivia(SyntaxTrivia trivia)
        {
            switch (trivia.Kind())
            {
                case SyntaxKind.SingleLineCommentTrivia:
                case SyntaxKind.MultiLineCommentTrivia:
                case SyntaxKind.ShebangDirectiveTrivia:
                    AddClassification(trivia, ClassificationTypeNames.Comment);
                    return;

                case SyntaxKind.DisabledTextTrivia:
                    AddClassification(trivia, ClassificationTypeNames.ExcludedCode);
                    return;

                case SyntaxKind.SkippedTokensTrivia:
                    ClassifySkippedTokens((SkippedTokensTriviaSyntax)trivia.GetStructure());
                    return;

                case SyntaxKind.SingleLineDocumentationCommentTrivia:
                case SyntaxKind.MultiLineDocumentationCommentTrivia:
                    ClassifyDocumentationComment((DocumentationCommentTriviaSyntax)trivia.GetStructure());
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
                case SyntaxKind.PragmaWarningDirectiveTrivia:
                case SyntaxKind.PragmaChecksumDirectiveTrivia:
                case SyntaxKind.ReferenceDirectiveTrivia:
                case SyntaxKind.LoadDirectiveTrivia:
                case SyntaxKind.BadDirectiveTrivia:
                    ClassifyPreprocessorDirective((DirectiveTriviaSyntax)trivia.GetStructure());
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
    }
}
