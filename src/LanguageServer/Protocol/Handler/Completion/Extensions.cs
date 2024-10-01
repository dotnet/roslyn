// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;

internal static class Extensions
{
    public static CompletionOptions GetCompletionOptionsForLsp(this IGlobalOptionService globalOptionService, string language, CompletionCapabilityHelper capabilityHelper)
    {
        var options = globalOptionService.GetCompletionOptions(language);

        if (capabilityHelper.SupportVSInternalClientCapabilities)
        {
            // Filter out unimported types for now as there are two issues with providing them:
            // 1.  LSP client does not currently provide a way to provide detail text on the completion item to show the namespace.
            //     https://dev.azure.com/devdiv/DevDiv/_workitems/edit/1076759
            // 2.  We need to figure out how to provide the text edits along with the completion item or provide them in the resolve request.
            //     https://devdiv.visualstudio.com/DevDiv/_workitems/edit/985860/
            // 3.  LSP client should support completion filters / expanders
            options = options with
            {
                ShowItemsFromUnimportedNamespaces = false,
                ExpandedCompletionBehavior = ExpandedCompletionMode.NonExpandedItemsOnly,
                UpdateImportCompletionCacheInBackground = false,
            };
        }
        else
        {
            var updateImportCompletionCacheInBackground = options.ShowItemsFromUnimportedNamespaces is true;
            options = options with
            {
                ShowNewSnippetExperienceUserOption = false,
                UpdateImportCompletionCacheInBackground = updateImportCompletionCacheInBackground
            };
        }

        return options;
    }

    public static bool TryGetCompletionListCacheEntry(
        this CompletionListCache completionListCache,
        LSP.CompletionItem request,
        [NotNullWhen(true)] out CompletionListCache.CacheEntry? cacheEntry)
    {
        Contract.ThrowIfNull(request.Data);
        var resolveData = JsonSerializer.Deserialize<CompletionResolveData>((JsonElement)request.Data, ProtocolConversions.LspJsonSerializerOptions);
        if (resolveData?.ResultId is null)
        {
            Contract.Fail("Result id should always be provided when resolving a completion item we returned.");
            cacheEntry = null;
            return false;
        }

        cacheEntry = completionListCache.GetCachedEntry(resolveData.ResultId);
        if (cacheEntry is null)
        {
            // No cache for associated completion item. Log some telemetry so we can understand how frequently this actually happens.
            Logger.Log(FunctionId.LSP_CompletionListCacheMiss, KeyValueLogMessage.NoProperty);
        }

        return cacheEntry is not null;
    }
}
