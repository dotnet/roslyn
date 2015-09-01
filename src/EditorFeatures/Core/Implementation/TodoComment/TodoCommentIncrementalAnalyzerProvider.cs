// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.Editor.Implementation.TodoComments
{
    [Export(typeof(ITodoListProvider))]
    [Shared]
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host, WorkspaceKind.Interactive, WorkspaceKind.MiscellaneousFiles)]
    internal class TodoCommentIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider, ITodoListProvider
    {
        private static readonly ConditionalWeakTable<Workspace, TodoCommentIncrementalAnalyzer> s_analyzers = new ConditionalWeakTable<Workspace, TodoCommentIncrementalAnalyzer>();
        private readonly TodoCommentTokens _todoCommentTokens;

        [ImportingConstructor]
        public TodoCommentIncrementalAnalyzerProvider(TodoCommentTokens todoCommentTokens)
        {
            _todoCommentTokens = todoCommentTokens;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            return s_analyzers.GetValue(workspace, w =>
               new TodoCommentIncrementalAnalyzer(w, w.Services.GetService<IOptionService>(), this, _todoCommentTokens));
        }

        internal void RaiseTaskListUpdated(object id, Workspace workspace, ProjectId projectId, DocumentId documentId, ImmutableArray<TodoItem> items)
        {
            var handler = this.TodoListUpdated;
            if (handler != null)
            {
                handler(this, new TodoListEventArgs(Tuple.Create(this, id), workspace, projectId, documentId, items));
            }
        }

        public event EventHandler<TodoListEventArgs> TodoListUpdated;

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

        private TodoCommentIncrementalAnalyzer TryGetAnalyzer(Workspace workspace)
        {
            TodoCommentIncrementalAnalyzer analyzer;
            if (s_analyzers.TryGetValue(workspace, out analyzer))
            {
                return analyzer;
            }

            return null;
        }
    }
}
