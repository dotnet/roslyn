// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.CSharp.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.RegularExpressions;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class RegexPatternTokenClassifier : AbstractSyntaxClassifier
    {
        private static readonly ConditionalWeakTable<SemanticModel, RegexPatternDetector> _modelToDetector =
            new ConditionalWeakTable<SemanticModel, RegexPatternDetector>();

        public override ImmutableArray<int> SyntaxTokenKinds { get; } = ImmutableArray.Create<int>((int)SyntaxKind.StringLiteralToken);

        public override void AddClassifications(SyntaxToken token, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            Debug.Assert(token.Kind() == SyntaxKind.StringLiteralToken);

            // Do some quick syntactic checks before doing any complex work.
            if (RegexPatternDetector.IsDefinitelyNotPattern(token, CSharpSyntaxFactsService.Instance))
            {
                return;
            }

            // Looks like it could be a regex pattern.  Do more complex check.
            // Cache the detector we create, so we don't have to continually do
            // the same semantic work for every string literal token we visit.
            var detector = _modelToDetector.GetValue(
                semanticModel, m => RegexPatternDetector.TryCreate(
                    m, CSharpSyntaxFactsService.Instance, CSharpSemanticFactsService.Instance));

            if (!detector.IsRegexPattern(token, cancellationToken, out var options))
            {
                return;
            }

            var virtualCharService = CSharpVirtualCharService.Instance;
            var chars = virtualCharService.TryConvertToVirtualChars(token);
            if (chars.IsDefaultOrEmpty)
            {
                return;
            }

            var tree = RegexParser.Parse(chars, options);
            var visitor = new Visitor(result);
            AddClassifications(tree.Root, visitor, result);
        }

        private void AddClassifications(RegexNode node, Visitor visitor, ArrayBuilder<ClassifiedSpan> result)
        {
            node.Accept(visitor);

            for (int i = 0, n = node.ChildCount; i < n; i++)
            {
                var child = node.ChildAt(i);
                if (child.IsNode)
                {
                    AddClassifications(child.Node, visitor, result);
                }
                else
                {
                    AddTriviaClassifications(child.Token, result);
                }
            }
        }

        private void AddTriviaClassifications(RegexToken token, ArrayBuilder<ClassifiedSpan> result)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                AddTriviaClassifications(trivia, result);
            }
        }

        private void AddTriviaClassifications(RegexTrivia trivia, ArrayBuilder<ClassifiedSpan> result)
        {
            if (trivia.Kind == RegexKind.CommentTrivia &&
                trivia.VirtualChars.Length > 0)
            {
                result.Add(new ClassifiedSpan(
                    ClassificationTypeNames.Comment, RegexHelpers.GetSpan(trivia.VirtualChars)));
            }
        }

        private class Visitor : IRegexNodeVisitor
        {
            private readonly ArrayBuilder<ClassifiedSpan> _result;

            public Visitor(ArrayBuilder<ClassifiedSpan> result)
            {
                _result = result;
            }

            public void Visit(RegexCompilationUnit node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexSequenceNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexTextNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCharacterClassNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNegatedCharacterClassNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCharacterClassRangeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCharacterClassSubtractionNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexPosixPropertyNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexWildcardNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexZeroOrMoreQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexOneOrMoreQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexZeroOrOneQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexLazyQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexExactNumericQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexOpenNumericRangeQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexClosedNumericRangeQuantifierNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexAnchorNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexAlternationNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexSimpleGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexSimpleOptionsGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNestedOptionsGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNonCapturingGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexPositiveLookaheadGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNegativeLookaheadGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexPositiveLookbehindGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNegativeLookbehindGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexNonBacktrackingGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCaptureGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexBalancingGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexConditionalCaptureGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexConditionalExpressionGroupingNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexSimpleEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexControlEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexHexEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexUnicodeEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCaptureEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexKCaptureEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexOctalEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexBackreferenceEscapeNode node)
            {
                throw new NotImplementedException();
            }

            public void Visit(RegexCategoryEscapeNode node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
