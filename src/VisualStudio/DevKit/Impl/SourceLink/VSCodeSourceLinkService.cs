// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Debugger.Contracts.SourceLink;
using Microsoft.VisualStudio.Debugger.Contracts.SymbolLocator;
using Microsoft.VisualStudio.LanguageServices.PdbSourceDocument;

namespace Microsoft.CodeAnalysis.LanguageServer.Services.SourceLink;

internal sealed class VSCodeSourceLinkService(IServiceBroker serviceBroker, IPdbSourceDocumentLogger? logger) : AbstractSourceLinkService
{
    private readonly IServiceBroker _serviceBroker = serviceBroker;

    protected override async Task<SymbolLocatorResult?> LocateSymbolFileAsync(SymbolLocatorPdbInfo pdbInfo, SymbolLocatorSearchFlags flags, CancellationToken cancellationToken)
    {
        var proxy = await _serviceBroker.GetProxyAsync<IDebuggerSymbolLocatorService>(BrokeredServiceDescriptors.DebuggerSymbolLocatorService, cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)proxy)
        {
            if (proxy is null)
            {
                return null;
            }

            try
            {
                var result = await proxy.LocateSymbolFileAsync(pdbInfo, flags, progress: null, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (StreamJsonRpc.RemoteMethodNotFoundException)
            {
                // Older versions of DevKit use an invalid service descriptor - calling it will throw a RemoteMethodNotFoundException.
                // Just return null as there isn't a valid service available.
                return null;
            }
        }
    }

    protected override async Task<SourceLinkResult?> GetSourceLinkAsync(string url, string relativePath, CancellationToken cancellationToken)
    {
        var proxy = await _serviceBroker.GetProxyAsync<IDebuggerSourceLinkService>(BrokeredServiceDescriptors.DebuggerSourceLinkService, cancellationToken).ConfigureAwait(false);
        using ((IDisposable?)proxy)
        {
            if (proxy is null)
            {
                return null;
            }

            try
            {
                var result = await proxy.GetSourceLinkAsync(url, relativePath, allowInteractiveLogin: false, cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (StreamJsonRpc.RemoteMethodNotFoundException)
            {
                // Older versions of DevKit use an invalid service descriptor - calling it will throw a RemoteMethodNotFoundException.
                // Just return null as there isn't a valid service available.
                return null;
            }
        }
    }

    protected override IPdbSourceDocumentLogger? Logger => logger;
}
