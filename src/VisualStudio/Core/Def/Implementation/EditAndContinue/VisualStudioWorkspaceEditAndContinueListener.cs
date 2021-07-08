// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.EditAndContinue
{
    /// <summary>
    /// Connects <see cref="VisualStudioWorkspace"/> to the ServiceHub services.
    /// Launches ServiceHub if it is not running yet and starts services that push information from <see cref="VisualStudioWorkspace"/> to the ServiceHub process.
    /// </summary>
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed class VisualStudioWorkspaceEditAndContinueListener : IEventListener<object>, IEventListenerStoppable
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceEditAndContinueListener()
        {
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            if (workspace is not VisualStudioWorkspace)
            {
                return;
            }

            workspace.DocumentOpened += WorkspaceDocumentOpened;
        }

        public void StopListening(Workspace workspace)
        {
            if (workspace is not VisualStudioWorkspace)
            {
                return;
            }

            workspace.DocumentOpened -= WorkspaceDocumentOpened;
        }

        private void WorkspaceDocumentOpened(object? sender, DocumentEventArgs e)
        {
            var proxy = new RemoteEditAndContinueServiceProxy(e.Document.Project.Solution.Workspace);
            _ = Task.Run(() => proxy.OnSourceFileUpdatedAsync(e.Document, CancellationToken.None)).ReportNonFatalErrorAsync();
        }
    }
}
