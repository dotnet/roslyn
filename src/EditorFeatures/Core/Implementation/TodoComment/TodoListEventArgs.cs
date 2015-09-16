// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class TodoListEventArgs : EventArgs
    {
        /// <summary>
        /// The identity of task item group. 
        /// </summary>
        public object Id { get; }

        /// <summary>
        /// Workspace this task items are associated with
        /// </summary>
        public Workspace Workspace { get; }

        /// <summary>
        /// Solution this task items are associated with
        /// </summary>
        public Solution Solution { get; }

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
        public ImmutableArray<TodoItem> TodoItems { get; }

        public TodoListEventArgs(
            object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<TodoItem> todoItems)
        {
            this.Id = id;
            this.Workspace = workspace;
            this.Solution = solution;
            this.ProjectId = projectId;
            this.DocumentId = documentId;
            this.TodoItems = todoItems;
        }
    }
}
