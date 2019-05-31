// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportWorkspaceEventListener(WorkspaceKind.Host), Shared]
    internal class HostSolutionCrawlerWorkspaceEventListener : IWorkspaceEventListener
    {
        public void Listen(Workspace workspace)
        {
            var registration = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registration.Register(workspace);
        }

        public void Stop(Workspace workspace)
        {
            // we do this so that we can stop solution crawler faster and fire some telemetry. 
            // this is to reduce a case where we keep going even when VS is shutting down since we don't know about that
            var registration = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registration.Unregister(workspace, blockingShutdown: true);
        }
    }

    [ExportWorkspaceEventListener(WorkspaceKind.MiscellaneousFiles), Shared]
    internal class MiscSolutionCrawlerWorkspaceEventListener : IWorkspaceEventListener
    {
        public void Listen(Workspace workspace)
        {
            // misc workspace will enable syntax errors and semantic errors for script files for
            // all participating projects in the workspace
            DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax | DiagnosticProvider.Options.ScriptSemantic);
        }

        public void Stop(Workspace workspace)
        {
            DiagnosticProvider.Disable(workspace);
        }
    }
}
