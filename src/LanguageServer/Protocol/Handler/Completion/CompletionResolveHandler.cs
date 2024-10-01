// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Handle a completion resolve request to add description.
    /// </summary>
    /// <remarks>
    /// This isn't a <see cref="ILspServiceDocumentRequestHandler{TRequest, TResponse}" /> because it could return null.
    /// </remarks>
    [Method(LSP.Methods.TextDocumentCompletionResolveName)]
    internal sealed class CompletionResolveHandler : ILspServiceRequestHandler<LSP.CompletionItem, LSP.CompletionItem>, ITextDocumentIdentifierHandler<LSP.CompletionItem, LSP.TextDocumentIdentifier?>
    {
        private readonly CompletionListCache _completionListCache;
        private readonly IGlobalOptionService _globalOptions;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        public CompletionResolveHandler(IGlobalOptionService globalOptions, CompletionListCache completionListCache)
        {
            _globalOptions = globalOptions;
            _completionListCache = completionListCache;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionItem request)
            => CompletionResolveHandler.GetTextDocumentCacheEntry(request);

        public async Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
        {
            var cacheEntry = GetCompletionListCacheEntry(completionItem);
            if (cacheEntry == null)
            {
                // Don't have a cache associated with this completion item, cannot resolve.
                context.TraceInformation("No cache entry found for the provided completion item at resolve time.");
                return completionItem;
            }

            var document = context.GetRequiredDocument();
            var completionService = document.Project.Services.GetRequiredService<CompletionService>();

            // Find the matching completion item in the completion list
            var selectedItem = cacheEntry.CompletionList.ItemsList.FirstOrDefault(cachedCompletionItem => MatchesLSPCompletionItem(completionItem, cachedCompletionItem));

            var completionOptions = _globalOptions.GetCompletionOptions(document.Project.Language);
            var symbolDescriptionOptions = _globalOptions.GetSymbolDescriptionOptions(document.Project.Language);

            if (selectedItem is not null)
            {
                var creationService = document.Project.Solution.Services.GetRequiredService<ILspCompletionResultCreationService>();
                await creationService.ResolveAsync(
                    completionItem,
                    selectedItem,
                    ProtocolConversions.DocumentToTextDocumentIdentifier(document),
                    document,
                    new CompletionCapabilityHelper(context.GetRequiredClientCapabilities()),
                    completionService,
                    completionOptions,
                    symbolDescriptionOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            return completionItem;
        }

        private static bool MatchesLSPCompletionItem(LSP.CompletionItem lspCompletionItem, CompletionItem completionItem)
        {
            // We want to make sure we are resolving the same unimported item in case we have multiple with same name
            // but from different namespaces. However, VSCode doesn't include labelDetails in the resolve request, so we 
            // compare SortText instead when it's set (which is when label != SortText)
            return lspCompletionItem.Label == completionItem.GetEntireDisplayText()
                && (lspCompletionItem.SortText is null || lspCompletionItem.SortText == completionItem.SortText);
        }

        private static LSP.TextDocumentIdentifier? GetTextDocumentCacheEntry(LSP.CompletionItem request)
        {
            Contract.ThrowIfNull(request.Data);
            var resolveData = JsonSerializer.Deserialize<DocumentResolveData>((JsonElement)request.Data);
            if (resolveData is null)
            {
                Contract.Fail("Document should always be provided when resolving a completion item request.");
                return null;
            }

            return resolveData.TextDocument;
        }

        private CompletionListCache.CacheEntry? GetCompletionListCacheEntry(LSP.CompletionItem request)
        {
            Contract.ThrowIfNull(request.Data);
            var resolveData = JsonSerializer.Deserialize<CompletionResolveData>((JsonElement)request.Data, ProtocolConversions.LspJsonSerializerOptions);
            if (resolveData?.ResultId == null)
            {
                Contract.Fail("Result id should always be provided when resolving a completion item we returned.");
                return null;
            }

            var cacheEntry = _completionListCache.GetCachedEntry(resolveData.ResultId);
            if (cacheEntry == null)
            {
                // No cache for associated completion item. Log some telemetry so we can understand how frequently this actually happens.
                Logger.Log(FunctionId.LSP_CompletionListCacheMiss, KeyValueLogMessage.NoProperty);
            }

            return cacheEntry;
        }
    }
}
