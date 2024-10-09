// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.InlineHints;
using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers
{
    internal static class InlayHints
    {
        // In the Roslyn LSP server this cache has the same lifetime as the LSP server. For Razor, running OOP, we don't have
        // that same lifetime anywhere, everything is just static. This is likely not ideal, but the inlay hint cache has a
        // max size of 3 items, so it's not a huge deal.
        private static InlayHintCache? s_resolveCache;

        public static Task<InlayHint[]?> GetInlayHintsAsync(Document document, TextDocumentIdentifier textDocumentIdentifier, Range range, bool displayAllOverride, CancellationToken cancellationToken)
        {
            s_resolveCache ??= new();

            // Currently Roslyn options don't sync to OOP so trying to get the real options out of IGlobalOptionsService will
            // always just result in the defaults, which for inline hints are to not show anything. However, the editor has a
            // setting for LSP inlay hints, so we can assume that if we get a request from the client, the user wants hints.
            // When overriding however, Roslyn does a nicer job if type hints are off.
            var options = InlineHintsOptions.Default;
            if (!displayAllOverride)
            {
                options = options with
                {
                    TypeOptions = options.TypeOptions with { EnabledForTypes = true },
                    ParameterOptions = options.ParameterOptions with { EnabledForParameters = true },
                };
            }

            return InlayHintHandler.GetInlayHintsAsync(document, textDocumentIdentifier, range, options, displayAllOverride, s_resolveCache, cancellationToken);
        }

        public static Task<InlayHint> ResolveInlayHintAsync(Document document, InlayHint request, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(s_resolveCache, "Cache should never be null for resolve, since it should have been created by the original request");

            return InlayHintResolveHandler.ResolveInlayHintAsync(document, request, s_resolveCache, cancellationToken);
        }
    }
}
