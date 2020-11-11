// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    /// <summary>
    /// A service that provide a way to undo operations applied to the workspace
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IGlobalUndoService), ServiceLayer.Host), Shared]
    internal partial class GlobalUndoServiceFactory : IWorkspaceServiceFactory
    {
        private readonly GlobalUndoService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public GlobalUndoServiceFactory(
            IThreadingContext threadingContext,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            SVsServiceProvider serviceProvider,
            Lazy<VisualStudioWorkspace> workspace)
        {
            _singleton = new GlobalUndoService(threadingContext, undoHistoryRegistry, serviceProvider, workspace);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;

        private class GlobalUndoService : IGlobalUndoService
        {
            private readonly IThreadingContext _threadingContext;
            private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
            private readonly IVsLinkedUndoTransactionManager _undoManager;
            private readonly Lazy<VisualStudioWorkspace> _lazyVSWorkspace;
            internal int ActiveTransactions;

            public GlobalUndoService(IThreadingContext threadingContext, ITextUndoHistoryRegistry undoHistoryRegistry, SVsServiceProvider serviceProvider, Lazy<VisualStudioWorkspace> lazyVSWorkspace)
            {
                _threadingContext = threadingContext;
                _undoHistoryRegistry = undoHistoryRegistry;
                _undoManager = (IVsLinkedUndoTransactionManager)serviceProvider.GetService(typeof(SVsLinkedUndoTransactionManager));
                _lazyVSWorkspace = lazyVSWorkspace;
            }

            public bool CanUndo(Workspace workspace)
            {
                // only primary workspace supports global undo
                return _lazyVSWorkspace.Value == workspace;
            }

            public IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction(Workspace workspace, string description)
            {
                if (!CanUndo(workspace))
                {
                    throw new ArgumentException(ServicesVSResources.given_workspace_doesn_t_support_undo);
                }

                var transaction = new WorkspaceUndoTransaction(_threadingContext, _undoHistoryRegistry, _undoManager, workspace, description, this);
                ActiveTransactions++;
                return transaction;
            }

            public bool IsGlobalTransactionOpen(Workspace workspace)
                => ActiveTransactions > 0;
        }
    }
}
