// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentSymbols;

[Export(typeof(IDocumentSymbolService)), Shared]
[method: ImportingConstructor]
internal sealed class DocumentSymbolService(IDocumentMappingService documentMappingService) : IDocumentSymbolService
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;

    public SumType<DocumentSymbol[], SymbolInformation[]>? GetDocumentSymbols(
        RazorFileKind fileKind,
        DocumentUri razorDocumentUri,
        RazorCSharpDocument implementationDocument,
        SumType<DocumentSymbol[], SymbolInformation[]> implementationSymbols,
        RazorCSharpDocument? declarationDocument,
        SumType<DocumentSymbol[], SymbolInformation[]>? declarationSymbols)
    {
        Debug.Assert((declarationDocument is null) == (declarationSymbols is null));

        if (implementationSymbols.TryGetFirst(out var implementationDocumentSymbols))
        {
            DocumentSymbol[]? declarationDocumentSymbols = null;
            if (declarationSymbols is { } symbols &&
                !symbols.TryGetFirst(out declarationDocumentSymbols))
            {
                throw CreateUnsupportedResponseTypeException();
            }

            var mappedDeclarationSymbols = RemapDocumentSymbols(fileKind, declarationDocument, declarationDocumentSymbols);
            var mappedImplementationSymbols = RemapDocumentSymbols(fileKind, implementationDocument, implementationDocumentSymbols);
            var mergedSymbols = MergeDocumentSymbols(mappedDeclarationSymbols, mappedImplementationSymbols);

            return mergedSymbols.Length == 0
                ? [CreateRenderMethodDocumentSymbol(fileKind)]
                : mergedSymbols;
        }
        else if (implementationSymbols.TryGetSecond(out var implementationSymbolInformations))
        {
            SymbolInformation[]? declarationSymbolInformations = null;
            if (declarationSymbols is { } symbols &&
                !symbols.TryGetSecond(out declarationSymbolInformations))
            {
                throw CreateUnsupportedResponseTypeException();
            }

            var mappedDeclarationSymbols = RemapSymbolInformations(fileKind, razorDocumentUri, declarationDocument, declarationSymbolInformations);
            var mappedImplementationSymbols = RemapSymbolInformations(fileKind, razorDocumentUri, implementationDocument, implementationSymbolInformations);

            return MergeSymbolInformations(fileKind, razorDocumentUri, mappedDeclarationSymbols, mappedImplementationSymbols);
        }
        else
        {
            throw CreateUnsupportedResponseTypeException();
        }
    }

    private SymbolInformation[]? RemapSymbolInformations(
        RazorFileKind fileKind,
        DocumentUri razorDocumentUri,
        RazorCSharpDocument? csharpDocument,
        SymbolInformation[]? symbolInformations)
    {
        if (csharpDocument is null ||
            symbolInformations is null)
        {
            return null;
        }

        using var _ = ListPool<SymbolInformation>.GetPooledObject(out var mappedSymbols);

        foreach (var symbolInformation in symbolInformations)
        {
            // SymbolInformation is obsolete, but things still return it so we have to handle it
#pragma warning disable CS0618 // Type or member is obsolete
            if (symbolInformation.Name == RenderMethodSignature(fileKind))
            {
                symbolInformation.Name = RenderMethodDisplay(fileKind);
                symbolInformation.Location.Range = LspFactory.DefaultRange;
                symbolInformation.Location.DocumentUri = razorDocumentUri;
                mappedSymbols.Add(symbolInformation);
            }
            else if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, symbolInformation.Location.Range, out var newRange))
            {
                symbolInformation.Location.Range = newRange;
                symbolInformation.Location.DocumentUri = razorDocumentUri;
                mappedSymbols.Add(symbolInformation);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        return mappedSymbols.ToArray();
    }

    private DocumentSymbol[]? RemapDocumentSymbols(RazorFileKind fileKind, RazorCSharpDocument? csharpDocument, DocumentSymbol[]? documentSymbols)
    {
        if (csharpDocument is null ||
            documentSymbols is null)
        {
            return null;
        }

        using var _ = ListPool<DocumentSymbol>.GetPooledObject(out var mappedSymbols);

        foreach (var documentSymbol in documentSymbols)
        {
            if (TryRemapRanges(csharpDocument, documentSymbol))
            {
                documentSymbol.Children = RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children);

                mappedSymbols.Add(documentSymbol);
            }
            else if (documentSymbol.Children is [_, ..] &&
                RemapDocumentSymbols(fileKind, csharpDocument, documentSymbol.Children) is [_, ..] mappedChildren)
            {
                // This range didn't map, but some/all of its children did, so we promote them to this level so we don't
                // lose any information.
                mappedSymbols.AddRange(mappedChildren);
            }
        }

        return mappedSymbols.ToArray();

        bool TryRemapRanges(RazorCSharpDocument csharpDocument, DocumentSymbol documentSymbol)
        {
            if (documentSymbol.Detail == RenderMethodSignature(fileKind))
            {
                // Special case BuildRenderTree to always map to the top of the document
                documentSymbol.Detail = RenderMethodDisplay(fileKind);
                documentSymbol.Range = LspFactory.DefaultRange;
                documentSymbol.SelectionRange = LspFactory.DefaultRange;

                return true;
            }
            else if (_documentMappingService.TryMapToRazorDocumentRange(csharpDocument, documentSymbol.Range, out var newRange) &&
                _documentMappingService.TryMapToRazorDocumentRange(csharpDocument, documentSymbol.SelectionRange, out var newSelectionRange))
            {
                documentSymbol.Range = newRange;
                documentSymbol.SelectionRange = newSelectionRange;

                return true;
            }

            return false;
        }
    }

    private static DocumentSymbol[] MergeDocumentSymbols(DocumentSymbol[]? declarationSymbols, DocumentSymbol[]? implementationSymbols)
    {
        if (declarationSymbols is null)
        {
            return implementationSymbols ?? [];
        }

        if (implementationSymbols is null)
        {
            return declarationSymbols;
        }

        using var _1 = ListPool<DocumentSymbol>.GetPooledObject(out var mergedSymbols);
        using var _2 = DictionaryPool<SymbolData, int>.GetPooledObject(out var symbolIndices);

        AddSymbols(declarationSymbols);
        AddSymbols(implementationSymbols);

        return mergedSymbols.ToArray();

        void AddSymbols(DocumentSymbol[]? symbols)
        {
            if (symbols is null)
            {
                return;
            }

            foreach (var symbol in symbols)
            {
                var key = SymbolData.Create(symbol.Name, symbol.Kind, symbol.SelectionRange);
                if (symbolIndices.TryGetValue(key, out var existingIndex))
                {
                    var existingSymbol = mergedSymbols[existingIndex];
                    if (symbol.Children is [_, ..])
                    {
                        existingSymbol.Children = existingSymbol.Children is [_, ..]
                            ? MergeDocumentSymbols(existingSymbol.Children, symbol.Children)
                            : symbol.Children;
                    }
                }
                else
                {
                    symbolIndices.Add(key, mergedSymbols.Count);
                    mergedSymbols.Add(symbol);
                }
            }
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private static SymbolInformation[] MergeSymbolInformations(
        RazorFileKind fileKind,
        DocumentUri razorDocumentUri,
        SymbolInformation[]? declarationSymbols,
        SymbolInformation[]? implementationSymbols)
    {
        using var _1 = ListPool<SymbolInformation>.GetPooledObject(out var mergedSymbols);
        using var _2 = HashSetPool<SymbolData>.GetPooledObject(out var seenSymbols);
        var foundRenderMethodSymbol = false;

        AddSymbols(declarationSymbols);
        AddSymbols(implementationSymbols);

        if (!foundRenderMethodSymbol)
        {
            mergedSymbols.Insert(0, CreateRenderMethodSymbolInformation(fileKind, razorDocumentUri));
        }

        return mergedSymbols.ToArray();

        void AddSymbols(SymbolInformation[]? symbols)
        {
            if (symbols is null)
            {
                return;
            }

            foreach (var symbol in symbols)
            {
                foundRenderMethodSymbol |= symbol.Name == RenderMethodDisplay(fileKind);
                if (seenSymbols.Add(SymbolData.Create(symbol.Name, symbol.Kind, symbol.Location.Range)))
                {
                    mergedSymbols.Add(symbol);
                }
            }
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static InvalidOperationException CreateUnsupportedResponseTypeException()
    {
        Debug.Fail("Unsupported response type");
        return new InvalidOperationException();
    }

    private static string RenderMethodSignature(RazorFileKind fileKind)
        => fileKind == RazorFileKind.Legacy
            ? "ExecuteAsync()"
            : "BuildRenderTree(RenderTreeBuilder __builder)";

    private static string RenderMethodDisplay(RazorFileKind fileKind)
        => fileKind == RazorFileKind.Legacy
            ? "ExecuteAsync()"
            : "BuildRenderTree()"; // We hide __builder because it can be misleading to users: https://github.com/dotnet/razor/issues/11960

    private static SymbolInformation CreateRenderMethodSymbolInformation(RazorFileKind fileKind, DocumentUri razorDocumentUri)
    {
        // SymbolInformation is obsolete, but things still return it so we have to handle it
#pragma warning disable CS0618 // Type or member is obsolete
        return new SymbolInformation
        {
            Name = RenderMethodDisplay(fileKind),
            Kind = Roslyn.LanguageServer.Protocol.SymbolKind.Method,
            Location = new()
            {
                DocumentUri = razorDocumentUri,
                Range = LspFactory.DefaultRange,
            },
        };
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static DocumentSymbol CreateRenderMethodDocumentSymbol(RazorFileKind fileKind)
        => new()
        {
            Name = RenderMethodDisplay(fileKind),
            Kind = Roslyn.LanguageServer.Protocol.SymbolKind.Method,
            Range = LspFactory.DefaultRange,
            SelectionRange = LspFactory.DefaultRange,
        };

    private readonly record struct SymbolData(
        string Name,
        Roslyn.LanguageServer.Protocol.SymbolKind Kind,
        int StartLine,
        int StartCharacter,
        int EndLine,
        int EndCharacter)
    {
        public static SymbolData Create(string name, Roslyn.LanguageServer.Protocol.SymbolKind kind, LspRange range)
            => new(name, kind, range.Start.Line, range.Start.Character, range.End.Line, range.End.Character);
    }
}
