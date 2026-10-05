// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

[ExportCSharpVisualBasicLspService(typeof(ServiceBrokerShutdown)), Shared(LspServiceComposition.SharingBoundary)]
internal class ServiceBrokerShutdown : IOnServerShutdown, ILspService
{
    private readonly ServiceBrokerFactory _serviceBrokerFactory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServiceBrokerShutdown(LspService<ServiceBrokerFactory> serviceBrokerFactory)
    {
        _serviceBrokerFactory = serviceBrokerFactory.Value;
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        await _serviceBrokerFactory.ShutdownAndWaitForCompletionAsync().ConfigureAwait(false);
    }
}
