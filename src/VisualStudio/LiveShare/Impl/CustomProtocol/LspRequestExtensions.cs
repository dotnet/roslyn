// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using LS = Microsoft.VisualStudio.LiveShare.LanguageServices;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Protocol;

public static class LspRequestExtensions
{
    internal static LS.LspRequest<TIn, TOut> ToLSRequest<TIn, TOut>(this LSP.LspRequest<TIn, TOut> lspRequest)
        => new(lspRequest.Name);

    internal static LSP.ClientCapabilities GetClientCapabilities(this LS.RequestContext requestContext)
        => requestContext.ClientCapabilities?.ToObject<LSP.ClientCapabilities>() ?? new LSP.VSInternalClientCapabilities();
}
