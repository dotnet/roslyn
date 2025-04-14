// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    private readonly EventMap _eventMap = new();

    private const string WorkspaceChangeEventName = "WorkspaceChanged";
    private const string WorkspaceChangedImmediateEventName = "WorkspaceChangedImmediate";
    private const string WorkspaceFailedEventName = "WorkspaceFailed";
    private const string DocumentOpenedEventName = "DocumentOpened";
    private const string DocumentClosedEventName = "DocumentClosed";
    private const string DocumentActiveContextChangedName = "DocumentActiveContextChanged";
    private const string TextDocumentOpenedEventName = "TextDocumentOpened";
    private const string TextDocumentClosedEventName = "TextDocumentClosed";

    private IWorkspaceEventListenerService _workspaceEventListenerService;

    /// <summary>
    /// An event raised whenever the current solution is changed.
    /// </summary>
    public event EventHandler<WorkspaceChangeEventArgs> WorkspaceChanged
    {
        add
        {
            _eventMap.AddEventHandler(WorkspaceChangeEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(WorkspaceChangeEventName, value);
        }
    }

    /// <summary>
    /// An event raised *immediately* whenever the current solution is changed. Handlers
    /// should be written to be very fast. Called on the same thread changing the workspace,
    /// which may vary depending on the workspace.
    /// </summary>
    internal event EventHandler<WorkspaceChangeEventArgs> WorkspaceChangedImmediate
    {
        add
        {
            _eventMap.AddEventHandler(WorkspaceChangedImmediateEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(WorkspaceChangedImmediateEventName, value);
        }
    }

    protected Task RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId projectId = null, DocumentId documentId = null)
    {
        if (newSolution == null)
        {
            throw new ArgumentNullException(nameof(newSolution));
        }

        if (oldSolution == newSolution)
        {
            return Task.CompletedTask;
        }

        if (projectId == null && documentId != null)
        {
            projectId = documentId.ProjectId;
        }

        WorkspaceChangeEventArgs args = null;
        var ev = GetEventHandlers<WorkspaceChangeEventArgs>(WorkspaceChangedImmediateEventName);

        if (ev.HasHandlers)
        {
            args = new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            RaiseEventForHandlers(ev, sender: this, args, FunctionId.Workspace_EventsImmediate);
        }

        var eventHandlerTasks = new List<Task>();

        ev = GetEventHandlers<WorkspaceChangeEventArgs>(WorkspaceChangeEventName);
        if (ev.HasHandlers)
        {
            args ??= new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            var syncEventHandlersTask = this.ScheduleTask(() =>
            {
                RaiseEventForHandlers(ev, sender: this, args, FunctionId.Workspace_Events);
            }, WorkspaceChangeEventName);

            eventHandlerTasks.Add(syncEventHandlersTask);
        }

        var asyncEv = _asyncEventMap.GetEventHandlerSet<WorkspaceChangeEventArgs>(WorkspaceChangeEventName);
        if (asyncEv.HasHandlers)
        {
            args ??= new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            var asyncEventHandlersTask = this.ScheduleTask(() => RaiseEventForAsyncHandlersAsync(asyncEv, args, FunctionId.Workspace_Events));

            eventHandlerTasks.Add(asyncEventHandlersTask);
        }

        return Task.WhenAll(eventHandlerTasks);

        static void RaiseEventForHandlers(
            EventMap.EventHandlerSet<EventHandler<WorkspaceChangeEventArgs>> handlers,
            Workspace sender,
            WorkspaceChangeEventArgs args,
            FunctionId functionId)
        {
            using (Logger.LogBlock(functionId, (s, p, d, k) => $"{s.Id} - {p} - {d} {args.Kind.ToString()}", args.NewSolution, args.ProjectId, args.DocumentId, args.Kind, CancellationToken.None))
            {
                handlers.RaiseEvent(static (handler, arg) => handler(arg.sender, arg.args), (sender, args));
            }
        }

        static async Task RaiseEventForAsyncHandlersAsync(
            AsyncEventMap.AsyncEventHandlerSet<WorkspaceChangeEventArgs> asyncHandlers,
            WorkspaceChangeEventArgs args,
            FunctionId functionId)
        {
            using (Logger.LogBlock(functionId, (s, p, d, k) => $"{s.Id} - {p} - {d} {args.Kind.ToString()}", args.NewSolution, args.ProjectId, args.DocumentId, args.Kind, CancellationToken.None))
            {
                await asyncHandlers.RaiseEventAsync(args).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// An event raised whenever the workspace or part of its solution model
    /// fails to access a file or other external resource.
    /// </summary>
    public event EventHandler<WorkspaceDiagnosticEventArgs> WorkspaceFailed
    {
        add
        {
            _eventMap.AddEventHandler(WorkspaceFailedEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(WorkspaceFailedEventName, value);
        }
    }

    protected internal virtual void OnWorkspaceFailed(WorkspaceDiagnostic diagnostic)
    {
        var ev = GetEventHandlers<WorkspaceDiagnosticEventArgs>(WorkspaceFailedEventName);
        if (ev.HasHandlers)
        {
            var args = new WorkspaceDiagnosticEventArgs(diagnostic);
            ev.RaiseEvent(static (handler, arg) => handler(arg.self, arg.args), (self: this, args));
        }
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentOpened
    {
        add
        {
            _eventMap.AddEventHandler(DocumentOpenedEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(DocumentOpenedEventName, value);
        }
    }

    protected Task RaiseDocumentOpenedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), DocumentOpenedEventName);

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentOpened
    {
        add
        {
            _eventMap.AddEventHandler(TextDocumentOpenedEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(TextDocumentOpenedEventName, value);
        }
    }

    protected Task RaiseTextDocumentOpenedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), TextDocumentOpenedEventName);

    private Task RaiseTextDocumentOpenedOrClosedEventAsync<TDocument, TDocumentEventArgs>(
        TDocument document,
        TDocumentEventArgs args,
        string eventName)
        where TDocument : TextDocument
        where TDocumentEventArgs : EventArgs
    {
        var eventHandlerTasks = new List<Task>();
        var ev = GetEventHandlers<TDocumentEventArgs>(eventName);

        if (ev.HasHandlers && document != null)
        {
            var syncEventHandlerTask = this.ScheduleTask(() =>
            {
                ev.RaiseEvent(static (handler, arg) => handler(arg.self, arg.args), (self: this, args));
            }, eventName);

            eventHandlerTasks.Add(syncEventHandlerTask);
        }

        var asyncEv = _asyncEventMap.GetEventHandlerSet<TDocumentEventArgs>(eventName);
        if (asyncEv.HasHandlers)
        {
            var asyncEventHandlersTask = this.ScheduleBackgroundTask(() => asyncEv.RaiseEventAsync(args));

            eventHandlerTasks.Add(asyncEventHandlersTask);
        }

        return Task.WhenAll(eventHandlerTasks);
    }

    /// <summary>
    /// An event that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    public event EventHandler<DocumentEventArgs> DocumentClosed
    {
        add
        {
            _eventMap.AddEventHandler(DocumentClosedEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(DocumentClosedEventName, value);
        }
    }

    protected Task RaiseDocumentClosedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), DocumentClosedEventName);

    /// <summary>
    /// An event that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    public event EventHandler<TextDocumentEventArgs> TextDocumentClosed
    {
        add
        {
            _eventMap.AddEventHandler(TextDocumentClosedEventName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(TextDocumentClosedEventName, value);
        }
    }

    protected Task RaiseTextDocumentClosedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), TextDocumentClosedEventName);

    /// <summary>
    /// An event that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    public event EventHandler<DocumentActiveContextChangedEventArgs> DocumentActiveContextChanged
    {
        add
        {
            _eventMap.AddEventHandler(DocumentActiveContextChangedName, value);
        }

        remove
        {
            _eventMap.RemoveEventHandler(DocumentActiveContextChangedName, value);
        }
    }

    [Obsolete("This member is obsolete. Use the RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer, DocumentId, DocumentId) overload instead.", error: true)]
    protected Task RaiseDocumentActiveContextChangedEventAsync(Document document)
        => throw new NotImplementedException();

    protected Task RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer sourceTextContainer, DocumentId oldActiveContextDocumentId, DocumentId newActiveContextDocumentId)
    {
        if (sourceTextContainer == null || oldActiveContextDocumentId == null || newActiveContextDocumentId == null)
            return Task.CompletedTask;

        var eventHandlerTasks = new List<Task>();
        var ev = GetEventHandlers<DocumentActiveContextChangedEventArgs>(DocumentActiveContextChangedName);
        DocumentActiveContextChangedEventArgs? args = null;

        if (ev.HasHandlers)
        {
            // Capture the current solution snapshot (inside the _serializationLock of OnDocumentContextUpdated)
            var currentSolution = this.CurrentSolution;
            args ??= new DocumentActiveContextChangedEventArgs(currentSolution, sourceTextContainer, oldActiveContextDocumentId, newActiveContextDocumentId);

            var syncEventHandlerTask = this.ScheduleTask(() =>
            {
                ev.RaiseEvent(static (handler, arg) => handler(arg.self, arg.args), (self: this, args));
            }, "Workspace.WorkspaceChanged");

            eventHandlerTasks.Add(syncEventHandlerTask);
        }

        var asyncEv = _asyncEventMap.GetEventHandlerSet<DocumentActiveContextChangedEventArgs>(DocumentActiveContextChangedName);
        if (asyncEv.HasHandlers)
        {
            var asyncEventHandlersTask = this.ScheduleBackgroundTask(() => asyncEv.RaiseEventAsync(args));

            eventHandlerTasks.Add(asyncEventHandlersTask);
        }

        return Task.WhenAll(eventHandlerTasks);
    }

    private EventMap.EventHandlerSet<EventHandler<T>> GetEventHandlers<T>(string eventName) where T : EventArgs
    {
        // this will register features that want to listen to workspace events
        // lazily first time workspace event is actually fired
        EnsureEventListeners();
        return _eventMap.GetEventHandlers<EventHandler<T>>(eventName);
    }

    private void EnsureEventListeners()
    {
        // Cache this service so it doesn't need to be retrieved from MEF during disposal.
        _workspaceEventListenerService ??= this.Services.GetService<IWorkspaceEventListenerService>();

        _workspaceEventListenerService?.EnsureListeners();
    }
}
