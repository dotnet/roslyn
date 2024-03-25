// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.Handler.CodeLens;
using Roslyn.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
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
            => GetTextDocument(request.Data) ?? throw new ArgumentException();

        public async Task<LSP.InlayHint> HandleRequestAsync(LSP.InlayHint request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var resolveData = GetInlayHintResolveData(request);
            var (cacheEntry, inlineHintToResolve) = GetCacheEntry(resolveData);

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

        private static LSP.TextDocumentIdentifier? GetTextDocument(object? requestData)
        {
            Contract.ThrowIfNull(requestData);
            var resolveData = ((JToken)requestData).ToObject<DocumentResolveData>();
            return resolveData?.TextDocument;
        }

        private (InlayHintCache.InlayHintCacheEntry CacheEntry, InlineHint InlineHintToResolve) GetCacheEntry(InlayHintResolveData resolveData)
        {
            var cacheEntry = _inlayHintCache.GetCachedEntry(resolveData.ResultId);
            Contract.ThrowIfNull(cacheEntry, "Missing cache entry for inlay hint resolve request");
            return (cacheEntry, cacheEntry.InlayHintMembers[resolveData.ListIndex]);
        }

        private static InlayHintResolveData GetInlayHintResolveData(LSP.InlayHint inlayHint)
        {
            var resolveData = (inlayHint.Data as JToken)?.ToObject<InlayHintResolveData>();
            Contract.ThrowIfNull(resolveData, "Missing data for inlay hint resolve request");
            return resolveData;
        }
    }
}
