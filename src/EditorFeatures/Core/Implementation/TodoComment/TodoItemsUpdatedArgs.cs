// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.TodoComments;

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
        public ImmutableArray<TodoCommentData> TodoItems { get; }

        public TodoItemsUpdatedArgs(
            object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<TodoCommentData> todoItems)
            : base(id, workspace, projectId, documentId)
        {
            Solution = solution;
            TodoItems = todoItems;
        }

        /// <summary>
        /// <see cref="DocumentId"/> this update is associated with.
        /// </summary>
        public new DocumentId DocumentId => base.DocumentId!;
    }
}
