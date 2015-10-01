// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Common;

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

        ImmutableArray<TodoItem> GetTodoItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken);

        /// <summary>
        /// Get current UpdatedEventArgs stored in ITodoListProvider
        /// </summary>
        IEnumerable<UpdatedEventArgs> GetTodoItemsUpdatedEventArgs(Workspace workspace, CancellationToken cancellationToken);
    }
}
