// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Notifies EnC service of host workspace events.
    /// </summary>
    [ExportEventListener(WellKnownEventListeners.Workspace, WorkspaceKind.Host), Shared]
    internal sealed class EditAndContinueHostWorkspaceEventListener : IEventListener<object>, IEventListenerStoppable
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditAndContinueHostWorkspaceEventListener()
        {
        }

        public void StartListening(Workspace workspace, object serviceOpt)
        {
            workspace.DocumentOpened += WorkspaceDocumentOpened;
        }

        public void StopListening(Workspace workspace)
        {
            workspace.DocumentOpened -= WorkspaceDocumentOpened;
        }

        private void WorkspaceDocumentOpened(object? sender, DocumentEventArgs e)
        {
            if (!DebuggerContractVersionCheck.IsRequiredDebuggerContractVersionAvailable())
            {
                return;
            }

            WorkspaceDocumentOpenedImpl(e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WorkspaceDocumentOpenedImpl(DocumentEventArgs e)
        {
            var proxy = new RemoteEditAndContinueServiceProxy(e.Document.Project.Solution.Workspace);
            _ = Task.Run(() => proxy.OnSourceFileUpdatedAsync(e.Document, CancellationToken.None)).ReportNonFatalErrorAsync();
        }
    }
}
