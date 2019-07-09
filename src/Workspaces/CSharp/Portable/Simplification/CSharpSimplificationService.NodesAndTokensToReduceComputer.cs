// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpSimplificationService : AbstractSimplificationService<ExpressionSyntax, StatementSyntax, CrefSyntax>
    {
        private class NodesAndTokensToReduceComputer : CSharpSyntaxRewriter
        {
            private readonly List<NodeOrTokenToReduce> _nodesAndTokensToReduce;
            private readonly Func<SyntaxNodeOrToken, bool> _isNodeOrTokenOutsideSimplifySpans;

            private static readonly Func<SyntaxNode, bool> s_containsAnnotations = n => n.ContainsAnnotations;
            private static readonly Func<SyntaxNodeOrToken, bool> s_hasSimplifierAnnotation = n => n.HasAnnotation(Simplifier.Annotation);

            private bool _simplifyAllDescendants;
            private bool _insideSpeculatedNode;

            /// <summary>
            /// Computes a list of nodes and tokens that need to be reduced in the given syntax root.
            /// </summary>
            public static ImmutableArray<NodeOrTokenToReduce> Compute(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
            {
                var reduceNodeComputer = new NodesAndTokensToReduceComputer(isNodeOrTokenOutsideSimplifySpans);
                reduceNodeComputer.Visit(root);
                return reduceNodeComputer._nodesAndTokensToReduce.ToImmutableArray();
            }

            private NodesAndTokensToReduceComputer(Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
                : base(visitIntoStructuredTrivia: true)
            {
                _nodesAndTokensToReduce = new List<NodeOrTokenToReduce>();
                _isNodeOrTokenOutsideSimplifySpans = isNodeOrTokenOutsideSimplifySpans;
                _simplifyAllDescendants = false;
                _insideSpeculatedNode = false;
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
                        // Add DontSimplifyAnnotation to node to ensure it doesn't get simplified.
                        return node.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation);
                    }
                    else
                    {
                        return node;
                    }
                }

                var savedSimplifyAllDescendants = _simplifyAllDescendants;
                _simplifyAllDescendants = _simplifyAllDescendants || node.HasAnnotation(Simplifier.Annotation);

                if (!_insideSpeculatedNode && SpeculationAnalyzer.CanSpeculateOnNode(node))
                {
                    if (_simplifyAllDescendants || node.DescendantNodesAndTokens(s_containsAnnotations, descendIntoTrivia: true).Any(s_hasSimplifierAnnotation))
                    {
                        _insideSpeculatedNode = true;
                        var rewrittenNode = base.Visit(node);
                        _nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(rewrittenNode, _simplifyAllDescendants, node));
                        _insideSpeculatedNode = false;
                    }
                }
                else if (node.ContainsAnnotations || savedSimplifyAllDescendants)
                {
                    node = base.Visit(node);
                }

                _simplifyAllDescendants = savedSimplifyAllDescendants;
                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (_isNodeOrTokenOutsideSimplifySpans(token))
                {
                    if (_simplifyAllDescendants)
                    {
                        // One of the ancestor node is within a simplification span, but this token is outside all simplification spans.
                        // Add DontSimplifyAnnotation to token to ensure it doesn't get simplified.
                        return token.WithAdditionalAnnotations(SimplificationHelpers.DontSimplifyAnnotation);
                    }
                    else
                    {
                        return token;
                    }
                }

                var savedSimplifyAllDescendants = _simplifyAllDescendants;
                _simplifyAllDescendants = _simplifyAllDescendants || token.HasAnnotation(Simplifier.Annotation);

                if (_simplifyAllDescendants && !_insideSpeculatedNode && !token.IsKind(SyntaxKind.None))
                {
                    _nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(token, simplifyAllDescendants: true, originalNodeOrToken: token));
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
                    var savedInsideSpeculatedNode = _insideSpeculatedNode;
                    _insideSpeculatedNode = false;
                    base.VisitTrivia(trivia);
                    _insideSpeculatedNode = savedInsideSpeculatedNode;
                }

                return trivia;
            }
        }
    }
}
