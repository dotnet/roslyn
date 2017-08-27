﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Undo;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    /// <summary>
    /// A service that provide a way to undo operations applied to the workspace
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IGlobalUndoService), ServiceLayer.Host), Shared]
    internal partial class GlobalUndoServiceFactory : IWorkspaceServiceFactory
    {
        private readonly GlobalUndoService _singleton;

        [ImportingConstructor]
        public GlobalUndoServiceFactory(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            SVsServiceProvider serviceProvider,
            Lazy<VisualStudioWorkspace> workspace)
        {
            _singleton = new GlobalUndoService(undoHistoryRegistry, serviceProvider, workspace);
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }

        private class GlobalUndoService : IGlobalUndoService
        {
            private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
            private readonly IVsLinkedUndoTransactionManager _undoManager;
            private readonly Lazy<VisualStudioWorkspace> _lazyVSWorkspace;
            internal int ActiveTransactions;

            public GlobalUndoService(ITextUndoHistoryRegistry undoHistoryRegistry, SVsServiceProvider serviceProvider, Lazy<VisualStudioWorkspace> lazyVSWorkspace)
            {
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

                var transaction = new WorkspaceUndoTransaction(_undoHistoryRegistry, _undoManager, workspace, description, this);
                ActiveTransactions++;
                return transaction;
            }

            public bool IsGlobalTransactionOpen(Workspace workspace)
            {
                return ActiveTransactions > 0;
            }
        }
    }
}
