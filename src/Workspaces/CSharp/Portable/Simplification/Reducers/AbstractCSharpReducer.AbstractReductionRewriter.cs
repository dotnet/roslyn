﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer
    {
        protected abstract class AbstractReductionRewriter : CSharpSyntaxRewriter, IReductionRewriter
        {
            private readonly ObjectPool<IReductionRewriter> _pool;

            protected CSharpParseOptions ParseOptions { get; private set; }
            protected OptionSet OptionSet { get; private set; }
            protected CancellationToken CancellationToken { get; private set; }
            protected SemanticModel SemanticModel { get; private set; }

            public bool HasMoreWork { get; private set; }

            // can be used to simplify whole subtrees while just annotating one syntax node.
            // This is e.g. useful in the name simplification, where a whole qualified name is annotated
            protected bool alwaysSimplify;

            private readonly HashSet<SyntaxNode> _processedParentNodes = new();

            protected AbstractReductionRewriter(ObjectPool<IReductionRewriter> pool)
                => _pool = pool;

            public void Initialize(ParseOptions parseOptions, OptionSet optionSet, CancellationToken cancellationToken)
            {
                ParseOptions = (CSharpParseOptions)parseOptions;
                OptionSet = optionSet;
                CancellationToken = cancellationToken;
            }

            public void Dispose()
            {
                ParseOptions = null;
                OptionSet = null;
                CancellationToken = CancellationToken.None;
                _processedParentNodes.Clear();
                SemanticModel = null;
                HasMoreWork = false;
                alwaysSimplify = false;

                _pool.Free(this);
            }

            private static SyntaxNode GetParentNode(SyntaxNode node)
                => node switch
                {
                    ExpressionSyntax expression => GetParentNode(expression),
                    PatternSyntax pattern => GetParentNode(pattern),
                    CrefSyntax cref => GetParentNode(cref),
                    _ => null
                };

            private static SyntaxNode GetParentNode(ExpressionSyntax expression)
            {
                var lastExpression = expression;
                for (SyntaxNode current = expression; current != null; current = current.Parent)
                {
                    if (current is ExpressionSyntax currentExpression)
                    {
                        lastExpression = currentExpression;
                    }
                }

                return lastExpression.Parent;
            }

            private static SyntaxNode GetParentNode(PatternSyntax pattern)
            {
                var lastPattern = pattern;
                for (SyntaxNode current = pattern; current != null; current = current.Parent)
                {
                    if (current is PatternSyntax currentPattern)
                    {
                        lastPattern = currentPattern;
                    }
                }

                return lastPattern.Parent;
            }

            private static SyntaxNode GetParentNode(CrefSyntax cref)
            {
                var topMostCref = cref
                    .AncestorsAndSelf()
                    .OfType<CrefSyntax>()
                    .LastOrDefault();

                return topMostCref.Parent;
            }

            protected SyntaxNode SimplifyNode<TNode>(
                TNode node,
                SyntaxNode newNode,
                SyntaxNode parentNode,
                Func<TNode, SemanticModel, OptionSet, CancellationToken, SyntaxNode> simplifier)
                where TNode : SyntaxNode
            {
                Debug.Assert(parentNode != null);

                this.CancellationToken.ThrowIfCancellationRequested();

                if (!this.alwaysSimplify && !node.HasAnnotation(Simplifier.Annotation))
                {
                    return newNode;
                }

                if (node != newNode || _processedParentNodes.Contains(parentNode))
                {
                    this.HasMoreWork = true;
                    return newNode;
                }

                if (!node.HasAnnotation(SimplificationHelpers.DontSimplifyAnnotation))
                {
                    var simplifiedNode = simplifier(node, this.SemanticModel, this.OptionSet, this.CancellationToken);
                    if (simplifiedNode != node)
                    {
                        _processedParentNodes.Add(parentNode);
                        this.HasMoreWork = true;
                        return simplifiedNode;
                    }
                }

                return node;
            }

            protected SyntaxNode SimplifyExpression<TExpression>(
                TExpression expression,
                SyntaxNode newNode,
                Func<TExpression, SemanticModel, OptionSet, CancellationToken, SyntaxNode> simplifier)
                where TExpression : SyntaxNode
            {
                var parentNode = GetParentNode(expression);
                if (parentNode == null)
                    return newNode;

                return SimplifyNode(expression, newNode, parentNode, simplifier);
            }

            protected SyntaxToken SimplifyToken(SyntaxToken token, Func<SyntaxToken, SemanticModel, OptionSet, CancellationToken, SyntaxToken> simplifier)
            {
                this.CancellationToken.ThrowIfCancellationRequested();

                return token.HasAnnotation(Simplifier.Annotation)
                    ? simplifier(token, this.SemanticModel, this.OptionSet, this.CancellationToken)
                    : token;
            }

            public override SyntaxNode VisitElementAccessExpression(ElementAccessExpressionSyntax node)
            {
                // Note that we prefer simplifying the argument list before the expression
                var argumentList = (BracketedArgumentListSyntax)this.Visit(node.ArgumentList);
                var expression = (ExpressionSyntax)this.Visit(node.Expression);

                return node.Update(expression, argumentList);
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                // Note that we prefer simplifying the argument list before the expression
                var argumentList = (ArgumentListSyntax)this.Visit(node.ArgumentList);
                var expression = (ExpressionSyntax)this.Visit(node.Expression);

                return node.Update(expression, argumentList);
            }

            public SyntaxNodeOrToken VisitNodeOrToken(SyntaxNodeOrToken nodeOrToken, SemanticModel semanticModel, bool simplifyAllDescendants)
            {
                this.SemanticModel = semanticModel;
                this.alwaysSimplify = simplifyAllDescendants;
                this.HasMoreWork = false;
                _processedParentNodes.Clear();

                if (nodeOrToken.IsNode)
                {
                    return Visit(nodeOrToken.AsNode());
                }
                else
                {
                    return VisitToken(nodeOrToken.AsToken());
                }
            }
        }
    }
}
