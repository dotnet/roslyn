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
        public static IPropertySymbol GetLengthOrCountProperty(INamedTypeSymbol namedType)
            => GetNoArgInt32Property(namedType, nameof(string.Length)) ??
               GetNoArgInt32Property(namedType, nameof(ICollection.Count));

        private static IPropertySymbol GetNoArgInt32Property(INamedTypeSymbol type, string name)
            => type.GetMembers(name)
                   .OfType<IPropertySymbol>()
                   .Where(p => IsPublicInstance(p) &&
                               p.Type.SpecialType == SpecialType.System_Int32)
                   .FirstOrDefault();

        public static bool IsPublicInstance(ISymbol symbol)
            => !symbol.IsStatic && symbol.DeclaredAccessibility == Accessibility.Public;

        public static IPropertySymbol GetIndexer(INamedTypeSymbol namedType, ITypeSymbol parameterType, ITypeSymbol returnType)
            => namedType.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.IsIndexer &&
                                    IsPublicInstance(p) &&
                                    returnType.Equals(p.Type) &&
                                    p.Parameters.Length == 1 &&
                                    p.Parameters[0].Type.Equals(parameterType))
                        .FirstOrDefault();

        public static PrefixUnaryExpressionSyntax IndexExpression(ExpressionSyntax expr)
            => SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.IndexExpression,
                expr.Parenthesize());

        /// <summary>
        /// Checks if this is an expression `expr.Length` where `expr` is equivalent to
        /// the instance we were originally invoking an accessor/method off of.
        /// </summary>
        public static bool IsInstanceLengthCheck(IPropertySymbol lengthLikeProperty, IOperation instance, IOperation operation)
            => operation is IPropertyReferenceOperation propertyRef &&
               propertyRef.Instance != null &&
               lengthLikeProperty.Equals(propertyRef.Property) &&
               CSharpSyntaxFactsService.Instance.AreEquivalent(instance.Syntax, propertyRef.Instance.Syntax);

        public static bool IsSubtraction(IOperation op, out IBinaryOperation subtraction)
        {
            if (op is IBinaryOperation binaryOperation &&
                binaryOperation.OperatorKind == BinaryOperatorKind.Subtract)
            {
                subtraction = binaryOperation;
                return true;
            }

            subtraction = null;
            return false;
        }

        private static bool IsConstantInt32(IOperation operation)
            => operation.ConstantValue.HasValue && operation.ConstantValue.Value is int;

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
        /// Look for methods like "SomeType MyType.Slice(int start, int length)".
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
