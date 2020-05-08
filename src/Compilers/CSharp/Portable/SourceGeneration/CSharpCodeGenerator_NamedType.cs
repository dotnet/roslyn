// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static TypeSyntax GenerateNamedTypeSyntaxWithoutNullable(INamedTypeSymbol symbol)
        {
            if (!symbol.TupleElements.IsDefault)
                return GenerateTupleTypeSyntaxWithoutNullable(symbol);

            if (symbol.SpecialType != SpecialType.None)
                return GenerateSpecialTypeSyntaxWithoutNullable(symbol);

            var nameSyntax = symbol.TypeArguments.IsDefaultOrEmpty
                ? (SimpleNameSyntax)IdentifierName(symbol.Name)
                : GenericName(
                    Identifier(symbol.Name),
                    GenerateTypeArgumentList(symbol.TypeArguments));

            if (symbol.ContainingType != null)
            {
                var containingType = symbol.ContainingType.GenerateNameSyntax();
                return QualifiedName(containingType, nameSyntax);
            }
            else if (symbol.ContainingNamespace != null)
            {
                if (symbol.ContainingNamespace.IsGlobalNamespace)
                    return AliasQualifiedName(SyntaxFacts.GetText(SyntaxKind.GlobalKeyword), nameSyntax);

                var containingNamespace = symbol.ContainingNamespace.GenerateNameSyntax();
                return QualifiedName(containingNamespace, nameSyntax);
            }

            return nameSyntax;
        }

        private static TypeSyntax GenerateTupleTypeSyntaxWithoutNullable(INamedTypeSymbol symbol)
        {
            if (symbol.TupleElements.Length < 2)
                throw new ArgumentException("Tuples must contain at least two elements");

            using var _ = GetArrayBuilder<TupleElementSyntax>(out var elements);

            foreach (var field in symbol.TupleElements)
            {
                var fieldType = field.Type.GenerateTypeSyntax();
                elements.Add(string.IsNullOrEmpty(field.Name)
                    ? TupleElement(fieldType)
                    : TupleElement(fieldType, Identifier(field.Name)));
            }

            return TupleType(SeparatedList(elements));
        }

        private static TypeSyntax GenerateSpecialTypeSyntaxWithoutNullable(INamedTypeSymbol symbol)
        {
            switch (symbol.SpecialType)
            {
                case SpecialType.System_Object: return PredefinedType(Token(SyntaxKind.ObjectKeyword));
                case SpecialType.System_Boolean: return PredefinedType(Token(SyntaxKind.BoolKeyword));
                case SpecialType.System_Char: return PredefinedType(Token(SyntaxKind.CharKeyword));
                case SpecialType.System_SByte: return PredefinedType(Token(SyntaxKind.SByteKeyword));
                case SpecialType.System_Byte: return PredefinedType(Token(SyntaxKind.ByteKeyword));
                case SpecialType.System_Int16: return PredefinedType(Token(SyntaxKind.ShortKeyword));
                case SpecialType.System_UInt16: return PredefinedType(Token(SyntaxKind.UShortKeyword));
                case SpecialType.System_Int32: return PredefinedType(Token(SyntaxKind.IntKeyword));
                case SpecialType.System_UInt32: return PredefinedType(Token(SyntaxKind.UIntKeyword));
                case SpecialType.System_Int64: return PredefinedType(Token(SyntaxKind.LongKeyword));
                case SpecialType.System_UInt64: return PredefinedType(Token(SyntaxKind.ULongKeyword));
                case SpecialType.System_Decimal: return PredefinedType(Token(SyntaxKind.DecimalKeyword));
                case SpecialType.System_Single: return PredefinedType(Token(SyntaxKind.FloatKeyword));
                case SpecialType.System_Double: return PredefinedType(Token(SyntaxKind.DoubleKeyword));
                case SpecialType.System_String: return PredefinedType(Token(SyntaxKind.StringKeyword));
            }

            throw new NotImplementedException();
        }
    }
}
