// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    /// <summary>
    /// This factory will create a service that provides workspace global undo service.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IGlobalUndoService), ServiceLayer.Default), Shared]
    internal class NoOpGlobalUndoServiceFactory : IWorkspaceServiceFactory
    {
        public static readonly IWorkspaceGlobalUndoTransaction Transaction = new NoOpUndoTransaction();

        private readonly NoOpGlobalUndoService _singleton = new NoOpGlobalUndoService();

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return _singleton;
        }

        private class NoOpGlobalUndoService : IGlobalUndoService
        {
            public bool IsGlobalTransactionOpen(Workspace workspace)
            {
                // TODO: this is technically wrong -- Transaction shouldn't be a singleton.
                return false;
            }

            public bool CanUndo(Workspace workspace)
            {
                // by default, undo is not supported
                return false;
            }

            public IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction(Workspace workspace, string description)
            {
                return Transaction;
            }
        }

        /// <summary>
        /// null object that doesn't do anything
        /// </summary>
        private class NoOpUndoTransaction : IWorkspaceGlobalUndoTransaction
        {
            public void Commit()
            {
            }

            public void Dispose()
            {
            }

            public void AddDocument(DocumentId id)
            {
            }
        }
    }
}
