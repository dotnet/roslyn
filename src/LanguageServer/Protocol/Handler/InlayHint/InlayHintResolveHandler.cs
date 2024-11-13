// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint
{
    [Method(Methods.InlayHintResolveName)]
    internal sealed class InlayHintResolveHandler : ILspServiceDocumentRequestHandler<LSP.InlayHint, LSP.InlayHint>
    {
        private readonly InlayHintCache _inlayHintCache;

        public InlayHintResolveHandler(InlayHintCache inlayHintCache)
        {
            _inlayHintCache = inlayHintCache;
        }

        public bool MutatesSolutionState => false;

        public bool RequiresLSPSolution => true;

        public TextDocumentIdentifier GetTextDocumentIdentifier(LSP.InlayHint request)
            => GetInlayHintResolveData(request).TextDocument;

        public Task<LSP.InlayHint> HandleRequestAsync(LSP.InlayHint request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            return ResolveInlayHintAsync(document, request, _inlayHintCache, cancellationToken);
        }

        internal static async Task<LSP.InlayHint> ResolveInlayHintAsync(Document document, LSP.InlayHint request, InlayHintCache inlayHintCache, CancellationToken cancellationToken)
        {
            var resolveData = GetInlayHintResolveData(request);
            var (cacheEntry, inlineHintToResolve) = GetCacheEntry(resolveData, inlayHintCache);

            var currentSyntaxVersion = await document.GetSyntaxVersionAsync(cancellationToken).ConfigureAwait(false);
            var cachedSyntaxVersion = cacheEntry.SyntaxVersion;

            if (currentSyntaxVersion != cachedSyntaxVersion)
            {
                throw new LocalRpcException($"Cached resolve version {cachedSyntaxVersion} does not match current version {currentSyntaxVersion}")
                {
                    ErrorCode = LspErrorCodes.ContentModified
                };
            }

            var taggedText = await inlineHintToResolve.GetDescriptionAsync(document, cancellationToken).ConfigureAwait(false);

            request.ToolTip = ProtocolConversions.GetDocumentationMarkupContent(taggedText, document, true);
            return request;
        }

        private static (InlayHintCache.InlayHintCacheEntry CacheEntry, InlineHint InlineHintToResolve) GetCacheEntry(InlayHintResolveData resolveData, InlayHintCache inlayHintCache)
        {
            var cacheEntry = inlayHintCache.GetCachedEntry(resolveData.ResultId);
            Contract.ThrowIfNull(cacheEntry, "Missing cache entry for inlay hint resolve request");
            return (cacheEntry, cacheEntry.InlayHintMembers[resolveData.ListIndex]);
        }

        private static InlayHintResolveData GetInlayHintResolveData(LSP.InlayHint inlayHint)
        {
            Contract.ThrowIfNull(inlayHint.Data);
            var resolveData = JsonSerializer.Deserialize<InlayHintResolveData>((JsonElement)inlayHint.Data, ProtocolConversions.LspJsonSerializerOptions);
            Contract.ThrowIfNull(resolveData, "Missing data for inlay hint resolve request");
            return resolveData;
        }
    }
}
