// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Returns Roslyn todo list from the workspace.
    /// </summary>
    internal interface ITodoListProvider
    {
        /// <summary>
        /// An event that is raised when the todo list has changed.  
        /// 
        /// When an event handler is newly added, this event will fire for the currently available todo items and then
        /// afterward for any changes since.
        /// </summary>
        event EventHandler<TodoItemsUpdatedArgs> TodoListUpdated;

        ImmutableArray<TodoCommentData> GetTodoItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken);
    }
}
