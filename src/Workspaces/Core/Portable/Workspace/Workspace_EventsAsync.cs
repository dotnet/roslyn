// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    private readonly AsyncEventMap _asyncEventMap = new();

    /// <summary>
    /// Registers a handler that is fired whenever the current solution is changed.
    /// </summary>
    internal WorkspaceEventRegistration RegisterWorkspaceChangedHandler(Func<WorkspaceChangeEventArgs, Task> handler)
        => RegisterAsyncHandler(WorkspaceChangeEventName, handler);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentOpenedHandler(Func<DocumentEventArgs, Task> handler)
        => RegisterAsyncHandler(DocumentOpenedEventName, handler);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentClosedHandler(Func<DocumentEventArgs, Task> handler)
        => RegisterAsyncHandler(DocumentClosedEventName, handler);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentOpenedHandler(Func<TextDocumentEventArgs, Task> handler)
        => RegisterAsyncHandler(TextDocumentOpenedEventName, handler);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentClosedHandler(Func<TextDocumentEventArgs, Task> handler)
        => RegisterAsyncHandler(TextDocumentClosedEventName, handler);

    /// <summary>
    /// Registers a handler that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentActiveContextChangedHandler(Func<DocumentActiveContextChangedEventArgs, Task> handler)
        => RegisterAsyncHandler(DocumentActiveContextChangedName, handler);

    private WorkspaceEventRegistration RegisterAsyncHandler<TEventArgs>(string eventName, Func<TEventArgs, Task> handler)
        where TEventArgs : EventArgs
    {
        _asyncEventMap.AddAsyncEventHandler(eventName, handler);

        return WorkspaceEventRegistration.Create(_asyncEventMap, eventName, handler);
    }
}
