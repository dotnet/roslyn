// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal class HostSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        public void StartListening(Workspace workspace, object serviceOpt)
        {
            var registration = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registration.Register(workspace);
        }

        public void StopListening(Workspace workspace)
        {
            // we do this so that we can stop solution crawler faster and fire some telemetry. 
            // this is to reduce a case where we keep going even when VS is shutting down since we don't know about that
            var registration = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registration.Unregister(workspace, blockingShutdown: true);
        }
    }
}
