// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
#if DEBUG
        /// <summary>
        /// nonOverlappingSpans spans used for Debug validation that spans that worker produces
        /// are not mutually overlapping.
        /// </summary>
        private SimpleIntervalTree<TextSpan> nonOverlappingSpans;
#endif

        private readonly TextSpan textSpan;
        private readonly List<ClassifiedSpan> result;
        private readonly CancellationToken cancellationToken;

        private Worker(TextSpan textSpan, List<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            this.result = result;
            this.textSpan = textSpan;
            this.cancellationToken = cancellationToken;
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

        [System.Diagnostics.Conditional("DEBUG")]
        private void Validate(TextSpan textSpan)
        {
#if DEBUG
            if (nonOverlappingSpans == null)
            {
                nonOverlappingSpans = SimpleIntervalTree.Create(TextSpanIntervalIntrospector.Instance);
            }

            // new span should not overlap with any span that we already have.
            Contract.Requires(!nonOverlappingSpans.GetOverlappingIntervals(textSpan.Start, textSpan.Length).Any());

            nonOverlappingSpans = nonOverlappingSpans.AddInterval(textSpan);
#endif
        }

        private void AddClassification(TextSpan span, string type)
        {
            Validate(span);
            this.result.Add(new ClassifiedSpan(type, span));
        }

        private void AddClassification(SyntaxTrivia trivia, string type)
        {
            if (trivia.Width() > 0 && textSpan.OverlapsWith(trivia.Span))
            {
                AddClassification(trivia.Span, type);
            }
        }

        private void AddClassification(SyntaxToken token, string type)
        {
            if (token.Width() > 0 && textSpan.OverlapsWith(token.Span))
            {
                AddClassification(token.Span, type);
            }
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
            foreach (var token in node.DescendantTokens(span: this.textSpan, descendIntoTrivia: false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                ClassifyToken(token);
            }
        }

        private void ClassifyToken(SyntaxToken token)
        {
            var span = token.Span;
            if (span.Length != 0 && textSpan.OverlapsWith(span))
            {
                var type = ClassificationHelpers.GetClassification(token);

                if (type != null)
                {
                    AddClassification(span, type);
                }
            }

            foreach (var trivia in token.LeadingTrivia)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ClassifyTrivia(trivia);
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ClassifyTrivia(trivia);
            }
        }

        private void ClassifyTrivia(SyntaxTrivia trivia)
        {
            if (trivia.Kind() == SyntaxKind.SingleLineCommentTrivia || trivia.Kind() == SyntaxKind.MultiLineCommentTrivia)
            {
                AddClassification(trivia, ClassificationTypeNames.Comment);
            }
            else if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
            {
                AddClassification(trivia, ClassificationTypeNames.ExcludedCode);
            }
            else if (trivia.Kind() == SyntaxKind.SkippedTokensTrivia)
            {
                ClassifySkippedTokens((SkippedTokensTriviaSyntax)trivia.GetStructure());
            }
            else if (trivia.IsDocComment())
            {
                ClassifyDocumentationComment((DocumentationCommentTriviaSyntax)trivia.GetStructure());
            }
            else if (trivia.Kind() == SyntaxKind.DocumentationCommentExteriorTrivia)
            {
                AddClassification(trivia, ClassificationTypeNames.XmlDocCommentDelimiter);
            }
            else if (SyntaxFacts.IsPreprocessorDirective(trivia.Kind()))
            {
                ClassifyPreprocessorDirective((DirectiveTriviaSyntax)trivia.GetStructure());
            }
        }

        private void ClassifySkippedTokens(SkippedTokensTriviaSyntax skippedTokens)
        {
            if (!textSpan.OverlapsWith(skippedTokens.Span))
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
