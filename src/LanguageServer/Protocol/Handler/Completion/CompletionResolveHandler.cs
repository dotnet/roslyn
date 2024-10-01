// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Host.Mef;
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
    [ExportCSharpVisualBasicStatelessLspService(typeof(CompletionResolveHandler)), Shared]
    [Method(LSP.Methods.TextDocumentCompletionResolveName)]
    internal sealed class CompletionResolveHandler : ILspServiceRequestHandler<LSP.CompletionItem, LSP.CompletionItem>, ITextDocumentIdentifierHandler<LSP.CompletionItem, LSP.TextDocumentIdentifier?>
    {
        private readonly IGlobalOptionService _globalOptions;

        public bool MutatesSolutionState => false;
        public bool RequiresLSPSolution => true;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CompletionResolveHandler(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.CompletionItem request)
            => GetTextDocumentCacheEntry(request);

        public Task<LSP.CompletionItem> HandleRequestAsync(LSP.CompletionItem completionItem, RequestContext context, CancellationToken cancellationToken)
        {
            var completionListCache = context.GetRequiredLspService<CompletionListCache>();

            if (!completionListCache.TryGetCompletionListCacheEntry(completionItem, out var cacheEntry))
            {
                // Don't have a cache associated with this completion item, cannot resolve.
                context.TraceInformation("No cache entry found for the provided completion item at resolve time.");
                return Task.FromResult(completionItem);
            }

            var document = context.GetRequiredDocument();
            var capabilityHelper = new CompletionCapabilityHelper(context.GetRequiredClientCapabilities());

            return ResolveCompletionItemAsync(
                completionItem, cacheEntry.CompletionList, document, _globalOptions, capabilityHelper, cancellationToken);
        }

        public static Task<LSP.CompletionItem> ResolveCompletionItemAsync(
            LSP.CompletionItem completionItem,
            Document document,
            IGlobalOptionService globalOptions,
            CompletionCapabilityHelper capabilityHelper,
            CompletionListCache completionListCache,
            CancellationToken cancellationToken)
        {
            if (!completionListCache.TryGetCompletionListCacheEntry(completionItem, out var cacheEntry))
            {
                // Don't have a cache associated with this completion item, cannot resolve.
                return Task.FromResult(completionItem);
            }

            return ResolveCompletionItemAsync(
                completionItem, cacheEntry.CompletionList, document, globalOptions, capabilityHelper, cancellationToken);
        }

        private static async Task<LSP.CompletionItem> ResolveCompletionItemAsync(
            LSP.CompletionItem completionItem,
            CompletionList cachedCompletionList,
            Document document,
            IGlobalOptionService globalOptions,
            CompletionCapabilityHelper capabilityHelper,
            CancellationToken cancellationToken)
        {
            // Find the matching completion item in the completion list
            var roslynItem = cachedCompletionList.ItemsList
                .FirstOrDefault(cachedCompletionItem => MatchesLSPCompletionItem(completionItem, cachedCompletionItem));

            if (roslynItem is null)
            {
                return completionItem;
            }

            var completionOptions = globalOptions.GetCompletionOptions(document.Project.Language);
            var symbolDescriptionOptions = globalOptions.GetSymbolDescriptionOptions(document.Project.Language);
            var completionService = document.Project.Services.GetRequiredService<CompletionService>();

            return await CompletionResultFactory.ResolveAsync(
                completionItem,
                roslynItem,
                ProtocolConversions.DocumentToTextDocumentIdentifier(document),
                document,
                capabilityHelper,
                completionService,
                completionOptions,
                symbolDescriptionOptions,
                cancellationToken).ConfigureAwait(false);
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
    }
}
