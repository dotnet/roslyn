// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    class PatternVariableFinder : CSharpSyntaxWalker
    {
        ArrayBuilder<DeclarationPatternSyntax> declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();
        internal static ArrayBuilder<DeclarationPatternSyntax> FindPatternVariables(
            ExpressionSyntax expression = null,
            ImmutableArray<ExpressionSyntax> expressions = default(ImmutableArray<ExpressionSyntax>),
            ImmutableArray<PatternSyntax> patterns = default(ImmutableArray<PatternSyntax>))
        {
            var finder = s_poolInstance.Allocate();
            finder.declarationPatterns = ArrayBuilder<DeclarationPatternSyntax>.GetInstance();
            finder.Visit(expression);
            if (!expressions.IsDefaultOrEmpty)
            {
                foreach (var subExpression in expressions)
                {
                    if (subExpression != null) finder.Visit(subExpression);
                }
            }
            if (!patterns.IsDefaultOrEmpty)
            {
                foreach (var pattern in patterns)
                {
                    finder.Visit(pattern);
                }
            }

            var result = finder.declarationPatterns;
            finder.declarationPatterns = null;
            s_poolInstance.Free(finder);
            return result;
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
