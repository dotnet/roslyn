// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.SourceGeneration;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpCodeGenerator
    {
        private static TypeSyntax GenerateNamedTypeSyntaxWithoutNullable(INamedTypeSymbol symbol, bool onlyNames)
        {
            if (!symbol.TupleElements.IsDefault)
                return GenerateTupleTypeSyntaxWithoutNullable(symbol, onlyNames);

            if (symbol.SpecialType != SpecialType.None)
                return GenerateSpecialTypeSyntaxWithoutNullable(symbol, onlyNames);

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

        private static TypeSyntax GenerateTupleTypeSyntaxWithoutNullable(INamedTypeSymbol symbol, bool onlyNames)
        {
            if (symbol.TupleElements.Length < 2 || onlyNames)
                return GenerateValueTuple(symbol.TupleElements, 0, symbol.TupleElements.Length).GenerateTypeSyntax();

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

        private static TypeSyntax GenerateSpecialTypeSyntaxWithoutNullable(INamedTypeSymbol symbol, bool onlyNames)
        {
            if (onlyNames)
                return GenerateSystemType(symbol.SpecialType).GenerateNameSyntax();

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

        public static MemberDeclarationSyntax GenerateNamedTypeDeclaration(INamedTypeSymbol symbol)
        {
            if (symbol.TypeKind == TypeKind.Enum)
                return GenerateEnumDeclaration(symbol);

            if (symbol.TypeKind == TypeKind.Delegate)
                return GenerateDelegateDeclaration(symbol);

            throw new NotImplementedException();
        }

        private static BaseListSyntax? GenerateBaseList(
            INamedTypeSymbol? baseType,
            ImmutableArray<INamedTypeSymbol> interfaces)
        {
            using var _ = GetArrayBuilder<BaseTypeSyntax>(out var types);

            if (baseType != null)
                types.Add(SimpleBaseType(baseType.GenerateTypeSyntax()));

            foreach (var type in interfaces)
                types.Add(SimpleBaseType(type.GenerateTypeSyntax()));

            return types.Count == 0 ? null : BaseList(SeparatedList(types));
        }
    }
}
