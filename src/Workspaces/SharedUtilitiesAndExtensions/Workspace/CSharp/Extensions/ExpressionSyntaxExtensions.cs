﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static partial class ExpressionSyntaxExtensions
    {
        public static ExpressionSyntax Parenthesize(
            this ExpressionSyntax expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            // a 'ref' expression should never be parenthesized.  It fundamentally breaks the code.
            // This is because, from the language's perspective there is no such thing as a ref
            // expression.  instead, there are constructs like ```return ref expr``` or 
            // ```x ? ref expr1 : ref expr2```, or ```ref int a = ref expr``` in these cases, the 
            // ref's do not belong to the exprs, but instead belong to the parent construct. i.e.
            // ```return ref``` or ``` ? ref  ... : ref ... ``` or ``` ... = ref ...```.  For 
            // parsing convenience, and to prevent having to update all these constructs, we settled
            // on a ref-expression node.  But this node isn't a true expression that be operated
            // on like with everything else.
            if (expression.IsKind(SyntaxKind.RefExpression))
            {
                return expression;
            }

            var result = ParenthesizeWorker(expression, includeElasticTrivia);
            return addSimplifierAnnotation
                ? result.WithAdditionalAnnotations(Simplifier.Annotation)
                : result;
        }

        private static ExpressionSyntax ParenthesizeWorker(
            this ExpressionSyntax expression, bool includeElasticTrivia)
        {
            var withoutTrivia = expression.WithoutTrivia();
            var parenthesized = includeElasticTrivia
                ? SyntaxFactory.ParenthesizedExpression(withoutTrivia)
                : SyntaxFactory.ParenthesizedExpression(
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    withoutTrivia,
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty));

            return parenthesized.WithTriviaFrom(expression);
        }

        public static CastExpressionSyntax Cast(
            this ExpressionSyntax expression,
            ITypeSymbol targetType)
        {
            return SyntaxFactory.CastExpression(
                type: targetType.GenerateTypeSyntax(),
                expression: expression.Parenthesize())
                .WithAdditionalAnnotations(Simplifier.Annotation);
        }

        /// <summary>
        /// Adds to <paramref name="targetType"/> if it does not contain an anonymous
        /// type and binds to the same type at the given <paramref name="position"/>.
        /// </summary>
        public static ExpressionSyntax CastIfPossible(
            this ExpressionSyntax expression,
            ITypeSymbol targetType,
            int position,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (targetType.ContainsAnonymousType())
            {
                return expression;
            }

            if (targetType.Kind == SymbolKind.DynamicType)
            {
                targetType = semanticModel.Compilation.GetSpecialType(SpecialType.System_Object);
            }

            var typeSyntax = targetType.GenerateTypeSyntax();
            var type = semanticModel.GetSpeculativeTypeInfo(
                position,
                typeSyntax,
                SpeculativeBindingOption.BindAsTypeOrNamespace).Type;

            if (!targetType.Equals(type))
            {
                return expression;
            }

            var castExpression = expression.Cast(targetType);

            // Ensure that inserting the cast doesn't change the semantics.
            var specAnalyzer = new SpeculationAnalyzer(expression, castExpression, semanticModel, cancellationToken);
            var speculativeSemanticModel = specAnalyzer.SpeculativeSemanticModel;
            if (speculativeSemanticModel == null)
            {
                return expression;
            }

            var speculatedCastExpression = (CastExpressionSyntax)specAnalyzer.ReplacedExpression;
            if (!speculatedCastExpression.IsUnnecessaryCast(speculativeSemanticModel, cancellationToken))
            {
                return expression;
            }

            return castExpression;
        }
    }
}

