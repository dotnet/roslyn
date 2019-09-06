// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer
    {
        protected abstract class AbstractReductionRewriter : CSharpSyntaxRewriter, IReductionRewriter
        {
            private readonly ObjectPool<IReductionRewriter> _pool;

            protected CSharpParseOptions ParseOptions { get; private set; }
            private OptionSet _optionSet;
            private CancellationToken _cancellationToken;
            private SemanticModel _semanticModel;

            public bool HasMoreWork { get; private set; }

            // can be used to simplify whole subtrees while just annotating one syntax node.
            // This is e.g. useful in the name simplification, where a whole qualified name is annotated
            protected bool alwaysSimplify;

            private readonly HashSet<SyntaxNode> _processedParentNodes = new HashSet<SyntaxNode>();

            protected AbstractReductionRewriter(ObjectPool<IReductionRewriter> pool)
                => _pool = pool;

            public void Initialize(ParseOptions parseOptions, OptionSet optionSet, CancellationToken cancellationToken)
            {
                ParseOptions = (CSharpParseOptions)parseOptions;
                _optionSet = optionSet;
                _cancellationToken = cancellationToken;
            }

            public void Dispose()
            {
                ParseOptions = null;
                _optionSet = null;
                _cancellationToken = CancellationToken.None;
                _processedParentNodes.Clear();
                _semanticModel = null;
                HasMoreWork = false;
                alwaysSimplify = false;

                _pool.Free(this);
            }

            private static SyntaxNode GetParentNode(SyntaxNode node)
            {
                if (node is ExpressionSyntax expression)
                {
                    return GetParentNode(expression);
                }

                if (node is CrefSyntax cref)
                {
                    return GetParentNode(cref);
                }

                return null;
            }

            private static SyntaxNode GetParentNode(ExpressionSyntax expression)
            {
                var topMostExpression = expression
                    .AncestorsAndSelf()
                    .OfType<ExpressionSyntax>()
                    .LastOrDefault();

                return topMostExpression.Parent;
            }

            private static SyntaxNode GetParentNode(CrefSyntax cref)
            {
                var topMostCref = cref
                    .AncestorsAndSelf()
                    .OfType<CrefSyntax>()
                    .LastOrDefault();

                return topMostCref.Parent;
            }

            private static SyntaxNode GetParentNode(StatementSyntax statement)
            {
                return statement
                    .AncestorsAndSelf()
                    .OfType<StatementSyntax>()
                    .LastOrDefault();
            }

            protected SyntaxNode SimplifyNode<TNode>(
                TNode node,
                SyntaxNode newNode,
                SyntaxNode parentNode,
                Func<TNode, SemanticModel, OptionSet, CancellationToken, SyntaxNode> simplifier)
                where TNode : SyntaxNode
            {
                Debug.Assert(parentNode != null);

                _cancellationToken.ThrowIfCancellationRequested();

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
                    var simplifiedNode = simplifier(node, _semanticModel, _optionSet, _cancellationToken);
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
                {
                    return newNode;
                }

                return SimplifyNode(expression, newNode, parentNode, simplifier);
            }

            protected SyntaxToken SimplifyToken(SyntaxToken token, Func<SyntaxToken, SemanticModel, OptionSet, CancellationToken, SyntaxToken> simplifier)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                return token.HasAnnotation(Simplifier.Annotation)
                    ? simplifier(token, _semanticModel, _optionSet, _cancellationToken)
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
                _semanticModel = semanticModel;
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
