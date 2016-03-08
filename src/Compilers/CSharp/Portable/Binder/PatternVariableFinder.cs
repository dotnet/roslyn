// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    class PatternVariableFinder : CSharpSyntaxWalker
    {
        ArrayBuilder<DeclarationPatternSyntax> declarationPatterns;
        ArrayBuilder<CSharpSyntaxNode> nodesToVisit = ArrayBuilder<CSharpSyntaxNode>.GetInstance();
        internal static ArrayBuilder<DeclarationPatternSyntax> FindPatternVariables(
            CSharpSyntaxNode node = null,
            ImmutableArray<CSharpSyntaxNode> nodes = default(ImmutableArray<CSharpSyntaxNode>))
        {
            var finder = s_poolInstance.Allocate();
            finder.declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();

            // push nodes to be visited onto a stack
            var nodesToVisit = finder.nodesToVisit;
            if (node != null) nodesToVisit.Add(node);
            if (!nodes.IsDefaultOrEmpty)
            {
                foreach (var subExpression in nodes)
                {
                    if (subExpression != null) nodesToVisit.Add(subExpression);
                }
            }

            nodesToVisit.ReverseContents();

            finder.VisitNodes();

            var result = finder.declarationPatterns;
            finder.declarationPatterns = null;
            Debug.Assert(finder.nodesToVisit.Count == 0);
            s_poolInstance.Free(finder);
            return result;
        }

        private void VisitNodes()
        {
            while (nodesToVisit.Count != 0)
            {
                var e = nodesToVisit[nodesToVisit.Count - 1];
                nodesToVisit.RemoveLast();
                Visit(e);
            }
        }

        public override void VisitDeclarationPattern(DeclarationPatternSyntax node)
        {
            declarationPatterns.Add(node);
            base.VisitDeclarationPattern(node);
        }
        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) { }
        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) { }
        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) { }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            // Variables declared in [in] expressions of top level from clause and 
            // join clauses are in scope 
            Visit(node.FromClause.Expression);

            foreach (var clause in node.Body.Clauses)
            {
                if (clause.Kind() == SyntaxKind.JoinClause)
                {
                    Visit(((JoinClauseSyntax)clause).InExpression);
                }
            }
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            nodesToVisit.Add(node.Right);
            nodesToVisit.Add(node.Left);
        }
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            nodesToVisit.Add(node.Operand);
        }
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            nodesToVisit.Add(node.Operand);
        }
        public override void VisitMatchExpression(MatchExpressionSyntax node)
        {
            Visit(node.Left);
        }

        #region pool
        private static readonly ObjectPool<PatternVariableFinder> s_poolInstance = CreatePool();

        public static ObjectPool<PatternVariableFinder> CreatePool()
        {
            return new ObjectPool<PatternVariableFinder>(() => new PatternVariableFinder(), 10);
        }
        #endregion
    }
}
