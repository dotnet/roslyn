// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle
{
    internal static class TypeStyleHelper
    {
        public static bool IsBuiltInType(ITypeSymbol type)
            => type?.IsSpecialType() == true;

        public static bool IsImplicitStylePreferred(
            OptionSet optionSet, bool isBuiltInTypeContext, bool isTypeApparentContext, bool isTypeExplicitContext)
        {
            return IsImplicitStylePreferred(
                GetCurrentTypeStylePreferences(optionSet),
                isBuiltInTypeContext,
                isTypeApparentContext,
                isTypeExplicitContext);
        }

        public static bool IsImplicitStylePreferred(
            UseVarPreference stylePreferences,
            bool isBuiltInTypeContext,
            bool isTypeApparentContext,
            bool isTypeExplicitContext)
        {
            if (isBuiltInTypeContext)
            {
                return stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }

            if (isTypeApparentContext &&
                stylePreferences.HasFlag(UseVarPreference.WhenTypeIsApparent))
            {
                return true;
            }

            if (isTypeExplicitContext &&
                stylePreferences.HasFlag(UseVarPreference.WhenTypeIsExplicit))
            {
                return true;
            }

            return stylePreferences.HasFlag(UseVarPreference.Elsewhere);
        }

        public static bool IsExplicitStylePreferred(
            UseVarPreference stylePreferences,
            bool isBuiltInTypeContext,
            bool isTypeApparentContext,
            bool isTypeExplicitContext)
        {
            if (isBuiltInTypeContext)
            {
                return !stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }

            if (isTypeApparentContext &&
                !stylePreferences.HasFlag(UseVarPreference.WhenTypeIsApparent))
            {
                return true;
            }

            if (isTypeExplicitContext &&
                !stylePreferences.HasFlag(UseVarPreference.WhenTypeIsExplicit))
            {
                return true;
            }

            return !stylePreferences.HasFlag(UseVarPreference.Elsewhere);
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
            UseVarPreference stylePreferences,
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            ITypeSymbol typeInDeclaration,
            CancellationToken cancellationToken)
        {
            // tuple literals
            if (initializerExpression.IsKind(SyntaxKind.TupleExpression))
            {
                var tuple = (TupleExpressionSyntax)initializerExpression;
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

            if (TypeFoundInGenericMethodCall(
                    semanticModel, typeInDeclaration, memberName, methodSymbol, cancellationToken))
            {
                return true;
            }

            if (memberName.IsRightSideOfDot())
            {
                var containingTypeName = memberName.GetLeftSideOfDot();
                return IsPossibleCreationOrConversionMethod(
                    methodSymbol, typeInDeclaration, semanticModel,
                    containingTypeName, cancellationToken);
            }

            return false;
        }

        public static bool IsTypeExplicitInAssignmentExpression(
            UseVarPreference stylePreferences,
            ExpressionSyntax initializerExpression,
            SemanticModel semanticModel,
            ITypeSymbol typeInDeclaration,
            CancellationToken cancellationToken)
        {
            if (typeInDeclaration == null)
            {
                // if we don't have a type on the left of the assignment, there's no way
                // for us to validate that it has been explicitly stated on the right.
                return false;
            }

            // If we've got a tuple expr, then we consider the type explicit as long as all
            // the exprs in the tuple are explicit.  i.e. if we have `((int)a, (int)b)`, then
            // that is considered an explicit use of the `(int, int)` type.  

            // tuple literals
            if (initializerExpression.IsKind(SyntaxKind.TupleExpression))
            {
                var tuple = (TupleExpressionSyntax)initializerExpression;
                if (!typeInDeclaration.IsTupleType)
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

                    if (!IsTypeExplicitInAssignmentExpression(
                            stylePreferences, argument.Expression, semanticModel, tupleElementType, cancellationToken))
                    {
                        return false;
                    }
                }

                return true;
            }

            // default(type)
            if (initializerExpression is DefaultExpressionSyntax defaultExpression)
            {
                return typeInDeclaration.Equals(
                    semanticModel.GetTypeInfo(defaultExpression.Type, cancellationToken).Type);
            }

            // we consider any usage of a literal to be a way of explicitly stating the type.
            // i.e. if we have "true", then it's effectively explicit that the type is "boolean".
            //
            // Note: we do not do this if the user has said they do not want var for built-in
            // types. This is effectively an override that we should always respect.
            if (initializerExpression.IsAnyLiteralExpression())
            {
                return stylePreferences.HasFlag(UseVarPreference.ForBuiltInTypes);
            }

            // constructor invocations cases:
            //      = new type();
            if (initializerExpression is ObjectCreationExpressionSyntax objectCreation)
            {
                return typeInDeclaration.Equals(
                    semanticModel.GetTypeInfo(objectCreation.Type, cancellationToken).Type);
            }

            // array creation, if it's not an implicit array:
            if (initializerExpression is ArrayCreationExpressionSyntax arrayCreation)
            {
                return typeInDeclaration.Equals(
                    semanticModel.GetTypeInfo(arrayCreation, cancellationToken).Type);
            }

            // explicit conversion cases: 
            //      (type)expr, expr as type
            if (initializerExpression is CastExpressionSyntax castExpression)
            {
                return typeInDeclaration.Equals(
                    semanticModel.GetTypeInfo(castExpression.Type, cancellationToken).Type);
            }

            if (initializerExpression.IsKind(SyntaxKind.AsExpression))
            {
                return typeInDeclaration.Equals(
                    semanticModel.GetTypeInfo(((BinaryExpressionSyntax)initializerExpression).Right, cancellationToken).Type);
            }

            // See if we have a method call, like  `X(...)` or `Y.X(...)`.
            //
            // If we have the former, then we would consider the type explicit if it showed up
            // like `X<TheType>(...)`.
            //
            // If we have the latter, we'll consider the type explicit if the left side 
            // is the same as our declared type, and the right side is a static method
            // that returns an instance of that type.  This is effectively a factory
            // method like `int.Parse` and the type is considered explicit enough in that 
            // case.

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

            if (TypeFoundInGenericMethodCall(
                    semanticModel, typeInDeclaration, memberName, methodSymbol, cancellationToken))
            {
                return true;
            }

            if (!memberName.IsRightSideOfDot())
            {
                return false;
            }

            var containingTypeName = memberName.GetLeftSideOfDot();
            if (!typeInDeclaration.Equals(semanticModel.GetTypeInfo(containingTypeName, cancellationToken).Type))
            {
                return false;
            }

            if (methodSymbol.IsStatic)
            {
                // something of the form Type.FactoryMethod.  The type is considered explicit here.
                return typeInDeclaration.Equals(methodSymbol.ReturnType);
            }

            // Everything else we consider not explicit.

            // TODO:
            // 1. is the type considered 'explicit' if you have an `x is T` expression?  
            //    it could be said that it's fairly explicit that this is boolean.
            //
            // 2. If you have something like `.ToString` and the type is 'String' (without
            //    generics), should this be considered 'explicit'?
            return false;
        }

        private static bool TypeFoundInGenericMethodCall(
            SemanticModel semanticModel, ITypeSymbol typeInDeclaration,
            SimpleNameSyntax memberName, IMethodSymbol method, CancellationToken cancellationToken)
        {
            if (!typeInDeclaration.Equals(method.ReturnType))
            {
                return false;
            }

            if (memberName is GenericNameSyntax genericName)
            {
                // if we have a call like `.GetService<SomeType>()`
                // the type is considered explicit if 'SomeType' matches.
                foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
                {
                    if (typeInDeclaration.Equals(semanticModel.GetTypeInfo(typeArgument, cancellationToken).Type))
                    {
                        return true;
                    }
                }
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
                || IsPossibleConversionMethod(methodSymbol);
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

        private static UseVarPreference GetCurrentTypeStylePreferences(OptionSet optionSet)
        {
            var stylePreferences = UseVarPreference.None;

            var styleForIntrinsicTypes = optionSet.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes);
            var styleForApparent = optionSet.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent);
            var styleForElsewhere = optionSet.GetOption(CSharpCodeStyleOptions.VarElsewhere);
            var styleForExplicit = optionSet.GetOption(CSharpCodeStyleOptions.VarWhenTypeIsApparent);

            if (styleForIntrinsicTypes.Value)
            {
                stylePreferences |= UseVarPreference.ForBuiltInTypes;
            }

            if (styleForApparent.Value)
            {
                stylePreferences |= UseVarPreference.WhenTypeIsApparent;
            }

            if (styleForElsewhere.Value)
            {
                stylePreferences |= UseVarPreference.Elsewhere;
            }

            if (styleForExplicit.Value)
            {
                stylePreferences |= UseVarPreference.WhenTypeIsExplicit;
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
