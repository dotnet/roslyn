// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.MiscellaneousFiles), Shared]
    internal class MiscSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscSolutionCrawlerWorkspaceEventListener()
        {
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            // misc workspace will enable syntax errors and semantic errors for script files for
            // all participating projects in the workspace
            DiagnosticProvider.Enable(workspace, DiagnosticProvider.Options.Syntax | DiagnosticProvider.Options.ScriptSemantic);
        }

        public void StopListening(Workspace workspace)
            => DiagnosticProvider.Disable(workspace);
    }
}
