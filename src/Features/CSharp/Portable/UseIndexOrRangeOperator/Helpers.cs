// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOrRangeOperator
{
    internal static class Helpers
    {
        /// <summary>
        /// Find an `int MyType.Count` or `int MyType.Length` property.
        /// </summary>
        public static IPropertySymbol TryGetLengthOrCountProperty(ITypeSymbol namedType)
            => TryGetNoArgInt32Property(namedType, nameof(string.Length)) ??
               TryGetNoArgInt32Property(namedType, nameof(ICollection.Count));

        /// <summary>
        /// Tried to find a public, non-static, int-returning property in the given type with the
        /// specified <paramref name="name"/>.
        /// </summary>
        public static IPropertySymbol TryGetNoArgInt32Property(ITypeSymbol type, string name)
            => type.GetMembers(name)
                   .OfType<IPropertySymbol>()
                   .Where(p => IsPublicInstance(p) &&
                               p.Type.SpecialType == SpecialType.System_Int32)
                   .FirstOrDefault();

        public static bool IsPublicInstance(ISymbol symbol)
            => symbol is { IsStatic: false, DeclaredAccessibility: Accessibility.Public };

        /// <summary>
        /// Creates an `^expr` index expression from a given `expr`.
        /// </summary>
        public static PrefixUnaryExpressionSyntax IndexExpression(ExpressionSyntax expr)
            => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.IndexExpression,
                expr.Parenthesize());

        /// <summary>
        /// Checks if this <paramref name="operation"/> is `expr.Length` where `expr` is equivalent
        /// to the <paramref name="instance"/> we were originally invoking an accessor/method off
        /// of.
        /// </summary>
        public static bool IsInstanceLengthCheck(IPropertySymbol lengthLikeProperty, IOperation instance, IOperation operation)
            => operation is IPropertyReferenceOperation propertyRef &&
               propertyRef.Instance != null &&
               lengthLikeProperty.Equals(propertyRef.Property) &&
               CSharpSyntaxFactsService.Instance.AreEquivalent(instance.Syntax, propertyRef.Instance.Syntax);

        /// <summary>
        /// Checks if <paramref name="operation"/> is a binary subtraction operator. If so, it
        /// will be returned through <paramref name="subtraction"/>.
        /// </summary>
        public static bool IsSubtraction(IOperation operation, out IBinaryOperation subtraction)
        {
            if (operation is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract)
            {
                subtraction = binaryOperation;
                return true;
            }

            subtraction = null;
            return false;
        }

        /// <summary>
        /// Look for methods like "SomeType MyType.Get(int)".  Also matches against the 'getter'
        /// of an indexer like 'SomeType MyType.this[int]`
        /// </summary>
        public static bool IsIntIndexingMethod(IMethodSymbol method)
            => method != null &&
               (method.MethodKind == MethodKind.PropertyGet || method.MethodKind == MethodKind.Ordinary) &&
               IsPublicInstance(method) &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].Type.SpecialType == SpecialType.System_Int32;

        /// <summary>
        /// Look for methods like "SomeType MyType.Slice(int start, int length)".  Note that the
        /// names of the parameters are checked to ensure they are appropriate slice-like.  These
        /// names were picked by examining the patterns in the BCL for slicing members.
        /// </summary>
        public static bool IsSliceLikeMethod(IMethodSymbol method)
            => method != null &&
               IsPublicInstance(method) &&
               method.Parameters.Length == 2 &&
               IsSliceFirstParameter(method.Parameters[0]) &&
               IsSliceSecondParameter(method.Parameters[1]);

        private static bool IsSliceFirstParameter(IParameterSymbol parameter)
            => parameter.Type.SpecialType == SpecialType.System_Int32 &&
               (parameter.Name == "start" || parameter.Name == "startIndex");

        private static bool IsSliceSecondParameter(IParameterSymbol parameter)
            => parameter.Type.SpecialType == SpecialType.System_Int32 &&
               (parameter.Name == "count" || parameter.Name == "length");

        /// <summary>
        /// Finds a public, non-static indexer in the given type.  The indexer has to accept the
        /// provided <paramref name="parameterType"/> and must return the provided <paramref
        /// name="returnType"/>.
        /// </summary>
        public static IPropertySymbol GetIndexer(ITypeSymbol type, ITypeSymbol parameterType, ITypeSymbol returnType)
            => type.GetMembers(WellKnownMemberNames.Indexer)
                   .OfType<IPropertySymbol>()
                   .Where(p => p.IsIndexer &&
                               IsPublicInstance(p) &&
                               returnType.Equals(p.Type) &&
                               p.Parameters.Length == 1 &&
                               p.Parameters[0].Type.Equals(parameterType))
                   .FirstOrDefault();

        /// <summary>
        /// Finds a public, non-static overload of <paramref name="method"/> in the containing type.
        /// The overload must have the same return type as <paramref name="method"/>.  It must only
        /// have a single parameter, with the provided <paramref name="parameterType"/>.
        /// </summary>
        public static IMethodSymbol GetOverload(IMethodSymbol method, ITypeSymbol parameterType)
            => method.MethodKind != MethodKind.Ordinary
                ? null
                : method.ContainingType.GetMembers(method.Name)
                                       .OfType<IMethodSymbol>()
                                       .Where(m => IsPublicInstance(m) &&
                                                   m.Parameters.Length == 1 &&
                                                   m.Parameters[0].Type.Equals(parameterType) &&
                                                   m.ReturnType.Equals(method.ReturnType))
                                       .FirstOrDefault();
    }
}
