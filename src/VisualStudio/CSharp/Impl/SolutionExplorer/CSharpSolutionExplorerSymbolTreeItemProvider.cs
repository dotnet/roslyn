// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;
using static Microsoft.CodeAnalysis.CSharp.FindSymbols.FindSymbolsUtilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.SolutionExplorer;

[ExportLanguageService(typeof(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSolutionExplorerSymbolTreeItemProvider()
    : AbstractSolutionExplorerSymbolTreeItemProvider<
        CompilationUnitSyntax,
        MemberDeclarationSyntax,
        BaseNamespaceDeclarationSyntax,
        EnumDeclarationSyntax,
        TypeDeclarationSyntax>
{
    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(CompilationUnitSyntax root)
        => root.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(BaseNamespaceDeclarationSyntax baseNamespace)
        => baseNamespace.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax typeDeclaration)
        => typeDeclaration.Members;

    protected override bool TryAddType(
        MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        switch (member)
        {
            case ExtensionBlockDeclarationSyntax extensionBlock:
                AddExtensionBlock(extensionBlock, items, nameBuilder);
                return true;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclaration(typeDeclaration, items, nameBuilder);
                return true;

            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclaration(enumDeclaration, items);
                return true;

            case DelegateDeclarationSyntax delegateDeclaration:
                AddDelegateDeclaration(delegateDeclaration, items, nameBuilder);
                return true;
        }

        return false;
    }

    protected override void AddMemberDeclaration(
        MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        switch (member)
        {
            case BaseFieldDeclarationSyntax fieldDeclaration:
                AddFieldDeclaration(fieldDeclaration, items, nameBuilder);
                return;

            case MethodDeclarationSyntax methodDeclaration:
                AddMethodDeclaration(methodDeclaration, items, nameBuilder);
                return;

            case OperatorDeclarationSyntax operatorDeclaration:
                AddOperatorDeclaration(operatorDeclaration, items, nameBuilder);
                return;

            case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                AddConversionOperatorDeclaration(conversionOperatorDeclaration, items, nameBuilder);
                return;

            case ConstructorDeclarationSyntax constructorDeclaration:
                AddConstructorOrDestructorDeclaration(constructorDeclaration, constructorDeclaration.Identifier, items, nameBuilder);
                return;

            case DestructorDeclarationSyntax destructorDeclaration:
                AddConstructorOrDestructorDeclaration(destructorDeclaration, destructorDeclaration.Identifier, items, nameBuilder);
                return;

            case PropertyDeclarationSyntax propertyDeclaration:
                AddPropertyDeclaration(propertyDeclaration, items, nameBuilder);
                return;

            case EventDeclarationSyntax eventDeclaration:
                AddEventDeclaration(eventDeclaration, items, nameBuilder);
                return;

            case IndexerDeclarationSyntax indexerDeclaration:
                AddIndexerDeclaration(indexerDeclaration, items, nameBuilder);
                return;
        }
    }

    private static void AddIndexerDeclaration(
        IndexerDeclarationSyntax indexerDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append("this");
        AppendCommaSeparatedList(
            nameBuilder, "[", "]",
            indexerDeclaration.ParameterList.Parameters,
            static (parameter, nameBuilder) => AppendType(parameter.Type, nameBuilder));
        nameBuilder.Append(" : ");
        AppendType(indexerDeclaration.Type, nameBuilder);

        var accessibility = GetAccessibility(indexerDeclaration, indexerDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Indexer, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            indexerDeclaration,
            indexerDeclaration.ThisKeyword));
    }

    private static void AddEventDeclaration(
        EventDeclarationSyntax eventDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append(eventDeclaration.Identifier.ValueText);
        nameBuilder.Append(" : ");
        AppendType(eventDeclaration.Type, nameBuilder);

        var accessibility = GetAccessibility(eventDeclaration, eventDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Event, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            eventDeclaration,
            eventDeclaration.Identifier));
    }

    private static void AddPropertyDeclaration(
        PropertyDeclarationSyntax propertyDeclaration, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        nameBuilder.Append(propertyDeclaration.Identifier.ValueText);
        nameBuilder.Append(" : ");
        AppendType(propertyDeclaration.Type, nameBuilder);

        var accessibility = GetAccessibility(propertyDeclaration, propertyDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Property, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            propertyDeclaration,
            propertyDeclaration.Identifier));
    }

    private static void AddConstructorOrDestructorDeclaration(
        BaseMethodDeclarationSyntax declaration,
        SyntaxToken identifier,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        if (declaration.Kind() == SyntaxKind.DestructorDeclaration)
            nameBuilder.Append('~');

        nameBuilder.Append(identifier.ValueText);
        AppendParameterList(nameBuilder, declaration.ParameterList);

        var accessibility = GetAccessibility(declaration, declaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Constructor, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            declaration,
            identifier));
    }

    private static void AddConversionOperatorDeclaration(
        ConversionOperatorDeclarationSyntax operatorDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append(operatorDeclaration.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword
            ? "implicit operator "
            : "explicit operator ");
        AppendType(operatorDeclaration.Type, nameBuilder);
        AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);

        var accessibility = GetAccessibility(operatorDeclaration, operatorDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            operatorDeclaration,
            operatorDeclaration.Type.GetFirstToken()));
    }

    private static void AddOperatorDeclaration(
        OperatorDeclarationSyntax operatorDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append("operator ");
        nameBuilder.Append(operatorDeclaration.OperatorToken.ToString());
        AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);
        nameBuilder.Append(" : ");
        AppendType(operatorDeclaration.ReturnType, nameBuilder);

        var accessibility = GetAccessibility(operatorDeclaration, operatorDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            operatorDeclaration,
            operatorDeclaration.OperatorToken));
    }

    private static void AddMethodDeclaration(
        MethodDeclarationSyntax methodDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append(methodDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, methodDeclaration.TypeParameterList);
        AppendParameterList(nameBuilder, methodDeclaration.ParameterList);
        nameBuilder.Append(" : ");
        AppendType(methodDeclaration.ReturnType, nameBuilder);

        var accessibility = GetAccessibility(methodDeclaration, methodDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Method, accessibility);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            methodDeclaration,
            methodDeclaration.Identifier));
    }

    private static void AddFieldDeclaration(
        BaseFieldDeclarationSyntax fieldDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            nameBuilder.Append(variable.Identifier.ValueText);
            nameBuilder.Append(" : ");
            AppendType(fieldDeclaration.Declaration.Type, nameBuilder);

            var accessibility = GetAccessibility(fieldDeclaration, fieldDeclaration.Modifiers);
            var kind = fieldDeclaration is EventFieldDeclarationSyntax
                ? DeclaredSymbolInfoKind.Event
                : DeclaredSymbolInfoKind.Field;

            items.Add(new(
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(kind, accessibility),
                hasItems: false,
                variable,
                variable.Identifier));
        }
    }

    protected override void AddEnumDeclarationMembers(
        EnumDeclarationSyntax enumDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        CancellationToken cancellationToken)
    {
        foreach (var member in enumDeclaration.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(new(
                member.Identifier.ValueText,
                Glyph.EnumMemberPublic,
                hasItems: false,
                member,
                member.Identifier));
        }
    }

    private static void AddEnumDeclaration(EnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItemData> items)
    {
        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Enum, GetAccessibility(enumDeclaration, enumDeclaration.Modifiers));

        items.Add(new(
            enumDeclaration.Identifier.ValueText,
            glyph,
            hasItems: enumDeclaration.Members.Count > 0,
            enumDeclaration,
            enumDeclaration.Identifier));
    }

    private static void AddExtensionBlock(
        ExtensionBlockDeclarationSyntax extensionBlock,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append("extension");
        AppendTypeParameterList(nameBuilder, extensionBlock.TypeParameterList);
        AppendParameterList(nameBuilder, extensionBlock.ParameterList);

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            Glyph.ClassPublic,
            hasItems: extensionBlock.Members.Count > 0,
            extensionBlock,
            extensionBlock.Keyword));
    }

    private static void AddDelegateDeclaration(
        DelegateDeclarationSyntax delegateDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append(delegateDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, delegateDeclaration.TypeParameterList);
        AppendParameterList(nameBuilder, delegateDeclaration.ParameterList);

        nameBuilder.Append(" : ");
        AppendType(delegateDeclaration.ReturnType, nameBuilder);

        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Delegate, GetAccessibility(delegateDeclaration, delegateDeclaration.Modifiers));

        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: false,
            delegateDeclaration,
            delegateDeclaration.Identifier));
    }

    private static void AddTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        StringBuilder nameBuilder)
    {
        nameBuilder.Append(typeDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, typeDeclaration.TypeParameterList);

        var glyph = GlyphExtensions.GetGlyph(
            GetDeclaredSymbolInfoKind(typeDeclaration),
            GetAccessibility(typeDeclaration, typeDeclaration.Modifiers));
        items.Add(new(
            nameBuilder.ToStringAndClear(),
            glyph,
            hasItems: typeDeclaration.Members.Count > 0,
            typeDeclaration,
            typeDeclaration.Identifier));
    }

    private static void AppendTypeParameterList(
        StringBuilder builder,
        TypeParameterListSyntax? typeParameterList)
    {
        AppendCommaSeparatedList(
            builder, "<", ">", typeParameterList,
            static typeParameterList => typeParameterList.Parameters,
            static (parameter, builder) => builder.Append(parameter.Identifier.ValueText));
    }

    private static void AppendParameterList(
        StringBuilder builder,
        ParameterListSyntax? parameterList)
    {
        AppendCommaSeparatedList(
            builder, "(", ")", parameterList,
            static parameterList => parameterList.Parameters,
            static (parameter, builder) => AppendType(parameter.Type, builder));
    }

    private static void AppendType(TypeSyntax? type, StringBuilder builder)
    {
        if (type is null)
            return;

        if (type is ArrayTypeSyntax arrayType)
        {
            AppendType(arrayType.ElementType, builder);
            AppendCommaSeparatedList(
                builder, "[", "]", arrayType.RankSpecifiers,
                static (_, _) => { }, ",");
        }
        else if (type is PointerTypeSyntax pointerType)
        {
            AppendType(pointerType.ElementType, builder);
            builder.Append('*');
        }
        else if (type is NullableTypeSyntax nullableType)
        {
            AppendType(nullableType.ElementType, builder);
            builder.Append('?');
        }
        else if (type is TupleTypeSyntax tupleType)
        {
            AppendCommaSeparatedList(
                builder, "(", ")", tupleType.Elements, AppendTupleElement);
        }
        else if (type is RefTypeSyntax refType)
        {
            builder.Append("ref ");
            AppendType(refType.Type, builder);
        }
        else if (type is ScopedTypeSyntax scopedType)
        {
            builder.Append("scoped ");
            AppendType(scopedType.Type, builder);
        }
        else if (type is PredefinedTypeSyntax predefinedType)
        {
            builder.Append(predefinedType.ToString());
        }
        else if (type is FunctionPointerTypeSyntax functionPointerType)
        {
            builder.Append("delegate*");
            AppendCommaSeparatedList(
                builder, "(", ")", functionPointerType.ParameterList.Parameters,
                static (parameter, builder) => AppendType(parameter.Type, builder));
        }
        else if (type is OmittedTypeArgumentSyntax)
        {
            // nothing to do here.
        }
        else if (type is QualifiedNameSyntax qualifiedName)
        {
            AppendType(qualifiedName.Right, builder);
        }
        else if (type is AliasQualifiedNameSyntax aliasQualifiedName)
        {
            AppendType(aliasQualifiedName.Name, builder);
        }
        else if (type is IdentifierNameSyntax identifierName)
        {
            builder.Append(identifierName.Identifier.ValueText);
        }
        else if (type is GenericNameSyntax genericName)
        {
            builder.Append(genericName.Identifier.ValueText);
            AppendCommaSeparatedList(
                builder, "<", ">", genericName.TypeArgumentList.Arguments, AppendType);
        }
        else
        {
            Debug.Fail("Unhandled type: " + type.GetType().FullName);
        }
    }

    private static void AppendTupleElement(TupleElementSyntax tupleElement, StringBuilder builder)
    {
        AppendType(tupleElement.Type, builder);
        if (tupleElement.Identifier != default)
        {
            builder.Append(' ');
            builder.Append(tupleElement.Identifier.ValueText);
        }
    }
}
