// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;

public static class LspRequestExtensions
{
    extension<TIn, TOut>(LSP.LspRequest<TIn, TOut> lspRequest)
    {
        internal LS.LspRequest<TIn, TOut> ToLSRequest()
        => new LS.LspRequest<TIn, TOut>(lspRequest.Name);
    }

    extension(LS.RequestContext requestContext)
    {
        internal LSP.ClientCapabilities GetClientCapabilities()
        => requestContext.ClientCapabilities?.ToObject<LSP.ClientCapabilities>() ?? new LSP.VSInternalClientCapabilities();
    }
}
