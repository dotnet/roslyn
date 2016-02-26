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
        ArrayBuilder<ExpressionSyntax> expressionsToVisit = ArrayBuilder<ExpressionSyntax>.GetInstance();
        internal static ArrayBuilder<DeclarationPatternSyntax> FindPatternVariables(
            ExpressionSyntax expression = null,
            ImmutableArray<ExpressionSyntax> expressions = default(ImmutableArray<ExpressionSyntax>),
            ImmutableArray<PatternSyntax> patterns = default(ImmutableArray<PatternSyntax>))
        {
            var finder = s_poolInstance.Allocate();
            finder.declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();

            // push expressions to be visited onto a stack
            var expressionsToVisit = finder.expressionsToVisit;
            if (expression != null) expressionsToVisit.Add(expression);
            if (!expressions.IsDefaultOrEmpty)
            {
                foreach (var subExpression in expressions)
                {
                    if (subExpression != null) expressionsToVisit.Add(subExpression);
                }
            }
            finder.VisitExpressions();

            if (!patterns.IsDefaultOrEmpty)
            {
                foreach (var pattern in patterns)
                {
                    finder.Visit(pattern);
                }
            }

            var result = finder.declarationPatterns;
            finder.declarationPatterns = null;
            Debug.Assert(finder.expressionsToVisit.Count == 0);
            s_poolInstance.Free(finder);
            return result;
        }

        private void VisitExpressions()
        {
            while (expressionsToVisit.Count != 0)
            {
                var e = expressionsToVisit[expressionsToVisit.Count - 1];
                expressionsToVisit.RemoveLast();
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
        public override void VisitQueryExpression(QueryExpressionSyntax node) { }
        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            expressionsToVisit.Add(node.Left);
            expressionsToVisit.Add(node.Right);
        }
        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            expressionsToVisit.Add(node.Operand);
        }
        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            expressionsToVisit.Add(node.Operand);
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
