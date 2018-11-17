// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class OperatorOverloadSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override void AddClassifications(
            Workspace workspace,
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken);
            if (symbolInfo.Symbol is IMethodSymbol methodSymbol
                && methodSymbol.MethodKind == MethodKind.UserDefinedOperator)
            {
                if (syntax is BinaryExpressionSyntax binaryExpression)
                {
                    result.Add(new ClassifiedSpan(binaryExpression.OperatorToken.Span, ClassificationTypeNames.OperatorOverload));
                }
                else if (syntax is PrefixUnaryExpressionSyntax prefixUnaryExpression)
                {
                    result.Add(new ClassifiedSpan(prefixUnaryExpression.OperatorToken.Span, ClassificationTypeNames.OperatorOverload));
                }
                else if (syntax is PostfixUnaryExpressionSyntax postfixUnaryExpression)
                {
                    result.Add(new ClassifiedSpan(postfixUnaryExpression.OperatorToken.Span, ClassificationTypeNames.OperatorOverload));
                }
                else if (syntax is ConditionalExpressionSyntax conditionalExpression)
                {
                    result.Add(new ClassifiedSpan(conditionalExpression.Condition.Span, ClassificationTypeNames.OperatorOverload));
                }
            }
        }

        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(BinaryExpressionSyntax), 
            typeof(PrefixUnaryExpressionSyntax), 
            typeof(PostfixUnaryExpressionSyntax),
            typeof(ConditionalExpressionSyntax));
    }
}
