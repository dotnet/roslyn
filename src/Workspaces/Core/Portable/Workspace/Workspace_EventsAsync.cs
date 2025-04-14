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
    internal WorkspaceEventRegistration RegisterWorkspaceChangedHandler(Action<WorkspaceChangeEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(WorkspaceChangeEventName, handler, requiresMainThread);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentOpenedHandler(Action<DocumentEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(DocumentOpenedEventName, handler, requiresMainThread);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentClosedHandler(Action<DocumentEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(DocumentClosedEventName, handler, requiresMainThread);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentOpenedHandler(Action<TextDocumentEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(TextDocumentOpenedEventName, handler, requiresMainThread);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentClosedHandler(Action<TextDocumentEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(TextDocumentClosedEventName, handler, requiresMainThread);

    /// <summary>
    /// Registers a handler that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentActiveContextChangedHandler(Action<DocumentActiveContextChangedEventArgs> handler, bool requiresMainThread)
        => RegisterAsyncHandler(DocumentActiveContextChangedName, handler, requiresMainThread);

    private WorkspaceEventRegistration RegisterAsyncHandler<TEventArgs>(string eventName, Action<TEventArgs> handler, bool requiresMainThread)
        where TEventArgs : EventArgs
    {
        _asyncEventMap.AddHandler(eventName, handler, requiresMainThread);

        return WorkspaceEventRegistration.Create(_asyncEventMap, eventName, handler, requiresMainThread);
    }
}
