// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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

        private readonly NoOpGlobalUndoService _singleton = new();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public NoOpGlobalUndoServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => _singleton;

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
                => Transaction;
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
