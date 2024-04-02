// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal interface IRazorCohostTextDocumentSyncHandler
{
    Task HandleAsync(int version, RazorCohostRequestContext context, CancellationToken cancellationToken);
}

internal static class IRazorCohostTextDocumentSyncHandlerExtensions
{
    public static async Task NotifyRazorAsync(this IRazorCohostTextDocumentSyncHandler? openOrChangeHandler, Uri uri, int version, RequestContext context, CancellationToken cancellationToken)
    {
        if (openOrChangeHandler is null)
            return;

        // Razor is a little special here, because when a .razor or .cshtml document is opened/changed, which is what this request is for,
        // they need to generate a C# and/or Html document. To do this they use the Razor Source Generator, but to run the generator they
        // need a TextDocument for the Razor file that this request is for. To facilitate that need we create a RequestContext here
        // and pass it to Razor, which is essentially the same as the RequestContext they would get on the next request, but by providing it
        // early here, they can do their generation before the didOpen/didChange/didClose mutating request is finished.
        // This is a little hacky, but it's the best we can do for now. In future hopefully we can switch to a pull model where the source
        // generator just provides C# source to the project/compilation as normal. Whether we need to maintain this system for the Html
        // generated documents remains to be seen.
        var clientCapabilitiesManager = context.GetRequiredService<IInitializeManager>();
        var clientCapabilities = clientCapabilitiesManager.TryGetClientCapabilities();
        var logger = context.GetRequiredService<AbstractLspLogger>();
        var serverInfoProvider = context.GetRequiredService<ServerInfoProvider>();
        var supportedLanguages = serverInfoProvider.SupportedLanguages;
        var lspServices = context.GetRequiredService<ILspServices>();

        var newContext = await RequestContext.CreateAsync(false, true, new TextDocumentIdentifier() { Uri = uri }, LanguageServer.WellKnownLspServerKinds.RazorCohostServer, clientCapabilities, supportedLanguages, lspServices, logger, context.Method, cancellationToken).ConfigureAwait(false);

        var razorContext = new RazorCohostRequestContext(newContext);

        await openOrChangeHandler.HandleAsync(version, razorContext, cancellationToken).ConfigureAwait(false);
    }
}
