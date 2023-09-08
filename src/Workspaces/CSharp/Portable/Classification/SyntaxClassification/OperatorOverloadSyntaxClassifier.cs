// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            SyntaxNode syntax,
            TextSpan textSpan,
            SemanticModel semanticModel,
            ClassificationOptions options,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (syntax.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return;
            }

            var operatorSpan = GetOperatorTokenSpan(syntax);
            if (!operatorSpan.IsEmpty && operatorSpan.IntersectsWith(textSpan))
            {
                var symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken);
                if (symbolInfo.Symbol is IMethodSymbol methodSymbol
                    && methodSymbol.MethodKind == MethodKind.UserDefinedOperator)
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
