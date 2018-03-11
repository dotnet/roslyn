// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    /// <summary>
    /// This provides a way to do global undo. but semantic of the global undo is defined by the workspace host.
    /// </summary>
    internal interface IGlobalUndoService : IWorkspaceService
    {
        /// <summary>
        /// Queries whether a global transaction is currently active.
        /// </summary>
        bool IsGlobalTransactionOpen(Workspace workspace);

        /// <summary>
        /// query method that can answer whether global undo is supported by the workspace
        /// </summary>
        bool CanUndo(Workspace workspace);

        /// <summary>
        /// open global undo transaction for the workspace
        /// </summary>
        IWorkspaceGlobalUndoTransaction OpenGlobalUndoTransaction(Workspace workspace, string description);
    }
}
