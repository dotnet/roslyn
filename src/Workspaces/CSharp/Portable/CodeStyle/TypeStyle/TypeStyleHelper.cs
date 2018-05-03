// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle
{
    internal static class TypeStyleHelper
    {
        /// <summary>
        /// Given an expression or assignment, answers whether the declaration can use var keyword,
        /// by looking at the user's style preferences obtained from options and the context
        /// obtained from the expression.
        /// </summary>
        public static bool IsImplicitTypePreferred(
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            var stylePreferences = GetCurrentTypeStylePreferences(optionSet);

            var isBuiltInTypeContext = IsBuiltInType(initializerExpression, semanticModel, cancellationToken);

            var isTypeApparentContext = IsTypeApparentInAssignmentExpression(
                stylePreferences, initializerExpression, semanticModel, cancellationToken);

            return IsImplicitStylePreferred(stylePreferences, isBuiltInTypeContext, isTypeApparentContext);
        }

        /// <summary>
        /// Given an expression of assignment, answers if its type is
        /// considered an intrinsic predefined type.
        /// </summary>
        private static bool IsBuiltInType(
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken) =>
                IsBuiltInType(semanticModel.GetTypeInfo(initializerExpression, cancellationToken).Type);

        public static bool IsBuiltInType(ITypeSymbol type)
            => type?.IsSpecialType() == true;

        public static bool IsImplicitStylePreferred(
            OptionSet optionSet, bool isBuiltInTypeContext, bool isTypeApparentContext)
        {
            return IsImplicitStylePreferred(
                GetCurrentTypeStylePreferences(optionSet),
                isBuiltInTypeContext,
                isTypeApparentContext);
        }

        private static bool IsImplicitStylePreferred(
            TypeStylePreference stylePreferences, bool isBuiltInTypeContext, bool isTypeApparentContext)
        {
            return isBuiltInTypeContext
                    ? stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeForIntrinsicTypes)
                    : isTypeApparentContext
                        ? stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWhereApparent)
                        : stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeWherePossible);
        }

        /// <summary>
        /// Analyzes if type information is obvious to the reader by simply looking at the assignment expression.
        /// </summary>
        /// <remarks>
        /// <paramref name="typeInDeclaration"/> accepts null, to be able to cater to codegen features
        /// that are about to generate a local declaration and do not have this information to pass in.
        /// Things (like analyzers) that do have a local declaration already, should pass this in.
        /// </remarks>
        public static bool IsTypeApparentInAssignmentExpression(
            TypeStylePreference stylePreferences,
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            ITypeSymbol typeInDeclaration = null)
        {
            // tuple literals
            if (initializerExpression.IsKind(SyntaxKind.TupleExpression))
            {
                var tuple = (TupleExpressionSyntax)initializerExpression;
                foreach (var argument in tuple.Arguments)
                {
                    if (!IsTypeApparentInAssignmentExpression(stylePreferences, argument.Expression, semanticModel, cancellationToken, typeInDeclaration))
                    {
                        return false;
                    }
                }

                return true;
            }

            // default(type)
            if (initializerExpression.IsKind(SyntaxKind.DefaultExpression))
            {
                return true;
            }

            // literals, use var if options allow usage here.
            if (initializerExpression.IsAnyLiteralExpression())
            {
                return stylePreferences.HasFlag(TypeStylePreference.ImplicitTypeForIntrinsicTypes);
            }

            // constructor invocations cases:
            //      = new type();
            if (initializerExpression.IsKind(SyntaxKind.ObjectCreationExpression, SyntaxKind.ArrayCreationExpression) &&
                !initializerExpression.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                return true;
            }

            // explicit conversion cases: 
            //      (type)expr, expr is type, expr as type
            if (initializerExpression.IsKind(SyntaxKind.CastExpression) ||
                initializerExpression.IsKind(SyntaxKind.IsExpression) ||
                initializerExpression.IsKind(SyntaxKind.AsExpression))
            {
                return true;
            }

            // other Conversion cases:
            //      a. conversion with helpers like: int.Parse methods
            //      b. types that implement IConvertible and then invoking .ToType()
            //      c. System.Convert.ToType()
            var memberName = GetRightmostInvocationExpression(initializerExpression).GetRightmostName();
            if (memberName == null)
            {
                return false;
            }

            var methodSymbol = semanticModel.GetSymbolInfo(memberName, cancellationToken).Symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return false;
            }

            if (memberName.IsRightSideOfDot())
            {
                var containingTypeName = memberName.GetLeftSideOfDot();
                return IsPossibleCreationOrConversionMethod(methodSymbol, typeInDeclaration, semanticModel, containingTypeName, cancellationToken);
            }

            return false;
        }

        private static bool IsPossibleCreationOrConversionMethod(IMethodSymbol methodSymbol,
            ITypeSymbol typeInDeclaration,
            SemanticModel semanticModel,
            ExpressionSyntax containingTypeName,
            CancellationToken cancellationToken)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return false;
            }

            var containingType = semanticModel.GetTypeInfo(containingTypeName, cancellationToken).Type;

            return IsPossibleCreationMethod(methodSymbol, typeInDeclaration, containingType)
                || IsPossibleConversionMethod(methodSymbol, typeInDeclaration, containingType, semanticModel, cancellationToken);
        }

        /// <summary>
        /// Looks for types that have static methods that return the same type as the container.
        /// e.g: int.Parse, XElement.Load, Tuple.Create etc.
        /// </summary>
        private static bool IsPossibleCreationMethod(IMethodSymbol methodSymbol,
            ITypeSymbol typeInDeclaration,
            ITypeSymbol containingType)
        {
            if (!methodSymbol.IsStatic)
            {
                return false;
            }

            return IsContainerTypeEqualToReturnType(methodSymbol, typeInDeclaration, containingType);
        }

        /// <summary>
        /// If we have a method ToXXX and its return type is also XXX, then type name is apparent
        /// e.g: Convert.ToString.
        /// </summary>
        private static bool IsPossibleConversionMethod(IMethodSymbol methodSymbol,
            ITypeSymbol typeInDeclaration,
            ITypeSymbol containingType,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var returnType = methodSymbol.ReturnType;
            var returnTypeName = returnType.IsNullable()
                ? returnType.GetTypeArguments().First().Name
                : returnType.Name;

            return methodSymbol.Name.Equals("To" + returnTypeName, StringComparison.Ordinal);
        }

        /// <remarks>
        /// If there are type arguments on either side of assignment, we match type names instead of type equality 
        /// to account for inferred generic type arguments.
        /// e.g: Tuple.Create(0, true) returns Tuple&lt;X,y&gt; which isn't the same as type Tuple.
        /// otherwise, we match for type equivalence
        /// </remarks>
        private static bool IsContainerTypeEqualToReturnType(IMethodSymbol methodSymbol,
            ITypeSymbol typeInDeclaration,
            ITypeSymbol containingType)
        {
            var returnType = UnwrapTupleType(methodSymbol.ReturnType);

            if (UnwrapTupleType(typeInDeclaration)?.GetTypeArguments().Length > 0 ||
                containingType.GetTypeArguments().Length > 0)
            {
                return UnwrapTupleType(containingType).Name.Equals(returnType.Name);
            }
            else
            {
                return UnwrapTupleType(containingType).Equals(returnType);
            }
        }

        private static ITypeSymbol UnwrapTupleType(ITypeSymbol symbol)
        {
            if (symbol is null)
                return null;

            if (!(symbol is INamedTypeSymbol namedTypeSymbol))
                return symbol;

            return namedTypeSymbol.TupleUnderlyingType ?? symbol;
        }

        private static ExpressionSyntax GetRightmostInvocationExpression(ExpressionSyntax node)
        {
            if (node is AwaitExpressionSyntax awaitExpression && awaitExpression.Expression != null)
            {
                return GetRightmostInvocationExpression(awaitExpression.Expression);
            }

            if (node is InvocationExpressionSyntax invocationExpression && invocationExpression.Expression != null)
            {
                return GetRightmostInvocationExpression(invocationExpression.Expression);
            }

            if (node is ConditionalAccessExpressionSyntax conditional)
            {
                return GetRightmostInvocationExpression(conditional.WhenNotNull);
            }

            return node;
        }

        private static TypeStylePreference GetCurrentTypeStylePreferences(OptionSet optionSet)
        {
            var stylePreferences = TypeStylePreference.None;

            var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes);
            var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWhereApparent);
            var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.UseImplicitTypeWherePossible);

            if (styleForIntrinsicTypes.Value)
            {
                stylePreferences |= TypeStylePreference.ImplicitTypeForIntrinsicTypes;
            }

            if (styleForApparent.Value)
            {
                stylePreferences |= TypeStylePreference.ImplicitTypeWhereApparent;
            }

            if (styleForElsewhere.Value)
            {
                stylePreferences |= TypeStylePreference.ImplicitTypeWherePossible;
            }

            return stylePreferences;
        }

        public static bool IsPredefinedType(TypeSyntax type)
        {
            var predefinedType = type as PredefinedTypeSyntax;

            return predefinedType != null
                ? SyntaxFacts.IsPredefinedType(predefinedType.Keyword.Kind())
                : false;
        }
    }
}
