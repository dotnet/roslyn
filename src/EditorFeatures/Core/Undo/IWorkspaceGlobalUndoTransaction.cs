// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Undo
{
    /// <summary>
    /// This represents workspace global undo transaction
    /// </summary>
    internal interface IWorkspaceGlobalUndoTransaction : IDisposable
    {
        /// <summary>
        /// explicitly add a document to the global undo transaction
        /// </summary>
        void AddDocument(DocumentId id);

        /// <summary>
        /// finish the undo transaction
        /// </summary>
        void Commit();
    }
}
