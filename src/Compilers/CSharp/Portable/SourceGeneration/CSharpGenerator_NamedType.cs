// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration
{
    internal partial class CSharpGenerator
    {
        private static TypeSyntax GenerateNamedTypeSyntaxWithoutNullable(INamedTypeSymbol symbol, bool onlyNames)
        {
            if (!symbol.TupleElements.IsDefault)
                return GenerateTupleTypeSyntaxWithoutNullable(symbol, onlyNames);

            if (!onlyNames && symbol.SpecialType != SpecialType.None)
                return GenerateSpecialTypeSyntaxWithoutNullable(symbol);

            return GenerateNormalNamedTypeSyntaxWithoutNullable(symbol);
        }

        private static TypeSyntax GenerateNormalNamedTypeSyntaxWithoutNullable(INamedTypeSymbol symbol)
        {
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
                case SpecialType.System_Void: return PredefinedType(Token(SyntaxKind.VoidKeyword));
            }

            // Fallback to normal generation.
            return GenerateNormalNamedTypeSyntaxWithoutNullable(symbol);
        }

        public MemberDeclarationSyntax GenerateNamedTypeDeclaration(INamedTypeSymbol symbol)
        {
            var previousNamedType = _currentNamedType;
            _currentNamedType = symbol;

            try
            {
                if (symbol.TypeKind == TypeKind.Enum)
                    return GenerateEnumDeclaration(symbol);

                if (symbol.TypeKind == TypeKind.Delegate)
                    return GenerateDelegateDeclaration(symbol);

                var typeKind =
                    symbol.TypeKind == TypeKind.Struct ? SyntaxKind.StructDeclaration :
                    symbol.TypeKind == TypeKind.Interface ? SyntaxKind.InterfaceDeclaration :
                    SyntaxKind.ClassDeclaration;

                var keyword = Token(
                    symbol.TypeKind == TypeKind.Struct ? SyntaxKind.StructKeyword :
                    symbol.TypeKind == TypeKind.Interface ? SyntaxKind.InterfaceKeyword :
                    SyntaxKind.ClassKeyword);

                return TypeDeclaration(
                    typeKind,
                    GenerateAttributeLists(symbol.GetAttributes()),
                    GenerateModifiers(symbol.DeclaredAccessibility, symbol.GetModifiers()),
                    keyword,
                    Identifier(symbol.Name),
                    GenerateTypeParameterList(symbol.TypeArguments),
                    GenerateBaseList(symbol.BaseType, symbol.Interfaces),
                    GenerateTypeParameterConstraintClauses(symbol.TypeArguments),
                    Token(SyntaxKind.OpenBraceToken),
                    GenerateMemberDeclarations(symbol.GetMembers()),
                    Token(SyntaxKind.CloseBraceToken),
                    semicolonToken: default);
            }
            finally
            {
                _currentNamedType = previousNamedType;
            }
        }

        private static BaseListSyntax? GenerateBaseList(
            INamedTypeSymbol? baseType,
            ImmutableArray<INamedTypeSymbol> interfaces)
        {
            using var _ = GetArrayBuilder<BaseTypeSyntax>(out var types);

            if (baseType != null && baseType.SpecialType != SpecialType.System_Object)
                types.Add(SimpleBaseType(baseType.GenerateTypeSyntax()));

            foreach (var type in interfaces)
                types.Add(SimpleBaseType(type.GenerateTypeSyntax()));

            return types.Count == 0 ? null : BaseList(SeparatedList(types));
        }
    }
}
