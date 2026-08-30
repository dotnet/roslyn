// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.NavigationBar;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionExplorer;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(DocumentSymbolsHandler)), Shared]
[Method(Methods.TextDocumentDocumentSymbolName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DocumentSymbolsHandler() : ILspServiceDocumentRequestHandler<RoslynDocumentSymbolParams, SumType<DocumentSymbol[], SymbolInformation[]>>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public TextDocumentIdentifier GetTextDocumentIdentifier(RoslynDocumentSymbolParams request) => request.TextDocument;

    public async Task<SumType<DocumentSymbol[], SymbolInformation[]>> HandleRequestAsync(
        RoslynDocumentSymbolParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = await context.GetRequiredDocumentAsync(cancellationToken).ConfigureAwait(false);
        var clientCapabilities = context.GetRequiredClientCapabilities();
        var useHierarchicalSymbols = clientCapabilities.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true || request.UseHierarchicalSymbols;
        var supportsVSExtensions = clientCapabilities.HasVisualStudioLspCapability();

        return await GetDocumentSymbolsAsync(document, useHierarchicalSymbols, supportsVSExtensions, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<SumType<DocumentSymbol[], SymbolInformation[]>> GetDocumentSymbolsAsync(
        Document document, bool useHierarchicalSymbols, bool supportsVSExtensions, CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        if (useHierarchicalSymbols)
        {
            if (document.SupportsSyntaxTree)
            {
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var solutionExplorerSymbolTreeItemProvider = document.Project.Services.GetRequiredService<ISolutionExplorerSymbolTreeItemProvider>();

                return GetDocumentSymbolsFromSolutionExplorer(document.Id, root, text, solutionExplorerSymbolTreeItemProvider, cancellationToken);
            }

            // Languages without a Roslyn syntax tree (e.g. F#) cannot use ISolutionExplorerSymbolTreeItemProvider.
            // Fall back to whatever hierarchical data their navigation bar provider exposes, if any.
            var navBarItemService = document.Project.Services.GetService<INavigationBarItemService>();
            if (navBarItemService == null)
                return Array.Empty<DocumentSymbol>();

            var navBarItems = await navBarItemService.GetItemsAsync(document, supportsCodeGeneration: false, frozenPartialSemantics: false, cancellationToken).ConfigureAwait(false);
            return GetDocumentSymbolsFromNavigationBarItems(navBarItems, text);
        }
        else
        {
            // The client does not support hierarchical symbols (for example, VS LiveShare navbar), so fall back to the navbar provider which provides a flatter list of symbols.
            var navBarService = document.Project.Services.GetRequiredService<INavigationBarItemService>();
            var navBarItems = await navBarService.GetItemsAsync(document, supportsCodeGeneration: false, frozenPartialSemantics: false, cancellationToken).ConfigureAwait(false);
            if (navBarItems.IsEmpty)
                return Array.Empty<DocumentSymbol>();

            using var _ = ArrayBuilder<SymbolInformation>.GetInstance(out var symbols);
            foreach (var item in navBarItems)
            {
                symbols.AddIfNotNull(GetSymbolInformation(item, document, text, containerName: null, supportsVSExtensions));

                foreach (var childItem in item.ChildItems)
                    symbols.AddIfNotNull(GetSymbolInformation(childItem, document, text, item.Text, supportsVSExtensions));
            }

            return symbols.ToArray();
        }
    }

    private static RoslynDocumentSymbol[] GetDocumentSymbolsFromSolutionExplorer(
        DocumentId documentId,
        SyntaxNode node,
        SourceText text,
        ISolutionExplorerSymbolTreeItemProvider provider,
        CancellationToken cancellationToken)
    {
        var items = provider.GetItems(documentId, node, includeNamespaces: true, cancellationToken);
        return [.. items.Select(i => ConvertToDocumentSymbol(i, text, documentId, provider, cancellationToken))];
    }

    private static RoslynDocumentSymbol ConvertToDocumentSymbol(
        SymbolTreeItemData item,
        SourceText text,
        DocumentId documentId,
        ISolutionExplorerSymbolTreeItemProvider provider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var itemKey = item.ItemKey;
        var itemSyntax = item.ItemSyntax;

        // Get the full span from the declaration node and the selection span from the navigation token
        var fullSpan = itemSyntax.DeclarationNode.Span;
        // If we're in the middle of typing, the navigation token (typically identifier) may be missing and this will be a SyntaxKind.None (0)
        var selectionSpan = itemSyntax.NavigationToken.RawKind == 0 ? itemSyntax.DeclarationNode.Span : itemSyntax.NavigationToken.Span;

        // Recursively get children if this item has child items
        var children = itemKey.HasItems
            ? GetDocumentSymbolsFromSolutionExplorer(documentId, itemSyntax.DeclarationNode, text, provider, cancellationToken)
            : [];

        return new RoslynDocumentSymbol
        {
            Name = GetDocumentSymbolName(itemKey.Name),
            Detail = itemKey.Name,
            Kind = ProtocolConversions.GlyphToSymbolKind(itemKey.Glyph),
            Glyph = (int)itemKey.Glyph,
            Range = ProtocolConversions.TextSpanToRange(fullSpan, text),
            SelectionRange = ProtocolConversions.TextSpanToRange(selectionSpan, text),
            Children = children,
        };
    }

    private static RoslynDocumentSymbol[] GetDocumentSymbolsFromNavigationBarItems(ImmutableArray<RoslynNavigationBarItem> items, SourceText text)
    {
        using var _ = ArrayBuilder<RoslynDocumentSymbol>.GetInstance(out var symbols);
        foreach (var item in items)
        {
            if (item is RoslynNavigationBarItem.SymbolItem symbolItem && symbolItem.Location.InDocumentInfo != null)
                symbols.Add(ConvertToDocumentSymbol(symbolItem, text));
        }

        return symbols.ToArray();
    }

    private static RoslynDocumentSymbol ConvertToDocumentSymbol(RoslynNavigationBarItem.SymbolItem item, SourceText text)
    {
        var (spans, navigationSpan) = item.Location.InDocumentInfo!.Value;
        var fullSpan = spans[0];

        return new RoslynDocumentSymbol
        {
            Name = GetDocumentSymbolName(item.Text),
            Detail = item.Name,
            Kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph),
            Glyph = (int)item.Glyph,
            Range = ProtocolConversions.TextSpanToRange(fullSpan, text),
            SelectionRange = ProtocolConversions.TextSpanToRange(navigationSpan, text),
            Children = GetDocumentSymbolsFromNavigationBarItems(item.ChildItems, text),
        };
    }

    /// <summary>
    /// Get a symbol information from a specified nav bar item.
    /// </summary>
    private static SymbolInformation? GetSymbolInformation(
        RoslynNavigationBarItem item, Document document, SourceText text, string? containerName, bool supportsVSExtensions)
    {
        if (item is not RoslynNavigationBarItem.SymbolItem symbolItem || symbolItem.Location.InDocumentInfo == null)
            return null;

        var name = GetDocumentSymbolName(item.Text);
        var kind = ProtocolConversions.GlyphToSymbolKind(item.Glyph);
        var location = new LSP.Location()
        {
            DocumentUri = document.GetURI(),
            Range = ProtocolConversions.TextSpanToRange(symbolItem.Location.InDocumentInfo.Value.navigationSpan, text),
        };

        return SymbolInformationFactory.Create(name, containerName, kind, location, item.Glyph, supportsVSExtensions);
    }

    /// <summary>
    /// DocumentSymbol name cannot be null or empty. Check if the name is invalid,
    /// and if so return a substitute string.
    /// </summary>
    /// <param name="proposedName">Name proposed for DocumentSymbol</param>
    /// <returns>Valid name for DocumentSymbol</returns>
    private static string GetDocumentSymbolName(string proposedName)
    {
        return String.IsNullOrEmpty(proposedName) ? "." : proposedName;
    }
}
