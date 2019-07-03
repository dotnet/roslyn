// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.MiscellaneousFiles), Shared]
    internal class MiscSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        public void StartListening(Workspace workspace, object serviceOpt)
        {
            // misc workspace will enable syntax errors and semantic errors for script files for
            // all participating projects in the workspace
            DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax | DiagnosticProvider.Options.ScriptSemantic);
        }

        public void StopListening(Workspace workspace)
        {
            DiagnosticProvider.Disable(workspace);
        }
    }
}
