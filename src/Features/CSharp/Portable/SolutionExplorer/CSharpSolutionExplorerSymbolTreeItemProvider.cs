// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionExplorer;
using static Microsoft.CodeAnalysis.CSharp.FindSymbols.FindSymbolsUtilities;

namespace Microsoft.CodeAnalysis.CSharp.SolutionExplorer;

[ExportLanguageService(typeof(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSolutionExplorerSymbolTreeItemProvider()
    : AbstractSolutionExplorerSymbolTreeItemProvider<
        CompilationUnitSyntax,
        MemberDeclarationSyntax,
        BaseNamespaceDeclarationSyntax,
        EnumDeclarationSyntax,
        TypeDeclarationSyntax,
        LocalFunctionStatementSyntax>
{
    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(CompilationUnitSyntax root)
        => root.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(BaseNamespaceDeclarationSyntax baseNamespace)
        => baseNamespace.Members;

    protected override SyntaxList<MemberDeclarationSyntax> GetMembers(TypeDeclarationSyntax typeDeclaration)
        => typeDeclaration.Members;

    protected override bool TryAddNamespace(
        DocumentId documentId, MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        if (member is not BaseNamespaceDeclarationSyntax namespaceDeclaration)
            return false;

        var name = namespaceDeclaration.Name;
        nameBuilder.Append(name.ToString());

        items.Add(new(
            documentId,
            nameBuilder.ToStringAndClear(),
            Glyph.Namespace,
            hasItems: namespaceDeclaration.Members.Count > 0,
            namespaceDeclaration,
            name.GetFirstToken()));

        return true;
    }

    protected override bool TryAddType(
        DocumentId documentId, MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        switch (member)
        {
            case ExtensionBlockDeclarationSyntax extensionBlock:
                AddExtensionBlock(extensionBlock);
                return true;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclaration(typeDeclaration);
                return true;

            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclaration(enumDeclaration);
                return true;

            case DelegateDeclarationSyntax delegateDeclaration:
                AddDelegateDeclaration(delegateDeclaration);
                return true;
        }

        return false;

        void AddExtensionBlock(ExtensionBlockDeclarationSyntax extensionBlock)
        {
            nameBuilder.Append("extension");
            AppendTypeParameterList(nameBuilder, extensionBlock.TypeParameterList);
            AppendParameterList(nameBuilder, extensionBlock.ParameterList);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                Glyph.ClassPublic,
                hasItems: extensionBlock.Members.Count > 0,
                extensionBlock,
                extensionBlock.Keyword));
        }

        void AddTypeDeclaration(TypeDeclarationSyntax typeDeclaration)
        {
            nameBuilder.Append(typeDeclaration.Identifier.ValueText);
            AppendTypeParameterList(nameBuilder, typeDeclaration.TypeParameterList);

            var glyph = GlyphExtensions.GetGlyph(
                GetDeclaredSymbolInfoKind(typeDeclaration),
                GetAccessibility(typeDeclaration.GetRequiredParent(), typeDeclaration.Modifiers));
            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: typeDeclaration.Members.Count > 0,
                typeDeclaration,
                typeDeclaration.Identifier));
        }

        void AddEnumDeclaration(EnumDeclarationSyntax enumDeclaration)
        {
            var glyph = GlyphExtensions.GetGlyph(
                DeclaredSymbolInfoKind.Enum, GetAccessibility(enumDeclaration.GetRequiredParent(), enumDeclaration.Modifiers));

            items.Add(new(
                documentId,
                enumDeclaration.Identifier.ValueText,
                glyph,
                hasItems: enumDeclaration.Members.Count > 0,
                enumDeclaration,
                enumDeclaration.Identifier));
        }

        void AddDelegateDeclaration(DelegateDeclarationSyntax delegateDeclaration)
        {
            nameBuilder.Append(delegateDeclaration.Identifier.ValueText);
            AppendTypeParameterList(nameBuilder, delegateDeclaration.TypeParameterList);
            AppendParameterList(nameBuilder, delegateDeclaration.ParameterList);

            nameBuilder.Append(" : ");
            AppendType(delegateDeclaration.ReturnType, nameBuilder);

            var glyph = GlyphExtensions.GetGlyph(
                DeclaredSymbolInfoKind.Delegate, GetAccessibility(delegateDeclaration.GetRequiredParent(), delegateDeclaration.Modifiers));

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: false,
                delegateDeclaration,
                delegateDeclaration.Identifier));
        }
    }

    protected override void AddMemberDeclaration(
        DocumentId documentId, MemberDeclarationSyntax member, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        switch (member)
        {
            case BaseFieldDeclarationSyntax fieldDeclaration:
                AddFieldDeclaration(fieldDeclaration);
                return;

            case MethodDeclarationSyntax methodDeclaration:
                AddMethodDeclaration(methodDeclaration);
                return;

            case OperatorDeclarationSyntax operatorDeclaration:
                AddOperatorDeclaration(operatorDeclaration);
                return;

            case ConversionOperatorDeclarationSyntax conversionOperatorDeclaration:
                AddConversionOperatorDeclaration(conversionOperatorDeclaration);
                return;

            case ConstructorDeclarationSyntax constructorDeclaration:
                AddConstructorOrDestructorDeclaration(constructorDeclaration, constructorDeclaration.Identifier);
                return;

            case DestructorDeclarationSyntax destructorDeclaration:
                AddConstructorOrDestructorDeclaration(destructorDeclaration, destructorDeclaration.Identifier);
                return;

            case PropertyDeclarationSyntax propertyDeclaration:
                AddPropertyDeclaration(propertyDeclaration);
                return;

            case EventDeclarationSyntax eventDeclaration:
                AddEventDeclaration(eventDeclaration);
                return;

            case IndexerDeclarationSyntax indexerDeclaration:
                AddIndexerDeclaration(indexerDeclaration);
                return;
        }

        void AddMethodDeclaration(MethodDeclarationSyntax methodDeclaration)
        {
            nameBuilder.Append(methodDeclaration.Identifier.ValueText);
            AppendTypeParameterList(nameBuilder, methodDeclaration.TypeParameterList);
            AppendParameterList(nameBuilder, methodDeclaration.ParameterList);
            nameBuilder.Append(" : ");
            AppendType(methodDeclaration.ReturnType, nameBuilder);

            var accessibility = GetAccessibility(methodDeclaration.GetRequiredParent(), methodDeclaration.Modifiers);
            var isExtension = methodDeclaration.IsParentKind(SyntaxKind.ExtensionBlockDeclaration) ||
                (methodDeclaration.ParameterList is { Parameters: [var parameter, ..] } && parameter.Modifiers.Any(SyntaxKind.ThisKeyword));
            var glyph = GlyphExtensions.GetGlyph(
                isExtension ? DeclaredSymbolInfoKind.ExtensionMethod : DeclaredSymbolInfoKind.Method, accessibility);

            var hasItems = HasDescendentLocalFunctions(methodDeclaration);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: hasItems,
                methodDeclaration,
                methodDeclaration.Identifier));
        }

        void AddFieldDeclaration(BaseFieldDeclarationSyntax fieldDeclaration)
        {
            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                nameBuilder.Append(variable.Identifier.ValueText);
                nameBuilder.Append(" : ");
                AppendType(fieldDeclaration.Declaration.Type, nameBuilder);

                var accessibility = GetAccessibility(fieldDeclaration.GetRequiredParent(), fieldDeclaration.Modifiers);
                var kind = fieldDeclaration is EventFieldDeclarationSyntax
                    ? DeclaredSymbolInfoKind.Event
                    : DeclaredSymbolInfoKind.Field;

                items.Add(new(
                    documentId,
                    nameBuilder.ToStringAndClear(),
                    GlyphExtensions.GetGlyph(kind, accessibility),
                    hasItems: false,
                    variable,
                    variable.Identifier));
            }
        }

        void AddOperatorDeclaration(OperatorDeclarationSyntax operatorDeclaration)
        {
            nameBuilder.Append("operator ");
            nameBuilder.Append(operatorDeclaration.OperatorToken.ToString());
            AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);
            nameBuilder.Append(" : ");
            AppendType(operatorDeclaration.ReturnType, nameBuilder);

            var accessibility = GetAccessibility(operatorDeclaration.GetRequiredParent(), operatorDeclaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: HasDescendentLocalFunctions(operatorDeclaration),
                operatorDeclaration,
                operatorDeclaration.OperatorToken));
        }

        void AddConversionOperatorDeclaration(ConversionOperatorDeclarationSyntax operatorDeclaration)
        {
            nameBuilder.Append(operatorDeclaration.ImplicitOrExplicitKeyword.Kind() == SyntaxKind.ImplicitKeyword
                ? "implicit operator "
                : "explicit operator ");
            AppendType(operatorDeclaration.Type, nameBuilder);
            AppendParameterList(nameBuilder, operatorDeclaration.ParameterList);

            var accessibility = GetAccessibility(operatorDeclaration.GetRequiredParent(), operatorDeclaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: HasDescendentLocalFunctions(operatorDeclaration),
                operatorDeclaration,
                operatorDeclaration.Type.GetFirstToken()));
        }

        void AddConstructorOrDestructorDeclaration(BaseMethodDeclarationSyntax declaration, SyntaxToken identifier)
        {
            if (declaration.Kind() == SyntaxKind.DestructorDeclaration)
                nameBuilder.Append('~');

            nameBuilder.Append(identifier.ValueText);
            AppendParameterList(nameBuilder, declaration.ParameterList);

            var accessibility = GetAccessibility(declaration.GetRequiredParent(), declaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Constructor, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: HasDescendentLocalFunctions(declaration),
                declaration,
                identifier));
        }

        void AddPropertyDeclaration(PropertyDeclarationSyntax propertyDeclaration)
        {
            nameBuilder.Append(propertyDeclaration.Identifier.ValueText);
            nameBuilder.Append(" : ");
            AppendType(propertyDeclaration.Type, nameBuilder);

            var accessibility = GetAccessibility(propertyDeclaration.GetRequiredParent(), propertyDeclaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Property, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: HasDescendentLocalFunctions(propertyDeclaration),
                propertyDeclaration,
                propertyDeclaration.Identifier));
        }

        void AddEventDeclaration(EventDeclarationSyntax eventDeclaration)
        {
            nameBuilder.Append(eventDeclaration.Identifier.ValueText);
            nameBuilder.Append(" : ");
            AppendType(eventDeclaration.Type, nameBuilder);

            var accessibility = GetAccessibility(eventDeclaration.GetRequiredParent(), eventDeclaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Event, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: false,
                eventDeclaration,
                eventDeclaration.Identifier));
        }

        void AddIndexerDeclaration(IndexerDeclarationSyntax indexerDeclaration)
        {
            nameBuilder.Append("this");
            AppendCommaSeparatedList(
                nameBuilder, "[", "]",
                indexerDeclaration.ParameterList.Parameters,
                static (parameter, nameBuilder) => AppendType(parameter.Type, nameBuilder));
            nameBuilder.Append(" : ");
            AppendType(indexerDeclaration.Type, nameBuilder);

            var accessibility = GetAccessibility(indexerDeclaration.GetRequiredParent(), indexerDeclaration.Modifiers);
            var glyph = GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Indexer, accessibility);

            items.Add(new(
                documentId,
                nameBuilder.ToStringAndClear(),
                glyph,
                hasItems: false,
                indexerDeclaration,
                indexerDeclaration.ThisKeyword));
        }
    }

    protected override void AddEnumDeclarationMembers(
        DocumentId documentId,
        EnumDeclarationSyntax enumDeclaration,
        ArrayBuilder<SymbolTreeItemData> items,
        CancellationToken cancellationToken)
    {
        foreach (var member in enumDeclaration.Members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            items.Add(new(
                documentId,
                member.Identifier.ValueText,
                Glyph.EnumMemberPublic,
                hasItems: false,
                member,
                member.Identifier));
        }
    }

    protected override ImmutableArray<LocalFunctionStatementSyntax> GetMemberDeclarationMembers(MemberDeclarationSyntax memberDeclaration)
        => GetLocalFunctions(memberDeclaration);

    protected override ImmutableArray<LocalFunctionStatementSyntax> GetMemberStatementMembers(LocalFunctionStatementSyntax memberDeclaration)
        => GetLocalFunctions(memberDeclaration);

    protected override void AddMemberStatement(DocumentId documentId, LocalFunctionStatementSyntax statement, ArrayBuilder<SymbolTreeItemData> items, StringBuilder nameBuilder)
    {
        nameBuilder.Append(statement.Identifier.ValueText);
        AppendTypeParameterList(nameBuilder, statement.TypeParameterList);
        AppendParameterList(nameBuilder, statement.ParameterList);
        nameBuilder.Append(" : ");
        AppendType(statement.ReturnType, nameBuilder);

        items.Add(new(
            documentId,
            nameBuilder.ToStringAndClear(),
            Glyph.MethodPrivate,
            hasItems: HasDescendentLocalFunctions(statement),
            statement,
            statement.Identifier));
    }

    private static bool HasDescendentLocalFunctions(SyntaxNode node) => GetLocalFunctions(node).Any();

    private static ImmutableArray<LocalFunctionStatementSyntax> GetLocalFunctions(SyntaxNode node)
    {
        var localFunctions = node.DescendantNodes(descendIntoChildren: (n) =>
            // Always descend from the original node even if its a local function, but do not descend further into descendent local functions.
            n == node || n is not LocalFunctionStatementSyntax).Where(n => n is LocalFunctionStatementSyntax).Cast<LocalFunctionStatementSyntax>().ToImmutableArray();
        return localFunctions;
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
            foreach (var rankSpecifier in arrayType.RankSpecifiers)
            {
                builder.Append('[');
                AppendCommaSeparatedList(
                    builder, "", "", rankSpecifier.Sizes,
                    static (_, _) => { }, ",");
                builder.Append(']');
            }
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
                builder, "<", ">", functionPointerType.ParameterList.Parameters,
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
