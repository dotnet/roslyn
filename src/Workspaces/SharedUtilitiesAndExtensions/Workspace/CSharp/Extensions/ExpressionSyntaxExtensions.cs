// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
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

            // Throw expressions are not permitted to be parenthesized:
            //
            //     "a" ?? throw new ArgumentNullException()
            //
            // is legal whereas
            //
            //     "a" ?? (throw new ArgumentNullException())
            //
            // is not.
            if (expression.IsKind(SyntaxKind.ThrowExpression))
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

        public static PatternSyntax Parenthesize(
            this PatternSyntax pattern, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true)
        {
            var withoutTrivia = pattern.WithoutTrivia();
            var parenthesized = includeElasticTrivia
                ? SyntaxFactory.ParenthesizedPattern(withoutTrivia)
                : SyntaxFactory.ParenthesizedPattern(
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.OpenParenToken, SyntaxTriviaList.Empty),
                    withoutTrivia,
                    SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.CloseParenToken, SyntaxTriviaList.Empty));

            var result = parenthesized.WithTriviaFrom(pattern);
            return addSimplifierAnnotation
                ? result.WithAdditionalAnnotations(Simplifier.Annotation)
                : result;
        }

        public static CastExpressionSyntax Cast(
            this ExpressionSyntax expression,
            ITypeSymbol targetType)
        {
            var parenthesized = expression.Parenthesize();
            var castExpression = SyntaxFactory.CastExpression(
                targetType.GenerateTypeSyntax(), parenthesized.WithoutTrivia()).WithTriviaFrom(parenthesized);

            return castExpression.WithAdditionalAnnotations(Simplifier.Annotation);
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
                return expression;

            if (targetType.IsSystemVoid())
                return expression;

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
            if (!CastSimplifier.IsUnnecessaryCast(speculatedCastExpression, speculativeSemanticModel, cancellationToken))
            {
                return expression;
            }

            return castExpression;
        }
        /// <summary>
        /// DeterminesCheck if we're in an interesting situation like this:
        /// <code>
        ///     int i = 5;
        ///     i.          // -- here
        ///     List ml = new List();
        /// </code>
        /// The problem is that "i.List" gets parsed as a type.  In this case we need to try binding again as if "i" is
        /// an expression and not a type.  In order to do that, we need to speculate as to what 'i' meant if it wasn't
        /// part of a local declaration's type.
        /// <para/>
        /// Another interesting case is something like:
        /// <code>
        ///      stringList.
        ///      await Test2();
        /// </code>
        /// Here "stringList.await" is thought of as the return type of a local function.
        /// </summary>
        public static bool ShouldNameExpressionBeTreatedAsExpressionInsteadOfType(
            this ExpressionSyntax name,
            SemanticModel semanticModel,
            out SymbolInfo leftHandBinding,
            out ITypeSymbol? container)
        {
            if (name.IsFoundUnder<LocalFunctionStatementSyntax>(d => d.ReturnType) ||
                name.IsFoundUnder<LocalDeclarationStatementSyntax>(d => d.Declaration.Type) ||
                name.IsFoundUnder<FieldDeclarationSyntax>(d => d.Declaration.Type))
            {
                leftHandBinding = semanticModel.GetSpeculativeSymbolInfo(
                    name.SpanStart, name, SpeculativeBindingOption.BindAsExpression);

                container = semanticModel.GetSpeculativeTypeInfo(
                    name.SpanStart, name, SpeculativeBindingOption.BindAsExpression).Type;
                return true;
            }

            leftHandBinding = default;
            container = null;
            return false;
        }
    }
}

