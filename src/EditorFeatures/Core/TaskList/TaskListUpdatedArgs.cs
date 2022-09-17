// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.Editor.TaskList
{
    internal sealed class TaskListUpdatedArgs : UpdatedEventArgs
    {
        /// <summary>
        /// Solution this task items are associated with
        /// </summary>
        public Solution Solution { get; }

        /// <summary>
        /// The task items associated with the ID.
        /// </summary>
        public ImmutableArray<TaskListItem> TaskListItems { get; }

        public TaskListUpdatedArgs(
            object id, Solution solution, DocumentId documentId, ImmutableArray<TaskListItem> items)
            : base(id, solution.Workspace, documentId.ProjectId, documentId)
        {
            Solution = solution;
            TaskListItems = items;
        }

        /// <summary>
        /// <see cref="DocumentId"/> this update is associated with.
        /// </summary>
        public new DocumentId DocumentId => base.DocumentId!;
    }
}
