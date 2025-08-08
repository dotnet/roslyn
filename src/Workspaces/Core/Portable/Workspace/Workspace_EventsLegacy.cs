// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    /// <summary>
    /// Allows conversion of legacy event handlers to the new event system. The first item in
    /// the key's tuple is an EventHandler&lt;TEventArgs&gt; and thus stored as an object.
    /// </summary>
    private readonly Dictionary<(object eventHandler, WorkspaceEventType eventType), (int adviseCount, IDisposable disposer)> _disposableEventHandlers = new();
    private readonly object _legacyWorkspaceEventsGate = new();

    /// <summary>
    /// An event raised whenever the current solution is changed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterWorkspaceChangedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<WorkspaceChangeEventArgs> WorkspaceChanged
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.WorkspaceChange);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.WorkspaceChange);
    }

    /// <summary>
    /// An event raised whenever the workspace or part of its solution model
    /// fails to access a file or other external resource.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterWorkspaceFailedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<WorkspaceDiagnosticEventArgs> WorkspaceFailed
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.WorkspaceFailed);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.WorkspaceFailed);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterDocumentOpenedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<DocumentEventArgs> DocumentOpened
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.DocumentOpened);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.DocumentOpened);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterTextDocumentOpenedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<TextDocumentEventArgs> TextDocumentOpened
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.TextDocumentOpened);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.TextDocumentOpened);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterDocumentClosedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<DocumentEventArgs> DocumentClosed
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.DocumentClosed);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.DocumentClosed);
    }

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterTextDocumentClosedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<TextDocumentEventArgs> TextDocumentClosed
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.TextDocumentClosed);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.TextDocumentClosed);
    }

    /// <summary>
    /// An event that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete($"Use {nameof(RegisterDocumentActiveContextChangedHandler)} instead, which by default will no longer run on the UI thread.", error: false)]
    public event EventHandler<DocumentActiveContextChangedEventArgs> DocumentActiveContextChanged
    {
        add => AddLegacyEventHandler(value, WorkspaceEventType.DocumentActiveContextChanged);
        remove => RemoveLegacyEventHandler(value, WorkspaceEventType.DocumentActiveContextChanged);
    }

    private void AddLegacyEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, WorkspaceEventType eventType)
        where TEventArgs : EventArgs
    {
        // Require main thread on the callback as this is used from publicly exposed events
        // and those callbacks may have main thread dependencies.
        var key = (eventHandler, eventType);

        lock (_legacyWorkspaceEventsGate)
        {
            if (_disposableEventHandlers.TryGetValue(key, out var adviseCountAndDisposer))
            {
                // If we already have a handler for this event type, update the map with the new advise count
                _disposableEventHandlers[key] = (adviseCountAndDisposer.adviseCount + 1, adviseCountAndDisposer.disposer);
            }
            else
            {
                // Safe to call RegisterHandler inside the lock as it doesn't invoke code outside the workspace event map code.
                var disposer = RegisterHandler(eventType, (Action<EventArgs>)Handler, WorkspaceEventOptions.RequiresMainThreadOptions);
                _disposableEventHandlers[key] = (adviseCount: 1, disposer);
            }
        }

        return;

        void Handler(EventArgs arg)
            => eventHandler(sender: this, (TEventArgs)arg);
    }

    private void RemoveLegacyEventHandler<TEventArgs>(EventHandler<TEventArgs> eventHandler, WorkspaceEventType eventType)
        where TEventArgs : EventArgs
    {
        IDisposable? disposer = null;

        lock (_legacyWorkspaceEventsGate)
        {
            if (_disposableEventHandlers.TryGetValue((eventHandler, eventType), out var adviseCountAndDisposer))
            {
                // If we already have a handler for this event type, just increment the advise count
                // and return.
                if (adviseCountAndDisposer.adviseCount == 1)
                {
                    disposer = adviseCountAndDisposer.disposer;
                    _disposableEventHandlers.Remove((eventHandler, eventType));
                }
                else
                {
                    _disposableEventHandlers[(eventHandler, eventType)] = (adviseCountAndDisposer.adviseCount - 1, adviseCountAndDisposer.disposer);
                }
            }
        }

        disposer?.Dispose();
    }
}
