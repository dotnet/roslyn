// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class TodoItemsUpdatedArgs : UpdatedEventArgs
    {
        /// <summary>
        /// Solution this task items are associated with
        /// </summary>
        public Solution Solution { get; }

        /// <summary>
        /// The task items associated with the ID.
        /// </summary>
        public ImmutableArray<TodoItem> TodoItems { get; }

        public TodoItemsUpdatedArgs(object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<TodoItem> todoItems)
            : base(id, workspace, projectId, documentId, buildTool: null)
        {
            Debug.Assert(solution != null);
            Debug.Assert(!todoItems.IsDefault);

            Solution = solution;
            TodoItems = todoItems;
        }
    }
}
