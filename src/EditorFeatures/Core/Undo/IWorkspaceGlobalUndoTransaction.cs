// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Undo;

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
