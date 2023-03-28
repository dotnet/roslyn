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
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;
using StreamJsonRpc;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

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
            => GetCacheEntry(request).CacheEntry.TextDocumentIdentifier;

        public async Task<LSP.InlayHint> HandleRequestAsync(LSP.InlayHint request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.GetRequiredDocument();
            var (cacheEntry, inlineHintToResolve) = GetCacheEntry(request);

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

        private (InlayHintCache.InlayHintCacheEntry CacheEntry, InlineHint InlineHintToResolve) GetCacheEntry(LSP.InlayHint request)
        {
            var resolveData = (request.Data as JToken)?.ToObject<InlayHintResolveData>();
            Contract.ThrowIfNull(resolveData, "Missing data for inlay hint resolve request");

            var cacheEntry = _inlayHintCache.GetCachedEntry(resolveData.ResultId);
            Contract.ThrowIfNull(cacheEntry, "Missing cache entry for inlay hint resolve request");
            return (cacheEntry, cacheEntry.InlayHintMembers[resolveData.ListIndex]);
        }
    }
}
