// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers
{
    internal static class InlayHints
    {
        // In the Roslyn LSP server this cache has the same lifetime as the LSP server. For Razor, running OOP, we don't have
        // that same lifetime anywhere, everything is just static. This is likely not ideal, but the inlay hint cache has a
        // max size of 3 items, so it's not a huge deal.
        [Obsolete("Use GetInlayHintsAsync with InlayHintCacheWrapper instead.")]
        private static InlayHintCache? s_resolveCache;

        [Obsolete("Use GetInlayHintsAsync with InlayHintCacheWrapper instead.")]
        public static Task<LSP.InlayHint[]?> GetInlayHintsAsync(Document document, LSP.TextDocumentIdentifier textDocumentIdentifier, LSP.Range range, bool displayAllOverride, CancellationToken cancellationToken)
        {
            s_resolveCache ??= new();
            return GetInlayHintsAsync(document, textDocumentIdentifier, range, displayAllOverride, s_resolveCache, cancellationToken);
        }

        public static Task<LSP.InlayHint[]?> GetInlayHintsAsync(Document document, LSP.TextDocumentIdentifier textDocumentIdentifier, LSP.Range range, bool displayAllOverride, InlayHintCacheWrapper cacheWrapper, CancellationToken cancellationToken)
        {
            return GetInlayHintsAsync(document, textDocumentIdentifier, range, displayAllOverride, cacheWrapper.GetCache(), cancellationToken);
        }

        private static Task<LSP.InlayHint[]?> GetInlayHintsAsync(Document document, LSP.TextDocumentIdentifier textDocumentIdentifier, LSP.Range range, bool displayAllOverride, InlayHintCache cache, CancellationToken cancellationToken)
        {
            // Currently Roslyn options don't sync to OOP so trying to get the real options out of IGlobalOptionsService will
            // always just result in the defaults, which for inline hints are to not show anything. However, the editor has a
            // setting for LSP inlay hints, so we can assume that if we get a request from the client, the user wants hints.
            // When overriding however, Roslyn does a nicer job if type hints are off.
            var options = GetOptions(displayAllOverride);

            return InlayHintHandler.GetInlayHintsAsync(document, textDocumentIdentifier, range, options, displayAllOverride, cache, cancellationToken);
        }

        [Obsolete("Use GetInlayHintsAsync with InlayHintCacheWrapper instead.")]
        public static Task<LSP.InlayHint> ResolveInlayHintAsync(Document document, LSP.InlayHint request, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(s_resolveCache, "Cache should never be null for resolve, since it should have been created by the original request");
            return ResolveInlayHintAsync(document, request, s_resolveCache, cancellationToken);
        }

        public static Task<LSP.InlayHint> ResolveInlayHintAsync(Document document, LSP.InlayHint request, InlayHintCacheWrapper cacheWrapper, CancellationToken cancellationToken)
        {
            return ResolveInlayHintAsync(document, request, cacheWrapper.GetCache(), cancellationToken);
        }

        private static Task<LSP.InlayHint> ResolveInlayHintAsync(Document document, LSP.InlayHint request, InlayHintCache cache, CancellationToken cancellationToken)
        {
            var data = InlayHintResolveHandler.GetInlayHintResolveData(request);
            var options = GetOptions(data.DisplayAllOverride);
            return InlayHintResolveHandler.ResolveInlayHintAsync(document, request, cache, data, options, cancellationToken);
        }

        private static InlineHintsOptions GetOptions(bool displayAllOverride)
        {
            var options = InlineHintsOptions.Default;
            if (!displayAllOverride)
            {
                options = options with
                {
                    TypeOptions = options.TypeOptions with { EnabledForTypes = true },
                    ParameterOptions = options.ParameterOptions with { EnabledForParameters = true },
                };
            }

            return options;
        }
    }
}
