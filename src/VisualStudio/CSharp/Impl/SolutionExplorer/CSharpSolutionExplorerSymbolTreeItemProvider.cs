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
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;
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
            else
                TryAddType(member, items, cancellationToken);
        }
    }

    private static void AddTopLevelTypes(BaseNamespaceDeclarationSyntax baseNamespace, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        foreach (var member in baseNamespace.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (member is BaseNamespaceDeclarationSyntax childNamespace)
                AddTopLevelTypes(childNamespace, items, cancellationToken);
            else
                TryAddType(member, items, cancellationToken);
        }
    }

    private static bool TryAddType(MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (member)
        {
            case ExtensionBlockDeclarationSyntax extensionBlock:
                AddExtensionBlock(extensionBlock, items);
                return true;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclaration(typeDeclaration, items);
                return true;

            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclaration(enumDeclaration, items);
                return true;

            case DelegateDeclarationSyntax delegateDeclaration:
                AddDelegateDeclaration(delegateDeclaration, items);
                return true;
        }

        return false;
    }

    public ImmutableArray<SymbolTreeItem> GetItems(SymbolTreeItem item, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<SymbolTreeItem>.GetInstance(out var items);

        var memberDeclaration = (MemberDeclarationSyntax)item.SyntaxNode;
        switch (memberDeclaration)
        {
            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclarationMembers(enumDeclaration, items, cancellationToken);
                break;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclarationMembers(typeDeclaration, items, cancellationToken);
                break;
        }

        return items.ToImmutableAndClear();
    }

    private static void AddTypeDeclarationMembers(TypeDeclarationSyntax typeDeclaration, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        foreach (var member in typeDeclaration.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (TryAddType(member, items, cancellationToken))
                continue;

            AddMemberDeclaration(member, items);
        }
    }

    private static void AddMemberDeclaration(
        MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItem> items)
    {
        switch (member)
        {
            case BaseFieldDeclarationSyntax fieldDeclaration:
                AddFieldDeclaration(fieldDeclaration, items);
                return;

            case MethodDeclarationSyntax methodDeclaration:
                AddMethodDeclaration(methodDeclaration, items);
                return;

            case OperatorDeclarationSyntax operatorDeclaration:
                AddOperatorDeclaration(operatorDeclaration, items);
                return;

            case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                AddConversionOperatorDeclaration(conversionOperatorDeclaration, items);
                return;
        }
    }

    private static void AddConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax operatorDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(operatorDeclaration.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword
            ? "implicit operator "
            : "explicit operator ");
        AppendType(operatorDeclaration.Type, nameBuilder);
        AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);

        var accessibility = GetAccessibility(operatorDeclaration, operatorDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

        items.Add(new(
            nameBuilder.ToString(),
            glyph,
            operatorDeclaration,
            hasItems: false));
    }

    private static void AddOperatorDeclaration(
        OperatorDeclarationSyntax operatorDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append("operator ");
        nameBuilder.Append(operatorDeclaration.OperatorToken.ToString());
        AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);
        nameBuilder.Append(" : ");
        AppendType(operatorDeclaration.ReturnType, nameBuilder);

        var accessibility = GetAccessibility(operatorDeclaration, operatorDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

        items.Add(new(
            nameBuilder.ToString(),
            glyph,
            operatorDeclaration,
            hasItems: false));
    }

    private static void AddMethodDeclaration(
        MethodDeclarationSyntax methodDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(methodDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, methodDeclaration.TypeParameterList);
        AppendParameterList(nameBuilder, methodDeclaration.ParameterList);
        nameBuilder.Append(" : ");
        AppendType(methodDeclaration.ReturnType, nameBuilder);

        var accessibility = GetAccessibility(methodDeclaration, methodDeclaration.Modifiers);
        var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Method, accessibility);

        items.Add(new(
            nameBuilder.ToString(),
            glyph,
            methodDeclaration,
            hasItems: false));
    }

    private static void AddFieldDeclaration(
        BaseFieldDeclarationSyntax fieldDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        foreach (var variable in fieldDeclaration.Declaration.Variables)
        {
            nameBuilder.Clear();

            nameBuilder.Append(variable.Identifier.ValueText);
            nameBuilder.Append(" : ");
            AppendType(fieldDeclaration.Declaration.Type, nameBuilder);

            var accessibility = GetAccessibility(fieldDeclaration, fieldDeclaration.Modifiers);
            var kind = fieldDeclaration is EventFieldDeclarationSyntax
                ? DeclaredSymbolInfoKind.Event
                : DeclaredSymbolInfoKind.Field;

            items.Add(new(
                nameBuilder.ToString(),
                GlyphExtensions.GetGlyph(kind, accessibility),
                variable,
                hasItems: false));
        }
    }

    private static void AddEnumDeclarationMembers(EnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        foreach (var member in enumDeclaration.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(new(
                member.Identifier.ValueText,
                Glyph.EnumMemberPublic,
                member,
                hasItems: false));
        }
    }

    private static void AddEnumDeclaration(EnumDeclarationSyntax enumDeclaration, ArrayBuilder<SymbolTreeItem> items)
    {
        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Enum, GetAccessibility(enumDeclaration, enumDeclaration.Modifiers));

        items.Add(new(
            enumDeclaration.Identifier.ValueText,
            glyph,
            enumDeclaration,
            hasItems: enumDeclaration.Members.Count > 0));
    }

    private static void AddExtensionBlock(
        ExtensionBlockDeclarationSyntax extensionBlock,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append("extension");
        AppendTypeParameterList(nameBuilder, extensionBlock.TypeParameterList);
        AppendParameterList(nameBuilder, extensionBlock.ParameterList);

        items.Add(new(
            nameBuilder.ToString(),
            Glyph.ClassPublic,
            extensionBlock,
            hasItems: extensionBlock.Members.Count > 0));
    }

    private static void AddDelegateDeclaration(
        DelegateDeclarationSyntax delegateDeclaration,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(delegateDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, delegateDeclaration.TypeParameterList);
        AppendParameterList(nameBuilder, delegateDeclaration.ParameterList);

        nameBuilder.Append(" : ");
        AppendType(delegateDeclaration.ReturnType, nameBuilder);

        var glyph = GlyphExtensions.GetGlyph(
            DeclaredSymbolInfoKind.Delegate, GetAccessibility(delegateDeclaration, delegateDeclaration.Modifiers));

        items.Add(new(
            nameBuilder.ToString(),
            glyph,
            delegateDeclaration,
            hasItems: false));
    }

    private static void AddTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        ArrayBuilder<SymbolTreeItem> items)
    {
        using var _ = PooledStringBuilder.GetInstance(out var nameBuilder);

        nameBuilder.Append(typeDeclaration.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, typeDeclaration.TypeParameterList);

        var glyph = GlyphExtensions.GetGlyph(
            GetDeclaredSymbolInfoKind(typeDeclaration),
            GetAccessibility(typeDeclaration, typeDeclaration.Modifiers));
        items.Add(new(
            nameBuilder.ToString(),
            glyph,
            typeDeclaration,
            hasItems: typeDeclaration.Members.Count > 0));
    }

    private static void AppendCommaSeparatedList<TArgumentList, TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        TArgumentList? argumentList,
        Func<TArgumentList, IEnumerable<TArgument>> getArguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgumentList : SyntaxNode
        where TArgument : SyntaxNode
    {
        if (argumentList is null)
            return;

        AppendCommaSeparatedList(builder, openBrace, closeBrace, getArguments(argumentList), append, separator);
    }

    private static void AppendCommaSeparatedList<TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        IEnumerable<TArgument> arguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgument : SyntaxNode
    {
        builder.Append(openBrace);
        builder.AppendJoinedValues(separator, arguments, append);
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
                builder, "(", ")", tupleType.Elements, BuildDisplayText);
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

    private static void BuildDisplayText(TupleElementSyntax tupleElement, StringBuilder builder)
    {
        AppendType(tupleElement.Type, builder);
        if (tupleElement.Identifier != default)
        {
            builder.Append(' ');
            builder.Append(tupleElement.Identifier.ValueText);
        }
    }
}
