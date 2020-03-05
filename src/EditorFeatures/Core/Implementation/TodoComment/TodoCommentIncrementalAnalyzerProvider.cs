﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Common;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    [Shared]
    [Export(typeof(ITodoListProvider))]
    [ExportIncrementalAnalyzerProvider(
        name: nameof(TodoCommentIncrementalAnalyzerProvider),
        workspaceKinds: new[] { WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.MiscellaneousFiles })]
    internal class TodoCommentIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider, ITodoListProvider
    {
        private static readonly ConditionalWeakTable<Workspace, TodoCommentIncrementalAnalyzer> s_analyzers = new ConditionalWeakTable<Workspace, TodoCommentIncrementalAnalyzer>();

        private readonly TodoCommentTokens _todoCommentTokens;
        private readonly EventListenerTracker<ITodoListProvider> _eventListenerTracker;

        [ImportingConstructor]
        public TodoCommentIncrementalAnalyzerProvider(
            TodoCommentTokens todoCommentTokens,
            [ImportMany]IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners)
        {
            _todoCommentTokens = todoCommentTokens;
            _eventListenerTracker = new EventListenerTracker<ITodoListProvider>(eventListeners, WellKnownEventListeners.TodoListProvider);
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return s_analyzers.GetValue(workspace, w =>
               new TodoCommentIncrementalAnalyzer(w, this, _todoCommentTokens));
        }

        internal void RaiseTaskListUpdated(object id, Workspace workspace, Solution solution, ProjectId projectId, DocumentId documentId, ImmutableArray<TodoItem> items)
        {
            _eventListenerTracker.EnsureEventListener(workspace, this);

            this.TodoListUpdated?.Invoke(this, new TodoItemsUpdatedArgs(Tuple.Create(this, id), workspace, solution, projectId, documentId, items));
        }

        public event EventHandler<TodoItemsUpdatedArgs> TodoListUpdated;

        public ImmutableArray<TodoItem> GetTodoItems(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            var analyzer = TryGetAnalyzer(workspace);
            if (analyzer == null)
            {
                return ImmutableArray<TodoItem>.Empty;
            }

            var document = workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                return ImmutableArray<TodoItem>.Empty;
            }

            return analyzer.GetTodoItems(workspace, document.Id, cancellationToken);
        }

        public IEnumerable<UpdatedEventArgs> GetTodoItemsUpdatedEventArgs(Workspace workspace, CancellationToken cancellationToken)
        {
            var analyzer = TryGetAnalyzer(workspace);
            if (analyzer == null)
            {
                return ImmutableArray<UpdatedEventArgs>.Empty;
            }

            return analyzer.GetTodoItemsUpdatedEventArgs(workspace);
        }

        private TodoCommentIncrementalAnalyzer TryGetAnalyzer(Workspace workspace)
        {
            if (s_analyzers.TryGetValue(workspace, out var analyzer))
            {
                return analyzer;
            }

            return null;
        }
    }
}
