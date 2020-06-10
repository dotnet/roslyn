// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification
{
    internal class OperatorOverloadSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(AssignmentExpressionSyntax),
            typeof(BinaryExpressionSyntax),
            typeof(PrefixUnaryExpressionSyntax),
            typeof(PostfixUnaryExpressionSyntax));

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
                var operatorSpan = GetOperatorTokenSpan(syntax);
                if (!operatorSpan.IsEmpty)
                {
                    result.Add(new ClassifiedSpan(operatorSpan, ClassificationTypeNames.OperatorOverloaded));
                }
            }
        }

        private static TextSpan GetOperatorTokenSpan(SyntaxNode syntax)
            => syntax switch
            {
                AssignmentExpressionSyntax assignmentExpression => assignmentExpression.OperatorToken.Span,
                BinaryExpressionSyntax binaryExpression => binaryExpression.OperatorToken.Span,
                PrefixUnaryExpressionSyntax prefixUnaryExpression => prefixUnaryExpression.OperatorToken.Span,
                PostfixUnaryExpressionSyntax postfixUnaryExpression => postfixUnaryExpression.OperatorToken.Span,
                _ => default,
            };
    }
}
