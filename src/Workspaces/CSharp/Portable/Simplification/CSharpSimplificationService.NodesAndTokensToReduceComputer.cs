// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification;

internal partial class CSharpSimplificationService
{
    private sealed class NodesAndTokensToReduceComputer : CSharpSyntaxRewriter
    {
        private readonly List<NodeOrTokenToReduce> _nodesAndTokensToReduce = [];
        private readonly Func<SyntaxNodeOrToken, bool> _isNodeOrTokenOutsideSimplifySpans;

        private bool _simplifyAllDescendants;
        private int? _speculatedNodeAnnotationCount;
        private bool InsideSpeculatedNode => _speculatedNodeAnnotationCount.HasValue;

        /// <summary>
        /// Computes a list of nodes and tokens that need to be reduced in the given syntax root.
        /// </summary>
        public static ImmutableArray<NodeOrTokenToReduce> Compute(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
        {
            var reduceNodeComputer = new NodesAndTokensToReduceComputer(isNodeOrTokenOutsideSimplifySpans);
            reduceNodeComputer.Visit(root);
            return [.. reduceNodeComputer._nodesAndTokensToReduce];
        }

        private NodesAndTokensToReduceComputer(Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
            : base(visitIntoStructuredTrivia: true)
        {
            _isNodeOrTokenOutsideSimplifySpans = isNodeOrTokenOutsideSimplifySpans;
            _simplifyAllDescendants = false;
            _speculatedNodeAnnotationCount = null;
        }

        public override SyntaxNode Visit(SyntaxNode node)
        {
            if (node == null)
            {
                return node;
            }

            if (_isNodeOrTokenOutsideSimplifySpans(node))
            {
                if (_simplifyAllDescendants)
                {
                    // One of the ancestor node is within a simplification span, but this node is outside all simplification spans.
                    // Add DoNotSimplifyAnnotation to node to ensure it doesn't get simplified.
                    return node.WithAdditionalAnnotations(SimplificationHelpers.DoNotSimplifyAnnotation);
                }
                else
                {
                    return node;
                }
            }

            var hasSimplifierAnnotation = node.HasAnnotation(Simplifier.Annotation);

            var savedSimplifyAllDescendants = _simplifyAllDescendants;
            _simplifyAllDescendants = _simplifyAllDescendants || hasSimplifierAnnotation;

            var descendantsWithSimplifierAnnotation = node.DescendantNodesAndTokens(s_containsAnnotations, descendIntoTrivia: true).Count(s_hasSimplifierAnnotation);
            var subTreeAnnotationCount = descendantsWithSimplifierAnnotation + (hasSimplifierAnnotation ? 1 : 0);

            // Consider the current node as a possible node for reducing if we curently
            // are not inside any speculated node OR if this node has an equal count of
            // simplifier annotations in it's subtree as the node we are currently speculating
            // in which case we will considering switching to this one.
            if ((!InsideSpeculatedNode || (subTreeAnnotationCount == _speculatedNodeAnnotationCount && IsSupportedType(node)))
                && SpeculationAnalyzer.CanSpeculateOnNode(node))
            {
                if (_simplifyAllDescendants || descendantsWithSimplifierAnnotation > 0)
                {
                    _speculatedNodeAnnotationCount = subTreeAnnotationCount;

                    var rewrittenNode = base.Visit(node);

                    // Extra check to see if we are still inside a speculated node
                    // or if we have already picked a better one
                    if (_speculatedNodeAnnotationCount >= 0)
                    {
                        _nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(rewrittenNode, _simplifyAllDescendants, node));
                        _speculatedNodeAnnotationCount = null;
                    }
                }
            }
            else if (node.ContainsAnnotations || savedSimplifyAllDescendants)
            {
                node = base.Visit(node);
            }

            _simplifyAllDescendants = savedSimplifyAllDescendants;
            return node;

            // While this is definitely not correct, enabling this behavior
            // just for ForStatementSyntax will work correctly in some
            // cases we care about without risking breaking anything.
            static bool IsSupportedType(SyntaxNode node)
            {
                return node is ForStatementSyntax;
            }
        }

        public override SyntaxToken VisitToken(SyntaxToken token)
        {
            if (_isNodeOrTokenOutsideSimplifySpans(token))
            {
                if (_simplifyAllDescendants)
                {
                    // One of the ancestor node is within a simplification span, but this token is outside all simplification spans.
                    // Add DoNotSimplifyAnnotation to token to ensure it doesn't get simplified.
                    return token.WithAdditionalAnnotations(SimplificationHelpers.DoNotSimplifyAnnotation);
                }
                else
                {
                    return token;
                }
            }

            var savedSimplifyAllDescendants = _simplifyAllDescendants;
            _simplifyAllDescendants = _simplifyAllDescendants || token.HasAnnotation(Simplifier.Annotation);

            if (_simplifyAllDescendants && !InsideSpeculatedNode && !token.IsKind(SyntaxKind.None))
            {
                _nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(token, SimplifyAllDescendants: true, token));
            }

            if (token.ContainsAnnotations || savedSimplifyAllDescendants)
            {
                token = base.VisitToken(token);
            }

            _simplifyAllDescendants = savedSimplifyAllDescendants;
            return token;
        }

        public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
        {
            if (trivia.HasStructure)
            {
                var savedAnnotationCount = _speculatedNodeAnnotationCount;
                _speculatedNodeAnnotationCount = null;
                base.VisitTrivia(trivia);
                _speculatedNodeAnnotationCount = savedAnnotationCount;
            }

            return trivia;
        }
    }
}
