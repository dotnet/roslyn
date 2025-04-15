// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    // Allows conversion of legacy event handlers to the new event system.
    private readonly ConcurrentDictionary<(object, WorkspaceEventType), IDisposable> _disposableEventHandlers = new();

    /// <summary>
    /// An event raised whenever the current solution is changed.
    /// </summary>
    public event EventHandler<WorkspaceChangeEventArgs> WorkspaceChanged
    {
        add => AddEventHandler(value, WorkspaceEventType.WorkspaceChange);
        remove => RemoveEventHandler(value, WorkspaceEventType.WorkspaceChange);
    }

    /// <summary>
    /// An event raised whenever the workspace or part of its solution model
    /// fails to access a file or other external resource.
    /// </summary>
    public event EventHandler<WorkspaceDiagnosticEventArgs> WorkspaceFailed
    {
        add => AddEventHandler(value, WorkspaceEventType.WorkspaceFailed);
        remove => RemoveEventHandler(value, WorkspaceEventType.WorkspaceFailed);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentOpened
    {
        add => AddEventHandler(value, WorkspaceEventType.DocumentOpened);
        remove => RemoveEventHandler(value, WorkspaceEventType.DocumentOpened);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentOpened
    {
        add => AddEventHandler(value, WorkspaceEventType.TextDocumentOpened);
        remove => RemoveEventHandler(value, WorkspaceEventType.TextDocumentOpened);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentClosed
    {
        add => AddEventHandler(value, WorkspaceEventType.DocumentClosed);
        remove => RemoveEventHandler(value, WorkspaceEventType.DocumentClosed);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentClosed
    {
        add => AddEventHandler(value, WorkspaceEventType.TextDocumentClosed);
        remove => RemoveEventHandler(value, WorkspaceEventType.TextDocumentClosed);
    }

    /// <summary>
    /// An event that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    public event EventHandler<DocumentActiveContextChangedEventArgs> DocumentActiveContextChanged
    {
        add => AddEventHandler(value, WorkspaceEventType.DocumentActiveContextChanged);
        remove => RemoveEventHandler(value, WorkspaceEventType.DocumentActiveContextChanged);
    }

    private void AddEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, WorkspaceEventType eventType)
        where TEventArgs : EventArgs
    {
        // Require main thread on the callback as this is used from publicly exposed eventss
        // and those callbacks may have main thread dependencies.
        var disposer = RegisterHandler(eventType, (Action<EventArgs>)handler, WorkspaceEventOptions.RequiresMainThreadOptions);

        _disposableEventHandlers[(eventHandler, eventType)] = disposer;

        void handler(EventArgs arg)
            => eventHandler(sender: this, (TEventArgs)arg);
    }

    private void RemoveEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, WorkspaceEventType eventType)
        where TEventArgs : EventArgs
    {
        _disposableEventHandlers.TryRemove((eventHandler, eventType), out var disposer);

        disposer?.Dispose();
    }
}
