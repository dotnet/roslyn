// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Roslyn.Utilities.EventMap;

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

    #region Event Registration

    /// <summary>
    /// Registers a handler that is fired whenever the current solution is changed.
    /// </summary>
    internal WorkspaceEventRegistration RegisterWorkspaceChangedHandler(Action<WorkspaceChangeEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceChangeEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired whenever the current solution is changed.
    /// </summary>
    internal WorkspaceEventRegistration RegisterWorkspaceChangedImmediateHandler(Action<WorkspaceChangeEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceChangedImmediateEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentOpenedHandler(Action<DocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(DocumentOpenedEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentClosedHandler(Action<DocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(DocumentClosedEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentOpenedHandler(Action<TextDocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(TextDocumentOpenedEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    internal WorkspaceEventRegistration RegisterTextDocumentClosedHandler(Action<TextDocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(TextDocumentClosedEventName, handler, options);

    /// <summary>
    /// Registers a handler that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    internal WorkspaceEventRegistration RegisterDocumentActiveContextChangedHandler(Action<DocumentActiveContextChangedEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(DocumentActiveContextChangedName, handler, options);

    private WorkspaceEventRegistration RegisterHandler<TEventArgs>(string eventName, Action<TEventArgs> handler, WorkspaceEventOptions? options = null)
        where TEventArgs : EventArgs
    {
        var handlerAndOptions = new WorkspaceEventHandlerAndOptions(args => handler((TEventArgs)args), options ?? WorkspaceEventOptions.Default);
        _eventMap.AddEventHandler(eventName, handlerAndOptions);

        return new WorkspaceEventRegistration(_eventMap, eventName, handlerAndOptions);
    }

    #endregion

    protected Task RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId projectId = null, DocumentId documentId = null)
    {
        if (newSolution == null)
            throw new ArgumentNullException(nameof(newSolution));

        if (oldSolution == newSolution)
            return Task.CompletedTask;

        if (projectId == null && documentId != null)
            projectId = documentId.ProjectId;

        WorkspaceChangeEventArgs args = null;

        var immediateHandlerSet = GetEventHandlers(WorkspaceChangedImmediateEventName);
        if (immediateHandlerSet.HasHandlers)
        {
            args = new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            immediateHandlerSet.RaiseEvent(args, shouldRaiseEvent: static option => true);
        }

        var handlerSet = GetEventHandlers(WorkspaceChangeEventName);
        if (handlerSet.HasHandlers)
        {
            args ??= new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            return this.ScheduleTask(args, handlerSet);
        }

        return Task.CompletedTask;
    }

    protected internal virtual void OnWorkspaceFailed(WorkspaceDiagnostic diagnostic)
    {
        var handlerSet = GetEventHandlers(WorkspaceFailedEventName);
        if (handlerSet.HasHandlers)
        {
            var args = new WorkspaceDiagnosticEventArgs(diagnostic);
            handlerSet.RaiseEvent(args, shouldRaiseEvent: static option => true);
        }
    }

    protected Task RaiseDocumentOpenedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), DocumentOpenedEventName);

    protected Task RaiseTextDocumentOpenedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), TextDocumentOpenedEventName);

    private Task RaiseTextDocumentOpenedOrClosedEventAsync<TDocument, TDocumentEventArgs>(
        TDocument document,
        TDocumentEventArgs args,
        string eventName)
        where TDocument : TextDocument
        where TDocumentEventArgs : EventArgs
    {
        var handlerSet = GetEventHandlers(eventName);
        if (handlerSet.HasHandlers && document != null)
            return this.ScheduleTask(args, handlerSet);

        return Task.CompletedTask;
    }

    protected Task RaiseDocumentClosedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), DocumentClosedEventName);

    protected Task RaiseTextDocumentClosedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), TextDocumentClosedEventName);

    [Obsolete("This member is obsolete. Use the RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer, DocumentId, DocumentId) overload instead.", error: true)]
    protected Task RaiseDocumentActiveContextChangedEventAsync(Document document)
        => throw new NotImplementedException();

    protected Task RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer sourceTextContainer, DocumentId oldActiveContextDocumentId, DocumentId newActiveContextDocumentId)
    {
        if (sourceTextContainer == null || oldActiveContextDocumentId == null || newActiveContextDocumentId == null)
            return Task.CompletedTask;

        var handlerSet = GetEventHandlers(DocumentActiveContextChangedName);
        if (handlerSet.HasHandlers)
        {
            // Capture the current solution snapshot (inside the _serializationLock of OnDocumentContextUpdated)
            var currentSolution = this.CurrentSolution;
            var args = new DocumentActiveContextChangedEventArgs(currentSolution, sourceTextContainer, oldActiveContextDocumentId, newActiveContextDocumentId);

            return this.ScheduleTask(args, handlerSet);
        }

        return Task.CompletedTask;
    }

    private EventMap.EventHandlerSet GetEventHandlers(string eventName)
    {
        // this will register features that want to listen to workspace events
        // lazily first time workspace event is actually fired
        EnsureEventListeners();
        return _eventMap.GetEventHandlerSet(eventName);
    }

    private void EnsureEventListeners()
    {
        // Cache this service so it doesn't need to be retrieved from MEF during disposal.
        _workspaceEventListenerService ??= this.Services.GetService<IWorkspaceEventListenerService>();

        _workspaceEventListenerService?.EnsureListeners();
    }
}
