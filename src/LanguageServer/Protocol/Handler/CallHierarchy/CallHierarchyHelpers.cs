// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CallHierarchy;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

internal static class CallHierarchyHelpers
{
    public static CallHierarchyResolveData GetResolveData(LSP.CallHierarchyItem item)
    {
        Contract.ThrowIfNull(item.Data);
        var resolveData = JsonSerializer.Deserialize<CallHierarchyResolveData>((JsonElement)item.Data, ProtocolConversions.LspJsonSerializerOptions);
        Contract.ThrowIfNull(resolveData, "Missing data for call hierarchy request");
        return resolveData;
    }

    public static async Task<LSP.CallHierarchyItem?> CreateItemAsync(
        CallHierarchyItemDescriptor descriptor,
        Solution solution,
        CancellationToken cancellationToken)
        => await CreateItemAsync(descriptor, solution, preferredDocumentId: null, cancellationToken).ConfigureAwait(false);

    public static async Task<LSP.CallHierarchyItem?> CreateItemAsync(
        CallHierarchyItemDescriptor descriptor,
        Solution solution,
        DocumentId? preferredDocumentId,
        CancellationToken cancellationToken)
    {
        var resolved = await descriptor.ItemId.TryResolveAsync(solution, cancellationToken).ConfigureAwait(false);
        if (resolved == null)
            return null;

        var (symbol, _) = resolved.Value;
        var sourceInfo = await GetSourceInfoAsync(symbol, solution, preferredDocumentId, cancellationToken).ConfigureAwait(false);
        if (sourceInfo == null)
            return null;

        var (document, declarationSpan, selectionSpan) = sourceInfo.Value;
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

        return new LSP.CallHierarchyItem
        {
            Name = GetName(descriptor),
            Kind = ProtocolConversions.GlyphToSymbolKind(descriptor.Glyph),
            Detail = GetDetail(descriptor),
            Uri = document.GetURI(),
            Range = ProtocolConversions.TextSpanToRange(declarationSpan, text),
            SelectionRange = ProtocolConversions.TextSpanToRange(selectionSpan, text),
            Data = new CallHierarchyResolveData(descriptor.ItemId.SymbolKeyData, descriptor.ItemId.ProjectId.Id, ProtocolConversions.DocumentToTextDocumentIdentifier(document)),
        };

        static string GetName(CallHierarchyItemDescriptor descriptor)
        {
            return string.IsNullOrEmpty(descriptor.ContainingTypeName)
                ? descriptor.MemberName
                : $"{descriptor.ContainingTypeName}.{descriptor.MemberName}";
        }

        static string? GetDetail(CallHierarchyItemDescriptor descriptor)
        {
            if (string.IsNullOrEmpty(descriptor.ContainingTypeName))
                return string.IsNullOrEmpty(descriptor.ContainingNamespaceName) ? null : descriptor.ContainingNamespaceName;

            return string.IsNullOrEmpty(descriptor.ContainingNamespaceName)
                ? descriptor.ContainingTypeName
                : $"{descriptor.ContainingNamespaceName}.{descriptor.ContainingTypeName}";
        }
    }

    public static async Task<ImmutableArray<LSP.Range>> ConvertLocationsToRangesAsync(
        ImmutableArray<Location> locations,
        Document document,
        CancellationToken cancellationToken)
    {
        var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
        return [.. locations.Where(static location => location.IsInSource).Select(location => ProtocolConversions.TextSpanToRange(location.SourceSpan, text))];
    }

    private static async Task<(Document Document, TextSpan DeclarationSpan, TextSpan SelectionSpan)?> GetSourceInfoAsync(
        ISymbol symbol,
        Solution solution,
        DocumentId? preferredDocumentId,
        CancellationToken cancellationToken)
    {
        var sourceInfo = await TryGetSourceInfoAsync(symbol, solution, preferredDocumentId, cancellationToken).ConfigureAwait(false);

        if (sourceInfo == null && symbol is IMethodSymbol { IsImplicitlyDeclared: true, MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } methodSymbol)
        {
            // Implicit constructors don't exist in source, so try to get the source info for the containing type instead.
            sourceInfo = await TryGetSourceInfoAsync(methodSymbol.ContainingType, solution, preferredDocumentId, cancellationToken).ConfigureAwait(false);
        }

        return sourceInfo;
    }

    private static async Task<(Document Document, TextSpan DeclarationSpan, TextSpan SelectionSpan)?> TryGetSourceInfoAsync(
        ISymbol symbol,
        Solution solution,
        DocumentId? preferredDocumentId,
        CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var document = solution.GetDocument(syntaxReference.SyntaxTree);
            if (document == null || (preferredDocumentId != null && document.Id != preferredDocumentId))
                continue;

            var syntax = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            var declarationSpan = syntax.Span;
            var selectionSpan = symbol.Locations.FirstOrDefault(location =>
                location.IsInSource &&
                location.SourceTree == syntax.SyntaxTree &&
                declarationSpan.Contains(location.SourceSpan))?.SourceSpan ?? declarationSpan;

            return (document, declarationSpan, selectionSpan);
        }

        return null;
    }
}
