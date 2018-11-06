// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SymbolDisplayVisitor
    {
        protected override void AddExplicitlyCastedLiteralValue(INamedTypeSymbol namedType, object value)
        {
            Debug.Assert(namedType.TypeKind == TypeKind.Enum);

            AddPunctuation(SyntaxKind.OpenParenToken);
            namedType.Accept(this.NotFirstVisitor);
            AddPunctuation(SyntaxKind.CloseParenToken);
            AddNonEnumConstantValue(namedType.EnumUnderlyingType, value);
        }

        private static bool CanUseNullLiteral(ITypeSymbol type)
            => type.IsReferenceType || type.TypeKind == TypeKind.Pointer || ITypeSymbolHelpers.IsNullableType(type);

        /// <summary>
        /// Append a default argument (i.e. the default argument of an optional parameter) of a non-enum type.
        /// </summary>
        protected override void AddNonEnumConstantValue(ITypeSymbol type, object value)
        {
            Debug.Assert(type.TypeKind != TypeKind.Enum);

            if (value is null && !CanUseNullLiteral(type))
            {
                // For default arguments of value types and type parameters, we have to use a default expression.
                AddKeyword(SyntaxKind.DefaultKeyword);
            }
            else
            {
                SymbolDisplay.AddConstantValue(builder, value, LiteralDisplayOptions);
            }
        }

        protected override void AddBitwiseOr()
        {
            AddPunctuation(SyntaxKind.BarToken);
        }
    }
}
