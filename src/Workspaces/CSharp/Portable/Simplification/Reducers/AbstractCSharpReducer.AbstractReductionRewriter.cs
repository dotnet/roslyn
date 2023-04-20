// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal abstract partial class AbstractCSharpReducer
    {
        protected abstract class AbstractReductionRewriter : CSharpSyntaxRewriter, IReductionRewriter
        {
            private readonly ObjectPool<IReductionRewriter> _pool;

            protected CSharpParseOptions? ParseOptions { get; private set; }
            protected CSharpSimplifierOptions? Options { get; private set; }
            protected CancellationToken CancellationToken { get; private set; }
            protected SemanticModel? SemanticModel { get; private set; }

            public bool HasMoreWork { get; private set; }

            // can be used to simplify whole subtrees while just annotating one syntax node.
            // This is e.g. useful in the name simplification, where a whole qualified name is annotated
            protected bool alwaysSimplify;

            private readonly HashSet<SyntaxNode> _processedParentNodes = new();

            protected AbstractReductionRewriter(ObjectPool<IReductionRewriter> pool)
                => _pool = pool;

            public void Initialize(ParseOptions parseOptions, SimplifierOptions options, CancellationToken cancellationToken)
            {
                Contract.ThrowIfNull(options);

                ParseOptions = (CSharpParseOptions)parseOptions;
                Options = (CSharpSimplifierOptions)options;
                CancellationToken = cancellationToken;
            }

            public void Dispose()
            {
                ParseOptions = null;
                Options = null;
                CancellationToken = CancellationToken.None;
                _processedParentNodes.Clear();
                SemanticModel = null;
                HasMoreWork = false;
                alwaysSimplify = false;

                _pool.Free(this);
            }

            [MemberNotNull(nameof(Options), nameof(ParseOptions), nameof(SemanticModel))]
            public void RequireInitialized()
            {
                Contract.ThrowIfNull(ParseOptions);
                Contract.ThrowIfNull(Options);
                Contract.ThrowIfNull(SemanticModel);
            }

            private static SyntaxNode GetParentNode(SyntaxNode node)
                => node switch
                {
                    ExpressionSyntax expression => GetParentNode(expression),
                    PatternSyntax pattern => GetParentNode(pattern),
                    CrefSyntax cref => GetParentNode(cref),
                    _ => node.GetRequiredParent(),
                };

            private static SyntaxNode GetParentNode(ExpressionSyntax expression)
            {
                // Walk all the way up the expression to the non-expression parent.  Effectively, once we change an
                // expression *within* some larger expression context, we want to stop rewriting any further sibling
                // expressions as they could be affected by this change.

                SyntaxNode parent = expression;
                for (var current = (SyntaxNode)expression; current != null; current = current.Parent)
                {
                    // if we're in an argument, walk up into that as well as the change in one argument can affect
                    // other arguments in a call.
                    if (current is ExpressionSyntax or ArgumentSyntax)
                        parent = current;
                }

                return parent.GetRequiredParent();
            }

            private static SyntaxNode GetParentNode(PatternSyntax pattern)
            {
                var lastPattern = pattern;
                for (SyntaxNode? current = pattern; current != null; current = current.Parent)
                {
                    if (current is PatternSyntax currentPattern)
                    {
                        lastPattern = currentPattern;
                    }
                }

                Contract.ThrowIfNull(lastPattern.Parent);
                return lastPattern.Parent;
            }

            private static SyntaxNode GetParentNode(CrefSyntax cref)
            {
                var topMostCref = cref
                    .AncestorsAndSelf()
                    .OfType<CrefSyntax>()
                    .Last();

                Contract.ThrowIfNull(topMostCref.Parent);
                return topMostCref.Parent;
            }

            protected SyntaxNode? SimplifyNode<TNode>(
                TNode node,
                SyntaxNode? newNode,
                Func<TNode, SemanticModel, CSharpSimplifierOptions, CancellationToken, SyntaxNode> simplifier)
                where TNode : SyntaxNode
            {
                var parentNode = GetParentNode(node);
                RequireInitialized();

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

                if (!node.HasAnnotation(SimplificationHelpers.DoNotSimplifyAnnotation))
                {
                    var simplifiedNode = simplifier(node, this.SemanticModel, this.Options, this.CancellationToken);
                    if (simplifiedNode != node)
                    {
                        _processedParentNodes.Add(parentNode);
                        this.HasMoreWork = true;
                        return simplifiedNode;
                    }
                }

                return node;
            }

            protected SyntaxToken SimplifyToken(SyntaxToken token, Func<SyntaxToken, SemanticModel, CSharpSimplifierOptions, CancellationToken, SyntaxToken> simplifier)
            {
                RequireInitialized();

                this.CancellationToken.ThrowIfCancellationRequested();

                return token.HasAnnotation(Simplifier.Annotation)
                    ? simplifier(token, this.SemanticModel, this.Options, this.CancellationToken)
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
