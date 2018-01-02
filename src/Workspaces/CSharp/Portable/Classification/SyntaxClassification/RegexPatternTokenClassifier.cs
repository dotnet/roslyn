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
                AddClassification(node.TextToken, ClassificationTypeNames.RegexText);
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
            }

            public void Visit(RegexCharacterClassRangeNode node)
            {
                AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);
            }

            public void Visit(RegexCharacterClassSubtractionNode node)
            {
                AddClassification(node.MinusToken, ClassificationTypeNames.RegexCharacterClass);
            }

            public void Visit(RegexPosixPropertyNode node)
            {
                // Because the .net regex parser completely skips these nodes, we'll
                // classify it as a comment as it has no impact on the actual regex.
                AddClassification(node.TextToken, ClassificationTypeNames.RegexComment);
            }

            public void Visit(RegexWildcardNode node)
            {
                
            }

            public void Visit(RegexZeroOrMoreQuantifierNode node)
            {
                AddClassification(node.AsteriskToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexOneOrMoreQuantifierNode node)
            {
                AddClassification(node.PlusToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexZeroOrOneQuantifierNode node)
            {
                AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexLazyQuantifierNode node)
            {
                AddClassification(node.QuestionToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexExactNumericQuantifierNode node)
            {
                AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexOpenNumericRangeQuantifierNode node)
            {
                AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexClosedNumericRangeQuantifierNode node)
            {
                AddClassification(node.OpenBraceToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.FirstNumberToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.CommaToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.SecondNumberToken, ClassificationTypeNames.RegexQuantifier);
                AddClassification(node.CloseBraceToken, ClassificationTypeNames.RegexQuantifier);
            }

            public void Visit(RegexAnchorNode node)
            {
                AddClassification(node.AnchorToken, ClassificationTypeNames.RegexAnchor);
            }

            public void Visit(RegexAlternationNode node)
            {
                AddClassification(node.BarToken, ClassificationTypeNames.RegexAlternation);
            }

            public void Visit(RegexSimpleGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexSimpleOptionsGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexNestedOptionsGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexNonCapturingGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexPositiveLookaheadGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexNegativeLookaheadGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexPositiveLookbehindGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexNegativeLookbehindGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexNonBacktrackingGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexCaptureGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexBalancingGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexConditionalCaptureGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            public void Visit(RegexConditionalExpressionGroupingNode node)
            {
                ClassifyGrouping(node);
            }

            private void ClassifyGrouping(RegexGroupingNode node)
            {
                foreach (var child in node)
                {
                    if (!child.IsNode)
                    {
                        AddClassification(child.Token, ClassificationTypeNames.RegexGrouping);
                    }
                }
            }

            public void Visit(RegexSimpleEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexControlEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexHexEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexUnicodeEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexCaptureEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexKCaptureEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexOctalEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexBackreferenceEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void Visit(RegexCategoryEscapeNode node)
            {
                ClassifyEscape(node);
            }

            public void ClassifyEscape(RegexNode node)
            {
                foreach (var child in node)
                {
                    if (child.IsNode)
                    {
                        ClassifyEscape(child.Node);
                    }
                    else 
                    {
                        AddClassification(
                            child.Token, ClassificationTypeNames.RegexEscape);
                    }
                }
            }
        }
    }
}
