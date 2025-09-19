// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal sealed partial class GlobalUndoServiceFactory
{
    private sealed class WorkspaceUndoTransaction : IWorkspaceGlobalUndoTransaction
    {
        private readonly IThreadingContext _threadingContext;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IVsLinkedUndoTransactionManager _undoManager;
        private readonly Workspace _workspace;
        private readonly string _description;
        private readonly GlobalUndoService _service;

        // indicate whether undo transaction is currently active
        private bool _transactionAlive;

        public WorkspaceUndoTransaction(
            IThreadingContext threadingContext,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IVsLinkedUndoTransactionManager undoManager,
            Workspace workspace,
            string description,
            GlobalUndoService service)
        {
            _threadingContext = threadingContext;
            _undoHistoryRegistry = undoHistoryRegistry;
            _undoManager = undoManager;
            _workspace = workspace;
            _description = description;
            _service = service;

            _threadingContext.ThrowIfNotOnUIThread();

            Marshal.ThrowExceptionForHR(_undoManager.OpenLinkedUndo((uint)LinkedTransactionFlags2.mdtGlobal, _description));
            _transactionAlive = true;
        }

        public void AddDocument(DocumentId id)
        {
            var visualStudioWorkspace = (VisualStudioWorkspace)_workspace;
            Contract.ThrowIfNull(visualStudioWorkspace);

            var solution = visualStudioWorkspace.CurrentSolution;
            var document = solution.GetDocument(id);
            if (document == null)
            {
                // document is not part of the workspace (newly created document that is not applied to the workspace yet?)
                return;
            }

            if (visualStudioWorkspace.IsDocumentOpen(id))
            {
                var container = document.GetTextSynchronously(CancellationToken.None).Container;
                var textBuffer = container.TryGetTextBuffer();
                var undoHistory = _undoHistoryRegistry.RegisterHistory(textBuffer);

                using var undoTransaction = undoHistory.CreateTransaction(_description);
                undoTransaction.AddUndo(new NoOpUndoPrimitive());
                undoTransaction.Complete();
            }
            else
            {
                // open and close the document so that it is included in the global undo transaction
                using (visualStudioWorkspace.OpenInvisibleEditor(id))
                {
                    // empty
                }
            }
        }

        public void Commit()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // once either commit or disposed is called, don't do finalizer check
            GC.SuppressFinalize(this);

            if (_transactionAlive)
            {
                _service.ActiveTransactions--;

                var result = _undoManager.CloseLinkedUndo();
                if (result == VSConstants.UNDO_E_CLIENTABORT)
                {
                    Dispose();
                }
                else
                {
                    Marshal.ThrowExceptionForHR(result);
                    _transactionAlive = false;
                }
            }
        }

        public void Dispose()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            // once either commit or disposed is called, don't do finalizer check
            GC.SuppressFinalize(this);

            if (_transactionAlive)
            {
                _service.ActiveTransactions--;

                Marshal.ThrowExceptionForHR(_undoManager.AbortLinkedUndo());
                _transactionAlive = false;
            }
        }

#if DEBUG
        ~WorkspaceUndoTransaction()
        {
            // make sure we closed it correctly
            Debug.Assert(!_transactionAlive);
        }
#endif
    }
}
