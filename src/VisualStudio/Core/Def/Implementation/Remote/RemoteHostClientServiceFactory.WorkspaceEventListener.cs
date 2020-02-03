// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
