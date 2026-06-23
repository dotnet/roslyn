// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.TypeHierarchy;

internal static class TypeHierarchyHelpers
{
    public static TypeHierarchyResolveData GetResolveData(LSP.TypeHierarchyItem item)
    {
        Contract.ThrowIfNull(item.Data);
        var resolveData = JsonSerializer.Deserialize<TypeHierarchyResolveData>((JsonElement)item.Data, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for type hierarchy request");
        return resolveData;
    }

    public static async Task<INamedTypeSymbol?> GetTypeSymbolAsync(
        LSP.TypeHierarchyItem item,
        Solution solution,
        CancellationToken cancellationToken)
    {
        var resolveData = GetResolveData(item);
        var project = solution.GetProject(resolveData.GetProjectId());
        if (project == null)
            return null;

        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation == null)
            return null;

        var symbol = SymbolKey.ResolveString(resolveData.SymbolKeyData, compilation, cancellationToken: cancellationToken).GetAnySymbol();
        return symbol as INamedTypeSymbol;
    }

    public static async Task<LSP.TypeHierarchyItem?> CreateItemAsync(
        INamedTypeSymbol symbol,
        Solution solution,
        CancellationToken cancellationToken)
        => await CreateItemAsync(symbol, solution, preferredDocumentId: null, cancellationToken).ConfigureAwait(false);

    public static async Task<LSP.TypeHierarchyItem?> CreateItemAsync(
        INamedTypeSymbol symbol,
        Solution solution,
        DocumentId? preferredDocumentId,
        CancellationToken cancellationToken)
    {
        var sourceInfo = await TryGetSourceInfoAsync(symbol, solution, preferredDocumentId, cancellationToken).ConfigureAwait(false);
        if (sourceInfo == null)
            return null;

        var (document, declarationSpan, selectionSpan) = sourceInfo.Value;
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        var name = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var detail = symbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : symbol.ContainingNamespace?.ToDisplayString();

        return new LSP.TypeHierarchyItem
        {
            Name = name,
            Kind = ProtocolConversions.GlyphToSymbolKind(symbol.GetGlyph()),
            Detail = detail,
            Uri = document.GetURI(),
            Range = ProtocolConversions.TextSpanToRange(declarationSpan, text),
            SelectionRange = ProtocolConversions.TextSpanToRange(selectionSpan, text),
            Data = new TypeHierarchyResolveData(
                SymbolKey.CreateString(symbol, cancellationToken),
                document.Project.Id.Id,
                ProtocolConversions.DocumentToTextDocumentIdentifier(document)),
        };
    }

    private static async Task<(Document Document, TextSpan DeclarationSpan, TextSpan SelectionSpan)?> TryGetSourceInfoAsync(
        INamedTypeSymbol symbol,
        Solution solution,
        DocumentId? preferredDocumentId,
        CancellationToken cancellationToken)
    {
        (Document Document, TextSpan DeclarationSpan, TextSpan SelectionSpan)? fallbackSourceInfo = null;

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var document = solution.GetDocument(syntaxReference.SyntaxTree);
            if (document == null)
                continue;

            var syntax = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var declarationSpan = syntax.Span;
            var selectionSpan = symbol.Locations.FirstOrDefault(location =>
                location.IsInSource &&
                location.SourceTree == syntax.SyntaxTree &&
                declarationSpan.Contains(location.SourceSpan))?.SourceSpan ?? declarationSpan;

            var sourceInfo = (document, declarationSpan, selectionSpan);

            // Prefer a declaration in the request document when possible, but
            // fall back to any source declaration when the symbol is declared elsewhere.
            if (preferredDocumentId == null || document.Id == preferredDocumentId)
                return sourceInfo;

            fallbackSourceInfo ??= sourceInfo;
        }

        return fallbackSourceInfo;
    }
}
