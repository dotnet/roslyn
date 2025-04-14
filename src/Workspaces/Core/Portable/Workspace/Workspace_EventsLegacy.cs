// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Roslyn.Utilities;
using static Roslyn.Utilities.EventMap;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    // Allows conversion of legacy event handlers to the new event system.
    private readonly ConcurrentDictionary<(object, string), IDisposable> _disposableEventHandlers = new();

    /// <summary>
    /// An event raised whenever the current solution is changed.
    /// </summary>
    public event EventHandler<WorkspaceChangeEventArgs> WorkspaceChanged
    {
        add => AddEventHandler(value, WorkspaceChangeEventName);
        remove => RemoveEventHandler(value, WorkspaceChangeEventName);
    }

    /// <summary>
    /// An event raised whenever the workspace or part of its solution model
    /// fails to access a file or other external resource.
    /// </summary>
    public event EventHandler<WorkspaceDiagnosticEventArgs> WorkspaceFailed
    {
        add => AddEventHandler(value, WorkspaceFailedEventName);
        remove => RemoveEventHandler(value, WorkspaceFailedEventName);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentOpened
    {
        add => AddEventHandler(value, DocumentOpenedEventName);
        remove => RemoveEventHandler(value, DocumentOpenedEventName);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentOpened
    {
        add => AddEventHandler(value, TextDocumentOpenedEventName);
        remove => RemoveEventHandler(value, TextDocumentOpenedEventName);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentClosed
    {
        add => AddEventHandler(value, DocumentClosedEventName);
        remove => RemoveEventHandler(value, DocumentClosedEventName);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentClosed
    {
        add => AddEventHandler(value, TextDocumentClosedEventName);
        remove => RemoveEventHandler(value, TextDocumentClosedEventName);
    }

    /// <summary>
    /// An event that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    public event EventHandler<DocumentActiveContextChangedEventArgs> DocumentActiveContextChanged
    {
        add => AddEventHandler(value, DocumentActiveContextChangedName);
        remove => RemoveEventHandler(value, DocumentActiveContextChangedName);
    }

    private void AddEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, string eventName)
        where TEventArgs : EventArgs
    {
        Action<EventArgs> handler = arg => eventHandler(sender: this, (TEventArgs)arg);
        var disposer = RegisterHandler(eventName, handler, WorkspaceEventOptions.MainThreadDependent);

        _disposableEventHandlers[(eventHandler, eventName)] = disposer;
    }

    private void RemoveEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, string eventName)
        where TEventArgs : EventArgs
    {
        _disposableEventHandlers.TryRemove((eventHandler, eventName), out var disposer);

        disposer?.Dispose();
    }
}
