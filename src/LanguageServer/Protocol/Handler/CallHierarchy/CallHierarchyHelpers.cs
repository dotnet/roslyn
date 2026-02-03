// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CallHierarchy;

internal static class CallHierarchyHelpers
{
    /// <summary>
    /// Extracts CallHierarchyItemData from the LSP CallHierarchyItem.Data field.
    /// </summary>
    public static CallHierarchyItemData? GetCallHierarchyItemData(LSP.CallHierarchyItem item)
    {
        if (item.Data == null)
            return null;

        try
        {
            // The Data field is a JsonElement when deserialized
            if (item.Data is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<CallHierarchyItemData>(jsonElement.GetRawText());
            }

            // If it's already our type, return it
            if (item.Data is CallHierarchyItemData data)
            {
                return data;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reconstructs a CallHierarchyItem from the serialized data.
    /// </summary>
    public static async Task<CodeAnalysis.CallHierarchy.CallHierarchyItem?> ReconstructCallHierarchyItemAsync(
        CallHierarchyItemData itemData,
        Solution solution,
        CancellationToken cancellationToken)
    {
        try
        {
            var projectId = ProjectId.CreateFromSerialized(Guid.Parse(itemData.ProjectId));
            var documentId = DocumentId.CreateFromSerialized(projectId, Guid.Parse(itemData.DocumentId));
            var document = solution.GetDocument(documentId);
            if (document == null)
                return null;

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation == null)
                return null;

            var symbolKey = SymbolKey.ResolveString(itemData.SymbolKey, compilation, cancellationToken: cancellationToken);
            var symbol = symbolKey.Symbol;
            if (symbol == null)
                return null;

            var span = new TextSpan(itemData.SpanStart, itemData.SpanLength);

            return new CodeAnalysis.CallHierarchy.CallHierarchyItem(
                symbolKey: SymbolKey.Create(symbol, cancellationToken),
                name: symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                kind: symbol.Kind,
                detail: symbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? "",
                containingNamespace: symbol.ContainingNamespace?.ToDisplayString() ?? "",
                projectId: projectId,
                documentId: documentId,
                span: span);
        }
        catch
        {
            return null;
        }
    }
}
