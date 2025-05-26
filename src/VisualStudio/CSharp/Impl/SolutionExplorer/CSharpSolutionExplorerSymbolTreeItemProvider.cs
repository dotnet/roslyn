// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
internal sealed class CSharpSolutionExplorerSymbolTreeItemProvider() : ISolutionExplorerSymbolTreeItemProvider
{
    public async Task<ImmutableArray<SymbolTreeItem>> GetItemsAsync(
        Document document, CancellationToken cancellationToken)
    {
        var root = (CompilationUnitSyntax)await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<SymbolTreeItem>.GetInstance(out var items);

        AddTopLevelTypes(root, items, cancellationToken);

        return items.ToImmutableAndClear();
    }

    private static void AddTopLevelTypes(CompilationUnitSyntax root, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        foreach (var member in root.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is BaseNamespaceDeclarationSyntax baseNamespace)
                AddTopLevelTypes(baseNamespace, items, cancellationToken);
            else if (member is BaseTypeDeclarationSyntax baseType)
                AddType(baseType, items, cancellationToken);
        }
    }

    private static void AddTopLevelTypes(BaseNamespaceDeclarationSyntax baseNamespace, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        foreach (var member in baseNamespace.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is BaseNamespaceDeclarationSyntax childNamespace)
                AddTopLevelTypes(childNamespace, items, cancellationToken);
            else if (member is BaseTypeDeclarationSyntax baseType)
                AddType(baseType, items, cancellationToken);
            else if (member is DelegateDeclarationSyntax delegateType)
                AddType(delegateType, items, cancellationToken);
        }
    }

    private static void AddType(MemberDeclarationSyntax baseType, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (baseType)
        {
            case ExtensionBlockDeclarationSyntax extensionBlock:
                AddExtensionBlock(extensionBlock, items);
                return;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclaration(typeDeclaration, items);
                return;

            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclaration(enumDeclaration, items);
                return;

            case DelegateDeclarationSyntax delegateDeclaration:
                AddDelegateDeclaration(delegateDeclaration, items);
                return;
        }
    }

    private static void AddEnumDeclaration(EnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Enum, GetAccessibility(enumDeclaration, enumDeclaration.Modifiers));

        items.Add(new(enumDeclaration.Identifier.ValueText, glyph, enumDeclaration));
    }

    private static void AddExtensionBlock(
        ExtensionBlockDeclarationSyntax extensionBlock,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append("extension");
        AppendTypeParameterList(nameBuilder, extensionBlock.TypeParameterList);
        AppendParameterList(nameBuilder, extensionBlock.ParameterList);

        items.Add(new(nameBuilder.ToString(), Glyph.ClassPublic, extensionBlock));
    }

    private static void AddDelegateDeclaration(
        DelegateDeclarationSyntax delegateDeclaration,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(delegateDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, delegateDeclaration.TypeParameterList);
        AppendParameterList(nameBuilder, delegateDeclaration.ParameterList);

        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Delegate, GetAccessibility(delegateDeclaration, delegateDeclaration.Modifiers));

        items.Add(new(nameBuilder.ToString(), glyph, delegateDeclaration));
    }

    private static void AddTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(typeDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, typeDeclaration.TypeParameterList);

        var accessibility = GetAccessibility(typeDeclaration, typeDeclaration.Modifiers);
        var kind = typeDeclaration.Kind() switch
        {
            SyntaxKind.ClassDeclaration => DeclaredSymbolInfoKind.Class,
            SyntaxKind.InterfaceDeclaration => DeclaredSymbolInfoKind.Interface,
            SyntaxKind.StructDeclaration => DeclaredSymbolInfoKind.Struct,
            SyntaxKind.RecordDeclaration => DeclaredSymbolInfoKind.Record,
            SyntaxKind.RecordStructDeclaration => DeclaredSymbolInfoKind.RecordStruct,
            _ => throw ExceptionUtilities.UnexpectedValue(typeDeclaration.Kind()),
        };

        var glyph = GlyphExtensions.GetGlyph(kind, accessibility);
        items.Add(new(nameBuilder.ToString(), glyph, typeDeclaration));
    }

    private static void AppendCommaSeparatedList<TArgumentList, TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        TArgumentList? argumentList,
        Func<TArgumentList, IEnumerable<TArgument>> getArguments,
        Action<TArgument, StringBuilder> append)
        where TArgumentList : SyntaxNode
        where TArgument : SyntaxNode
    {
        if (argumentList is null)
            return;

        builder.Append(openBrace);
        builder.AppendJoinedValues(", ", getArguments(argumentList), append);
        builder.Append(closeBrace);
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
            static (parameter, builder) => BuildDisplayText(builder, parameter.Type));
    }

    private static string GetDisplayText(TypeSyntax? type)
    {
        using var _ = PooledStringBuilder.GetInstance(out var builder);
        BuildDisplayText(builder, type);
        return builder.ToString();
    }

    private static void BuildDisplayText(StringBuilder builder, TypeSyntax? type)
    {
        if (type is null)
            return;

        if (type is ArrayTypeSyntax arrayType)
        {
            BuildDisplayText(builder, arrayType.ElementType);
            builder.Append('[');
            builder.Append(',', arrayType.RankSpecifiers.Count);
            builder.Append(']');
        }
        else if (type is PointerTypeSyntax pointerType)
        {
            BuildDisplayText(builder, pointerType.ElementType);
            builder.Append('*');
        }
        else if (type is NullableTypeSyntax nullableType)
        {
            BuildDisplayText(builder, nullableType.ElementType);
            builder.Append('?');
        }
        else if (type is TupleTypeSyntax tupleType)
        {
            builder.Append('(');
            builder.AppendJoinedValues(", ", tupleType.Elements, static (tupleElement, builder) => BuildDisplayText(builder, tupleElement));
            builder.Append(')');
        }
        else if (type is RefTypeSyntax refType)
        {
            builder.Append("ref ");
            BuildDisplayText(builder, refType.Type);
        }
        else if (type is ScopedTypeSyntax scopedType)
        {
            builder.Append("scoped ");
            BuildDisplayText(builder, scopedType.Type);
        }
        else if (type is PredefinedTypeSyntax predefinedType)
        {
            builder.Append(predefinedType.ToString());
        }
        else if (type is FunctionPointerTypeSyntax functionPointerType)
        {
            builder.Append("delegate");
            builder.Append("*(");
            builder.AppendJoinedValues(
                ", ", functionPointerType.ParameterList.Parameters,
                static (parameter, builder) => BuildDisplayText(builder, parameter.Type));
            builder.Append(')');
        }
        else if (type is OmittedTypeArgumentSyntax)
        {
            // nothing to do here.
        }
        else if (type is QualifiedNameSyntax qualifiedName)
        {
            BuildDisplayText(builder, qualifiedName.Right);
        }
        else if (type is AliasQualifiedNameSyntax aliasQualifiedName)
        {
            BuildDisplayText(builder, aliasQualifiedName.Name);
        }
        else if (type is IdentifierNameSyntax identifierName)
        {
            builder.Append(identifierName.Identifier.ValueText);
        }
        else if (type is GenericNameSyntax genericName)
        {
            builder.Append(genericName.Identifier.ValueText);
            builder.Append('<');
            builder.AppendJoinedValues(
                ", ", genericName.TypeArgumentList.Arguments, static (type, builder) => BuildDisplayText(builder, type));
            builder.Append('>');
        }
        else
        {
            Debug.Fail("Unhandled type: " + type.GetType().FullName);
        }
    }

    private static void BuildDisplayText(StringBuilder builder, TupleElementSyntax tupleElement)
    {
        BuildDisplayText(builder, tupleElement.Type);
        if (tupleElement.Identifier != default)
        {
            builder.Append(' ');
            builder.Append(tupleElement.Identifier.ValueText);
        }
    }
}
