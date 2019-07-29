// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public abstract partial class Workspace
    {
        private readonly EventMap _eventMap = new EventMap();

        private const string WorkspaceChangeEventName = "WorkspaceChanged";
        private const string WorkspaceFailedEventName = "WorkspaceFailed";
        private const string DocumentOpenedEventName = "DocumentOpened";
        private const string DocumentClosedEventName = "DocumentClosed";
        private const string DocumentActiveContextChangedName = "DocumentActiveContextChanged";

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

            var ev = GetEventHandlers<WorkspaceChangeEventArgs>(WorkspaceChangeEventName);
            if (ev.HasHandlers)
            {
                return this.ScheduleTask(() =>
                {
                    using (Logger.LogBlock(FunctionId.Workspace_Events, (s, p, d, k) => $"{s.Id} - {p} - {d} {kind.ToString()}", newSolution, projectId, documentId, kind, CancellationToken.None))
                    {
                        var args = new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
                        ev.RaiseEvent(handler => handler(this, args));
                    }
                }, WorkspaceChangeEventName);
            }
            else
            {
                return Task.CompletedTask;
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
                ev.RaiseEvent(handler => handler(this, args));
            }
        }

        /// <summary>
        /// An event that is fired when a documents is opened in the editor.
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
        {
            var ev = GetEventHandlers<DocumentEventArgs>(DocumentOpenedEventName);
            if (ev.HasHandlers && document != null)
            {
                return this.ScheduleTask(() =>
                {
                    var args = new DocumentEventArgs(document);
                    ev.RaiseEvent(handler => handler(this, args));
                }, DocumentOpenedEventName);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// An event that is fired when a document is closed in the editor.
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
        {
            var ev = GetEventHandlers<DocumentEventArgs>(DocumentClosedEventName);
            if (ev.HasHandlers && document != null)
            {
                return this.ScheduleTask(() =>
                {
                    var args = new DocumentEventArgs(document);
                    ev.RaiseEvent(handler => handler(this, args));
                }, DocumentClosedEventName);
            }
            else
            {
                return Task.CompletedTask;
            }
        }

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
        {
            throw new NotImplementedException();
        }

        protected Task RaiseDocumentActiveContextChangedEventAsync(SourceTextContainer sourceTextContainer, DocumentId oldActiveContextDocumentId, DocumentId newActiveContextDocumentId)
        {
            var ev = GetEventHandlers<DocumentActiveContextChangedEventArgs>(DocumentActiveContextChangedName);
            if (ev.HasHandlers && sourceTextContainer != null && oldActiveContextDocumentId != null && newActiveContextDocumentId != null)
            {
                // Capture the current solution snapshot (inside the _serializationLock of OnDocumentContextUpdated)
                var currentSolution = this.CurrentSolution;

                return this.ScheduleTask(() =>
                {
                    var args = new DocumentActiveContextChangedEventArgs(currentSolution, sourceTextContainer, oldActiveContextDocumentId, newActiveContextDocumentId);
                    ev.RaiseEvent(handler => handler(this, args));
                }, "Workspace.WorkspaceChanged");
            }
            else
            {
                return Task.CompletedTask;
            }
        }

        private EventMap.EventHandlerSet<EventHandler<T>> GetEventHandlers<T>(string eventName) where T : EventArgs
        {
            // this will register features that want to listen to workspace events
            // lazily first time workspace event is actually fired
            this.Services.GetService<IWorkspaceEventListenerService>()?.EnsureListeners();
            return _eventMap.GetEventHandlers<EventHandler<T>>(eventName);
        }
    }
}
