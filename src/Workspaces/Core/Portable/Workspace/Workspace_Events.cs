// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.WorkspaceEventMap;

namespace Microsoft.CodeAnalysis;

public abstract partial class Workspace
{
    private readonly WorkspaceEventMap _eventMap = new();

    internal enum WorkspaceEventType
    {
        DocumentActiveContextChanged,
        DocumentClosed,
        DocumentOpened,
        TextDocumentClosed,
        TextDocumentOpened,
        WorkspaceChange,
        WorkspaceChangedImmediate,
        WorkspaceFailed,
    }

    private IWorkspaceEventListenerService? _workspaceEventListenerService;

    #region Event Registration

    /// <summary>
    /// Registers a handler that is fired whenever the current solution is changed.
    /// </summary>
    public WorkspaceEventRegistration RegisterWorkspaceChangedHandler(Action<WorkspaceChangeEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.WorkspaceChange, handler, options);

    /// <summary>
    /// Registers a handler that is fired *immediately* whenever the current solution is changed.
    /// Handlers should be written to be very fast. Always called from the thread changing the workspace,
    /// regardless of the preferences indicated by the passed in options. This thread my vary depending
    /// on the workspace.
    /// </summary>
    public WorkspaceEventRegistration RegisterWorkspaceChangedImmediateHandler(Action<WorkspaceChangeEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.WorkspaceChangedImmediate, handler, options);

    /// <summary>
    /// Registers a handler that is fired whenever the workspace or part of its solution model
    /// fails to access a file or other external resource.
    /// </summary>
    public WorkspaceEventRegistration RegisterWorkspaceFailedHandler(Action<WorkspaceDiagnosticEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.WorkspaceFailed, handler, options);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is opened in the editor.
    /// </summary>
    public WorkspaceEventRegistration RegisterDocumentOpenedHandler(Action<DocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.DocumentOpened, handler, options);

    /// <summary>
    /// Registers a handler that is fired when a <see cref="Document"/> is closed in the editor.
    /// </summary>
    public WorkspaceEventRegistration RegisterDocumentClosedHandler(Action<DocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.DocumentClosed, handler, options);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is opened in the editor.
    /// </summary>
    public WorkspaceEventRegistration RegisterTextDocumentOpenedHandler(Action<TextDocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.TextDocumentOpened, handler, options);

    /// <summary>
    /// Registers a handler that is fired when any <see cref="TextDocument"/> is closed in the editor.
    /// </summary>
    public WorkspaceEventRegistration RegisterTextDocumentClosedHandler(Action<TextDocumentEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.TextDocumentClosed, handler, options);

    /// <summary>
    /// Registers a handler that is fired when the active context document associated with a buffer 
    /// changes.
    /// </summary>
    public WorkspaceEventRegistration RegisterDocumentActiveContextChangedHandler(Action<DocumentActiveContextChangedEventArgs> handler, WorkspaceEventOptions? options = null)
        => RegisterHandler(WorkspaceEventType.DocumentActiveContextChanged, handler, options);

    private WorkspaceEventRegistration RegisterHandler<TEventArgs>(WorkspaceEventType eventType, Action<TEventArgs> handler, WorkspaceEventOptions? options = null)
        where TEventArgs : EventArgs
    {
        var handlerAndOptions = new WorkspaceEventHandlerAndOptions(args => handler((TEventArgs)args), options ?? WorkspaceEventOptions.DefaultOptions);

        return _eventMap.AddEventHandler(eventType, handlerAndOptions);
    }

    #endregion

    protected Task RaiseWorkspaceChangedEventAsync(WorkspaceChangeKind kind, Solution oldSolution, Solution newSolution, ProjectId? projectId = null, DocumentId? documentId = null)
    {
        if (newSolution == null)
            throw new ArgumentNullException(nameof(newSolution));

        if (oldSolution == newSolution)
            return Task.CompletedTask;

        if (projectId == null && documentId != null)
            projectId = documentId.ProjectId;

        WorkspaceChangeEventArgs? args = null;

        var immediateHandlerSet = GetEventHandlers(WorkspaceEventType.WorkspaceChangedImmediate);
        if (immediateHandlerSet.HasHandlers)
        {
            args = new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            immediateHandlerSet.RaiseEvent(args, shouldRaiseEvent: static option => true);
        }

        var handlerSet = GetEventHandlers(WorkspaceEventType.WorkspaceChange);
        if (handlerSet.HasHandlers)
        {
            args ??= new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
            return this.ScheduleTask(args, handlerSet);
        }

        return Task.CompletedTask;
    }

    protected internal virtual void OnWorkspaceFailed(WorkspaceDiagnostic diagnostic)
    {
        var handlerSet = GetEventHandlers(WorkspaceEventType.WorkspaceFailed);
        if (handlerSet.HasHandlers)
        {
            var args = new WorkspaceDiagnosticEventArgs(diagnostic);
            handlerSet.RaiseEvent(args, shouldRaiseEvent: static option => true);
        }
    }

    protected Task RaiseDocumentOpenedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), WorkspaceEventType.DocumentOpened);

    protected Task RaiseTextDocumentOpenedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), WorkspaceEventType.TextDocumentOpened);

    private async Task RaiseTextDocumentOpenedOrClosedEventAsync<TDocument, TDocumentEventArgs>(
        TDocument document,
        TDocumentEventArgs args,
        WorkspaceEventType eventType)
        where TDocument : TextDocument
        where TDocumentEventArgs : EventArgs
    {
        var handlerSet = GetEventHandlers(eventType);
        if (handlerSet.HasHandlers && document != null)
            await ScheduleTask(args, handlerSet).ConfigureAwait(false);
    }

    protected Task RaiseDocumentClosedEventAsync(Document document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new DocumentEventArgs(document), WorkspaceEventType.DocumentClosed);

    protected Task RaiseTextDocumentClosedEventAsync(TextDocument document)
        => RaiseTextDocumentOpenedOrClosedEventAsync(document, new TextDocumentEventArgs(document), WorkspaceEventType.TextDocumentClosed);

    [Obsolete("This member is obsolete. Use the RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer, DocumentId, DocumentId) overload instead.", error: true)]
    protected Task RaiseDocumentActiveContextChangedEventAsync(Document document)
        => throw new NotImplementedException();

    protected async Task RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer sourceTextContainer, DocumentId oldActiveContextDocumentId, DocumentId newActiveContextDocumentId)
    {
        if (sourceTextContainer == null || oldActiveContextDocumentId == null || newActiveContextDocumentId == null)
            return;

        var handlerSet = GetEventHandlers(WorkspaceEventType.DocumentActiveContextChanged);
        if (handlerSet.HasHandlers)
        {
            // Capture the current solution snapshot (inside the _serializationLock of OnDocumentContextUpdated)
            var currentSolution = this.CurrentSolution;
            var args = new DocumentActiveContextChangedEventArgs(currentSolution, sourceTextContainer, oldActiveContextDocumentId, newActiveContextDocumentId);

            await this.ScheduleTask(args, handlerSet).ConfigureAwait(false);
        }
    }

    private EventHandlerSet GetEventHandlers(WorkspaceEventType eventType)
    {
        // this will register features that want to listen to workspace events
        // lazily first time workspace event is actually fired
        EnsureEventListeners();
        return _eventMap.GetEventHandlerSet(eventType);
    }

    private protected void EnsureEventListeners()
    {
        // Cache this service so it doesn't need to be retrieved from MEF during disposal.
        _workspaceEventListenerService ??= this.Services.GetService<IWorkspaceEventListenerService>();

        _workspaceEventListenerService?.EnsureListeners();
    }
}
