// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

internal class ServiceBrokerShutdown(ServiceBrokerFactory serviceBrokerFactory) : IOnServerShutdown, ILspService
{
    [ExportCSharpVisualBasicLspServiceFactory(typeof(ServiceBrokerShutdown)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private class ServiceBrokerShutdownFactory() : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var serviceBrokerFactory = lspServices.GetRequiredService<ServiceBrokerFactory>();
            return new ServiceBrokerShutdown(serviceBrokerFactory);
        }
    }

    public Task ExitAsync()
    {
        return Task.CompletedTask;
    }

    public async Task ShutdownAsync()
    {
        await serviceBrokerFactory.ShutdownAndWaitForCompletionAsync().ConfigureAwait(false);
    }
}
