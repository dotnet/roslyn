// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
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

    private void AddTopLevelTypes(CompilationUnitSyntax root, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
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

    private void AddTopLevelTypes(BaseNamespaceDeclarationSyntax baseNamespace, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
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

    private void AddType(MemberDeclarationSyntax baseType, ArrayBuilder<SymbolTreeItem> items, CancellationToken cancellationToken)
    {
        switch (baseType)
        {
            case ExtensionBlockDeclarationSyntax extensionBlock:
                AddExtensionBlock(extensionBlock, items, cancellationToken);
                return;

            case TypeDeclarationSyntax typeDeclaration:
                AddTypeDeclaration(typeDeclaration, items, cancellationToken);
                return;

            case EnumDeclarationSyntax enumDeclaration:
                AddEnumDeclaration(enumDeclaration, items, cancellationToken);
                return;

            case DelegateDeclarationSyntax delegateDeclaration:
                AddDelegateDeclaration(delegateDeclaration, items, cancellationToken);
                return;
        }
    }

    private void AddTypeDeclaration(
        TypeDeclarationSyntax typeDeclaration,
        ArrayBuilder<SymbolTreeItem> items,
        CancellationToken cancellationToken)
    {
        var name = GetName(
            typeDeclaration.Identifier.ValueText, "<", ">",
            typeDeclaration.TypeParameterList,
            static typeParameterList => typeParameterList.Parameters.Select(p => p.Identifier.ValueText));

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

        var glyph = Microsoft.CodeAnalysis.GlyphExtensions.GetGlyph(kind, accessibility);
        items.Add(new(name, glyph, typeDeclaration));
    }

    private static string GetName<TArguments>(
        string baseName,
        string openBrace,
        string closeBrace,
        TArguments? arguments,
        Func<TArguments, IEnumerable<string>> getPieces) where TArguments : SyntaxNode
    {
        if (arguments is null)
            return baseName;

        return $"{baseName}{openBrace}{string.Join(", ", getPieces(arguments))}{closeBrace}";
    }
}
