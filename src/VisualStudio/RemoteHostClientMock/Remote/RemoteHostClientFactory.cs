// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Test.Utilities.Remote;

namespace Roslyn.VisualStudio.DiagnosticsWindow.Remote
{
#pragma warning disable RS0032 // Test exports should not be discoverable
    [ExportWorkspaceService(typeof(IRemoteHostClientFactory), layer: ServiceLayer.Host), Shared]
#pragma warning restore RS0032 // Test exports should not be discoverable
    internal class RemoteHostClientFactory : IRemoteHostClientFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteHostClientFactory()
        {
        }

        public Task<RemoteHostClient> CreateAsync(Workspace workspace, CancellationToken cancellationToken)
        {
            // this is the point where we can create different kind of remote host client in future (cloud or etc)
            if (workspace.Options.GetOption(RemoteHostClientFactoryOptions.RemoteHost_InProc))
            {
                return InProcRemoteHostClient.CreateAsync(workspace, runCacheCleanup: true, cancellationToken: cancellationToken);
            }

            return ServiceHubRemoteHostClient.CreateAsync(workspace, cancellationToken);
        }
    }
}
