// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
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
            workspace.WorkspaceChanged += WorkspaceChanged;
        }

        private static void WorkspaceChanged(object? sender, WorkspaceChangeEventArgs e)
        {
            Debug.Assert(sender is Workspace);

            if (e.DocumentId == null)
            {
                return;
            }

            var oldDocument = e.OldSolution.GetDocument(e.DocumentId);
            if (oldDocument == null)
            {
                // document added
                return;
            }

            var newDocument = e.NewSolution.GetDocument(e.DocumentId);
            if (newDocument == null)
            {
                // document removed
                return;
            }

            // When a document is open its loader transitions from file-based loader to text buffer based.
            // The file checksum is no longer available from the latter, so capture it at this moment.
            if (oldDocument.State.TextAndVersionSource.CanReloadText && !newDocument.State.TextAndVersionSource.CanReloadText)
            {
                WorkspaceDocumentLoaderChanged((Workspace)sender, oldDocument);
            }
        }

        public void StopListening(Workspace workspace)
        {
            workspace.WorkspaceChanged -= WorkspaceChanged;
        }

        // NoInlining to avoid loading Debugger.Contracts in certain RPS scenarios.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WorkspaceDocumentLoaderChanged(Workspace workspace, Document document)
        {
            if (DebuggerContractVersionCheck.IsRequiredDebuggerContractVersionAvailable())
            {
                WorkspaceDocumentLoaderChangedImpl(workspace, document);
            }
        }

        // NoInlining to avoid loading proxy type that might require newer version of Debugger.Contracts than available (only applicable to Integration Tests).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WorkspaceDocumentLoaderChangedImpl(Workspace workspace, Document document)
        {
            var proxy = new RemoteEditAndContinueServiceProxy(workspace);
            _ = Task.Run(() => proxy.OnSourceFileUpdatedAsync(document, CancellationToken.None)).ReportNonFatalErrorAsync();
        }
    }
}
