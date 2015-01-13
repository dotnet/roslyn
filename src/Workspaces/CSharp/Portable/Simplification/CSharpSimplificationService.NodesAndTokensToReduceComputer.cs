// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            private readonly List<NodeOrTokenToReduce> nodesAndTokensToReduce;
            private readonly Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans;

            private static readonly Func<SyntaxNode, bool> ContainsAnnotations = n => n.ContainsAnnotations;
            private static readonly Func<SyntaxNodeOrToken, bool> HasSimplifierAnnotation = n => n.HasAnnotation(Simplifier.Annotation);

            private bool simplifyAllDescendants;
            private bool insideSpeculatedNode;

            /// <summary>
            /// Computes a list of nodes and tokens that need to be reduced in the given syntax root.
            /// </summary>
            public static ImmutableArray<NodeOrTokenToReduce> Compute(SyntaxNode root, Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
            {
                var reduceNodeComputer = new NodesAndTokensToReduceComputer(isNodeOrTokenOutsideSimplifySpans);
                reduceNodeComputer.Visit(root);
                return reduceNodeComputer.nodesAndTokensToReduce.ToImmutableArray();
            }

            private NodesAndTokensToReduceComputer(Func<SyntaxNodeOrToken, bool> isNodeOrTokenOutsideSimplifySpans)
                : base(visitIntoStructuredTrivia: true)
            {
                this.nodesAndTokensToReduce = new List<NodeOrTokenToReduce>();
                this.isNodeOrTokenOutsideSimplifySpans = isNodeOrTokenOutsideSimplifySpans;
                this.simplifyAllDescendants = false;
                this.insideSpeculatedNode = false;
            }

            public override SyntaxNode Visit(SyntaxNode node)
            {
                if (node == null)
                {
                    return node;
                }

                if (isNodeOrTokenOutsideSimplifySpans(node))
                {
                    if (this.simplifyAllDescendants)
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

                var savedSimplifyAllDescendants = this.simplifyAllDescendants;
                this.simplifyAllDescendants = this.simplifyAllDescendants || node.HasAnnotation(Simplifier.Annotation);

                if (!this.insideSpeculatedNode && SpeculationAnalyzer.CanSpeculateOnNode(node))
                {
                    if (this.simplifyAllDescendants || node.DescendantNodesAndTokens(ContainsAnnotations, descendIntoTrivia: true).Any(HasSimplifierAnnotation))
                    {
                        this.insideSpeculatedNode = true;
                        var rewrittenNode = base.Visit(node);
                        this.nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(rewrittenNode, this.simplifyAllDescendants, node));
                        this.insideSpeculatedNode = false;
                    }
                }
                else if (node.ContainsAnnotations || savedSimplifyAllDescendants)
                {
                    node = base.Visit(node);
                }

                this.simplifyAllDescendants = savedSimplifyAllDescendants;
                return node;
            }

            public override SyntaxToken VisitToken(SyntaxToken token)
            {
                if (isNodeOrTokenOutsideSimplifySpans(token))
                {
                    if (this.simplifyAllDescendants)
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

                bool savedSimplifyAllDescendants = this.simplifyAllDescendants;
                this.simplifyAllDescendants = this.simplifyAllDescendants || token.HasAnnotation(Simplifier.Annotation);

                if (this.simplifyAllDescendants && !this.insideSpeculatedNode && !token.IsKind(SyntaxKind.None))
                {
                    this.nodesAndTokensToReduce.Add(new NodeOrTokenToReduce(token, simplifyAllDescendants: true, originalNodeOrToken: token));
                }

                if (token.ContainsAnnotations || savedSimplifyAllDescendants)
                {
                    token = base.VisitToken(token);
                }

                this.simplifyAllDescendants = savedSimplifyAllDescendants;
                return token;
            }

            public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
            {
                if (trivia.HasStructure)
                {
                    var savedInsideSpeculatedNode = this.insideSpeculatedNode;
                    this.insideSpeculatedNode = false;
                    base.VisitTrivia(trivia);
                    this.insideSpeculatedNode = savedInsideSpeculatedNode;
                }

                return trivia;
            }
        }
    }
}