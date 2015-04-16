// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
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
                throw new ArgumentNullException("newSolution");
            }

            if (oldSolution == newSolution)
            {
                return SpecializedTasks.EmptyTask;
            }

            if (projectId == null && documentId != null)
            {
                projectId = documentId.ProjectId;
            }

            if (_eventMap.HasEventHandlers<EventHandler<WorkspaceChangeEventArgs>>(WorkspaceChangeEventName))
            {
                return this.ScheduleTask(() =>
                {
                    var args = new WorkspaceChangeEventArgs(kind, oldSolution, newSolution, projectId, documentId);
                    _eventMap.RaiseEvent<EventHandler<WorkspaceChangeEventArgs>>(WorkspaceChangeEventName, handler => handler(this, args));
                }, "Workspace.WorkspaceChanged");
            }
            else
            {
                return SpecializedTasks.EmptyTask;
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
            if (_eventMap.HasEventHandlers<EventHandler<WorkspaceDiagnosticEventArgs>>(WorkspaceFailedEventName))
            {
                var args = new WorkspaceDiagnosticEventArgs(diagnostic);
                _eventMap.RaiseEvent<EventHandler<WorkspaceDiagnosticEventArgs>>(WorkspaceFailedEventName, handler => handler(this, args));
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
            if (_eventMap.HasEventHandlers<EventHandler<DocumentEventArgs>>(DocumentOpenedEventName))
            {
                return this.ScheduleTask(() =>
                {
                    var args = new DocumentEventArgs(document);
                    _eventMap.RaiseEvent<EventHandler<DocumentEventArgs>>(DocumentOpenedEventName, handler => handler(this, args));
                }, "Workspace.WorkspaceChanged");
            }
            else
            {
                return SpecializedTasks.EmptyTask;
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
            if (_eventMap.HasEventHandlers<EventHandler<DocumentEventArgs>>(DocumentClosedEventName))
            {
                return this.ScheduleTask(() =>
                {
                    var args = new DocumentEventArgs(document);
                    _eventMap.RaiseEvent<EventHandler<DocumentEventArgs>>(DocumentClosedEventName, handler => handler(this, args));
                }, "Workspace.DocumentClosed");
            }
            else
            {
                return SpecializedTasks.EmptyTask;
            }
        }

        /// <summary>
        /// An event that is fired when the active context document associated with a buffer 
        /// changes.
        /// </summary>
        internal event EventHandler<DocumentEventArgs> DocumentActiveContextChanged
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

        protected Task RaiseDocumentActiveContextChangedEventAsync(Document document)
        {
            if (_eventMap.HasEventHandlers<EventHandler<DocumentEventArgs>>(DocumentActiveContextChangedName))
            {
                return this.ScheduleTask(() =>
                {
                    var args = new DocumentEventArgs(document);
                    _eventMap.RaiseEvent<EventHandler<DocumentEventArgs>>(DocumentActiveContextChangedName, handler => handler(this, args));
                }, "Workspace.WorkspaceChanged");
            }
            else
            {
                return SpecializedTasks.EmptyTask;
            }
        }
    }
}
