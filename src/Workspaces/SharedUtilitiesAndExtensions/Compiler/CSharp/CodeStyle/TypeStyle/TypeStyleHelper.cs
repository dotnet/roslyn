// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle
{
    internal static class TypeStyleHelper
    {
        public static bool IsBuiltInType(ITypeSymbol type)
            => type?.IsSpecialType() == true;

        /// <summary>
        /// Analyzes if type information is obvious to the reader by simply looking at the assignment expression.
        /// </summary>
        /// <remarks>
        /// <paramref name="typeInDeclaration"/> accepts null, to be able to cater to codegen features
        /// that are about to generate a local declaration and do not have this information to pass in.
        /// Things (like analyzers) that do have a local declaration already, should pass this in.
        /// </remarks>
        public static bool IsTypeApparentInAssignmentExpression(
            UseVarPreference stylePreferences,
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            ITypeSymbol? typeInDeclaration,
            CancellationToken cancellationToken)
        {
            // tuple literals
            if (initializerExpression is TupleExpressionSyntax tuple)
            {
                if (typeInDeclaration == null || !typeInDeclaration.IsTupleType)
                {
                    return false;
                }

                var tupleType = (INamedTypeSymbol)typeInDeclaration;
                if (tupleType.TupleElements.Length != tuple.Arguments.Count)
                {
                    return false;
                }

                for (int i = 0, n = tuple.Arguments.Count; i < n; i++)
                {
                    var argument = tuple.Arguments[i];
                    var tupleElementType = tupleType.TupleElements[i].Type;

                    if (!IsTypeApparentInAssignmentExpression(
                            stylePreferences, argument.Expression, semanticModel, tupleElementType, cancellationToken))
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
                return stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }

            // constructor invocations cases:
            //      = new type();
            if (initializerExpression.Kind() is SyntaxKind.ObjectCreationExpression or SyntaxKind.ArrayCreationExpression &&
                !initializerExpression.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
            {
                return true;
            }

            // explicit conversion cases: 
            //      (type)expr, expr is type, expr as type
            if (initializerExpression.Kind() is SyntaxKind.CastExpression or SyntaxKind.IsExpression or SyntaxKind.AsExpression)
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

            if (semanticModel.GetSymbolInfo(memberName, cancellationToken).Symbol is not IMethodSymbol methodSymbol)
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
            ITypeSymbol? typeInDeclaration,
            SemanticModel semanticModel,
            ExpressionSyntax containingTypeName,
            CancellationToken cancellationToken)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return false;
            }

            var containingType = semanticModel.GetTypeInfo(containingTypeName, cancellationToken).Type;

            // The containing type was determined from an expression of the form ContainingType.MemberName, and the
            // caller verifies that MemberName resolves to a method symbol.
            Contract.ThrowIfNull(containingType);

            return IsPossibleCreationMethod(methodSymbol, typeInDeclaration, containingType)
                || IsPossibleConversionMethod(methodSymbol);
        }

        /// <summary>
        /// Looks for types that have static methods that return the same type as the container.
        /// e.g: int.Parse, XElement.Load, Tuple.Create etc.
        /// </summary>
        private static bool IsPossibleCreationMethod(IMethodSymbol methodSymbol,
            ITypeSymbol? typeInDeclaration,
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
        private static bool IsPossibleConversionMethod(IMethodSymbol methodSymbol)
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
            ITypeSymbol? typeInDeclaration,
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

        [return: NotNullIfNotNull(nameof(symbol))]
        private static ITypeSymbol? UnwrapTupleType(ITypeSymbol? symbol)
        {
            if (symbol is null)
                return null;

            if (symbol is not INamedTypeSymbol namedTypeSymbol)
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

        public static bool IsPredefinedType(TypeSyntax type)
        {
            return type is PredefinedTypeSyntax predefinedType
                ? SyntaxFacts.IsPredefinedType(predefinedType.Keyword.Kind())
                : false;
        }
    }
}
