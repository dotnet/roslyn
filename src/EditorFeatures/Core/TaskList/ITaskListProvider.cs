// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.Editor.TaskList
{
    /// <summary>
    /// Returns Roslyn todo list from the workspace.
    /// </summary>
    internal interface ITaskListProvider
    {
        /// <summary>
        /// An event that is raised when the todo list has changed.  
        /// 
        /// When an event handler is newly added, this event will fire for the currently available todo items and then
        /// afterward for any changes since.
        /// </summary>
        event EventHandler<TaskListUpdatedArgs> TaskListUpdated;

        ImmutableArray<TaskListItem> GetTaskListItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken);
    }
}
