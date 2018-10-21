// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
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

        public static IPropertySymbol GetIndexer(INamedTypeSymbol namedType, ITypeSymbol parameterType)
            => namedType.GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.IsIndexer &&
                                    IsPublicInstance(p) &&
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
    }
}
