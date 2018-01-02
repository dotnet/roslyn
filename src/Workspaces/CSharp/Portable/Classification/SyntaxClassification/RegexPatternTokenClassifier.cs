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
                    ClassificationTypeNames.RegexComment, RegexHelpers.GetSpan(trivia.VirtualChars)));
            }
        }

        private class Visitor : IRegexNodeVisitor
        {
            private readonly ArrayBuilder<ClassifiedSpan> _result;

            public Visitor(ArrayBuilder<ClassifiedSpan> result)
            {
                _result = result;
            }

            private void AddClassification(RegexToken token, string typeName)
            {
                if (!token.IsMissing)
                {
                    _result.Add(new ClassifiedSpan(typeName, RegexHelpers.GetSpan(token)));
                }
            }

            public void Visit(RegexCompilationUnit node)
            {
                // Nothing to highlight.
            }

            public void Visit(RegexSequenceNode node)
            {
                // Nothing to highlight.   
            }

            public void Visit(RegexTextNode node)
            {
                // Nothing to highlight.
            }

            public void Visit(RegexCharacterClassNode node)
            {
                AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
                AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
            }

            public void Visit(RegexNegatedCharacterClassNode node)
            {
                AddClassification(node.OpenBracketToken, ClassificationTypeNames.RegexCharacterClass);
                AddClassification(node.CaretToken, ClassificationTypeNames.RegexCharacterClass);
                AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);

                AddClassification(node.CloseBracketToken, ClassificationTypeNames.RegexCharacterClass);
            }

            public void Visit(RegexCharacterClassRangeNode node)
            {
                
            }

            public void Visit(RegexCharacterClassSubtractionNode node)
            {
                
            }

            public void Visit(RegexPosixPropertyNode node)
            {
                
            }

            public void Visit(RegexWildcardNode node)
            {
                
            }

            public void Visit(RegexZeroOrMoreQuantifierNode node)
            {
                
            }

            public void Visit(RegexOneOrMoreQuantifierNode node)
            {
                
            }

            public void Visit(RegexZeroOrOneQuantifierNode node)
            {
                
            }

            public void Visit(RegexLazyQuantifierNode node)
            {
                
            }

            public void Visit(RegexExactNumericQuantifierNode node)
            {
                
            }

            public void Visit(RegexOpenNumericRangeQuantifierNode node)
            {
                
            }

            public void Visit(RegexClosedNumericRangeQuantifierNode node)
            {
                
            }

            public void Visit(RegexAnchorNode node)
            {
                
            }

            public void Visit(RegexAlternationNode node)
            {
                
            }

            public void Visit(RegexSimpleGroupingNode node)
            {
                
            }

            public void Visit(RegexSimpleOptionsGroupingNode node)
            {
                
            }

            public void Visit(RegexNestedOptionsGroupingNode node)
            {
                
            }

            public void Visit(RegexNonCapturingGroupingNode node)
            {
                
            }

            public void Visit(RegexPositiveLookaheadGroupingNode node)
            {
                
            }

            public void Visit(RegexNegativeLookaheadGroupingNode node)
            {
                
            }

            public void Visit(RegexPositiveLookbehindGroupingNode node)
            {
                
            }

            public void Visit(RegexNegativeLookbehindGroupingNode node)
            {
                
            }

            public void Visit(RegexNonBacktrackingGroupingNode node)
            {
                
            }

            public void Visit(RegexCaptureGroupingNode node)
            {
                
            }

            public void Visit(RegexBalancingGroupingNode node)
            {
                
            }

            public void Visit(RegexConditionalCaptureGroupingNode node)
            {
                
            }

            public void Visit(RegexConditionalExpressionGroupingNode node)
            {
                
            }

            public void Visit(RegexSimpleEscapeNode node)
            {
                
            }

            public void Visit(RegexControlEscapeNode node)
            {
                
            }

            public void Visit(RegexHexEscapeNode node)
            {
                
            }

            public void Visit(RegexUnicodeEscapeNode node)
            {
                
            }

            public void Visit(RegexCaptureEscapeNode node)
            {
                
            }

            public void Visit(RegexKCaptureEscapeNode node)
            {
                
            }

            public void Visit(RegexOctalEscapeNode node)
            {
                
            }

            public void Visit(RegexBackreferenceEscapeNode node)
            {
                
            }

            public void Visit(RegexCategoryEscapeNode node)
            {
                
            }
        }
    }
}
