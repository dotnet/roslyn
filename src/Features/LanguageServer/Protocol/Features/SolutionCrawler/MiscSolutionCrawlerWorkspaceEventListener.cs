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
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.MiscellaneousFiles), Shared]
    internal sealed class MiscSolutionCrawlerWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MiscSolutionCrawlerWorkspaceEventListener(IGlobalOptionService globalOptions)
        {
            _globalOptions = globalOptions;
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            if (_globalOptions.GetOption(SolutionCrawlerRegistrationService.EnableSolutionCrawler))
                DiagnosticProvider.Enable(workspace);
        }

        public void StopListening(Workspace workspace)
            => DiagnosticProvider.Disable(workspace);
    }
}
