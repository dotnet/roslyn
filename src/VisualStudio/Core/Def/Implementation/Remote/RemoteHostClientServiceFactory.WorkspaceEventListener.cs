// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class RemoteHostClientServiceFactory : IWorkspaceServiceFactory
    {
        [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
        private sealed class RemoteHostWorkspaceEventListener : IEventListener<object>
        {
            public void StartListening(Workspace workspace, object serviceOpt)
            {
                var service = (RemoteHostClientService)workspace.Services.GetService<IRemoteHostClientService>();
                service.Enable();
            }
        }
    }
}
