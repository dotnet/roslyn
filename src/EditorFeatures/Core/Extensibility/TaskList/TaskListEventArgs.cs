// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class TaskListEventArgs : EventArgs
    {
        /// <summary>
        /// The identity of task item group. 
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// task item type
        /// </summary>
        public string TaskListType { get; }

        /// <summary>
        /// Workspace this task items are associated with
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        /// projectId this task items are associated with
        /// </summary>
        public ProjectId ProjectId { get; }

        /// <summary>
        /// documentId this task items are associated with
        /// </summary>
        public DocumentId DocumentId { get; }

        /// <summary>
        /// The task items associated with the ID.
        /// </summary>
        public ImmutableArray<ITaskItem> TaskItems { get; }

        public TaskListEventArgs(
            object id, string type,
            Workspace workspace, ProjectId projectId, DocumentId documentId,
            ImmutableArray<ITaskItem> taskItems)
        {
            this.Id = id;
            this.TaskListType = type;
            this.Workspace = workspace;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
            this.TaskItems = taskItems;
        }
    }
}
