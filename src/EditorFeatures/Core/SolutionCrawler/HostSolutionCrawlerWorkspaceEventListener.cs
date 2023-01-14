// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed class HostSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public HostSolutionCrawlerWorkspaceEventListener(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public void StartListening(Workspace workspace, object? serviceOpt)
        {
            if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
            {
                workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>().Register(workspace);
            }
        }

        public void StopListening(Workspace workspace)
        {
            // we do this so that we can stop solution crawler faster and fire some telemetry. 
            // this is to reduce a case where we keep going even when VS is shutting down since we don't know about that
            var registration = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
            registration.Unregister(workspace, blockingShutdown: true);
        }
    }
}
