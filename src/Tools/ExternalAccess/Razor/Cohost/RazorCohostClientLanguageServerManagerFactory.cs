// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

[ExportRazorLspServiceFactory(typeof(IRazorCohostClientLanguageServerManager)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class RazorCohostClientLanguageServerManagerFactory() : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
    {
        var notificationManager = lspServices.GetRequiredService<IClientLanguageServerManager>();

        return new RazorCohostClientLanguageServerManager(notificationManager);
    }

    internal class RazorCohostClientLanguageServerManager(IClientLanguageServerManager clientLanguageServerManager) : IRazorCohostClientLanguageServerManager
    {
        public Task<TResponse> SendRequestAsync<TParams, TResponse>(string methodName, TParams @params, CancellationToken cancellationToken)
            => clientLanguageServerManager.SendRequestAsync<TParams, TResponse>(methodName, @params, cancellationToken);

        public ValueTask SendRequestAsync(string methodName, CancellationToken cancellationToken)
            => clientLanguageServerManager.SendRequestAsync(methodName, cancellationToken);

        public ValueTask SendRequestAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
            => clientLanguageServerManager.SendRequestAsync<TParams>(methodName, @params, cancellationToken);

        public ValueTask SendNotificationAsync(string methodName, CancellationToken cancellationToken)
            => clientLanguageServerManager.SendNotificationAsync(methodName, cancellationToken);

        public ValueTask SendNotificationAsync<TParams>(string methodName, TParams @params, CancellationToken cancellationToken)
            => clientLanguageServerManager.SendNotificationAsync(methodName, @params, cancellationToken);
    }
}
