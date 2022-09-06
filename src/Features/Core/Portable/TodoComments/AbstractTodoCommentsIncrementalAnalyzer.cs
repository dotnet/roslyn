﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal abstract partial class AbstractTodoCommentsIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        private readonly object _gate = new();
        private ImmutableArray<string> _lastTokenList = ImmutableArray<string>.Empty;
        private ImmutableArray<TodoCommentDescriptor> _lastDescriptors = ImmutableArray<TodoCommentDescriptor>.Empty;

        /// <summary>
        /// Set of documents that we have reported an non-empty set of todo comments for.  Used so that we don't bother
        /// notifying the host about documents with empty-todo lists (the common case). Note: no locking is needed for
        /// this set as the incremental analyzer is guaranteed to make all calls sequentially to us.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithTodoComments = new();

        protected AbstractTodoCommentsIncrementalAnalyzer()
        {
        }

        protected abstract ValueTask ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken);
        protected abstract ValueTask<TodoCommentOptions> GetOptionsAsync(CancellationToken cancellationToken);

        public override Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            // Remove the doc id from what we're tracking to prevent unbounded growth in the set.

            // If the doc that is being removed is not in the set of docs we've told the host has todo comments,
            // then no need to notify the host at all about it.
            if (!_documentsWithTodoComments.Remove(documentId))
                return Task.CompletedTask;

            // Otherwise, report that there should now be no todo comments for this doc.
            return ReportTodoCommentDataAsync(documentId, ImmutableArray<TodoCommentData>.Empty, cancellationToken).AsTask();
        }

        private ImmutableArray<TodoCommentDescriptor> GetTodoCommentDescriptors(ImmutableArray<string> tokenList)
        {
            lock (_gate)
            {
                if (!tokenList.SequenceEqual(_lastTokenList))
                {
                    _lastDescriptors = TodoCommentDescriptor.Parse(tokenList);
                    _lastTokenList = tokenList;
                }

                return _lastDescriptors;
            }
        }

        private static ITodoCommentDataService? GetTodoService(Document document)
        {
            // Legacy compat until TypeScript moves to EA pattern.
            var todoService = document.GetLanguageService<ITodoCommentService>();
            if (todoService != null)
                return new TodoCommentServiceWrapper(todoService);

            var todoDataService = document.GetLanguageService<ITodoCommentDataService>();
            if (todoDataService != null)
                return todoDataService;

            return null;
        }

        public override async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            var todoCommentService = GetTodoService(document);
            if (todoCommentService == null)
                return;

            var options = await GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var descriptors = GetTodoCommentDescriptors(options.TokenList);

            // We're out of date.  Recompute this info.
            var todoComments = await todoCommentService.GetTodoCommentDataAsync(
                document, descriptors, cancellationToken).ConfigureAwait(false);

            if (todoComments.IsEmpty)
            {
                // Remove this doc from the set of docs with todo comments in it. If this was a doc that previously
                // had todo comments in it, then fall through and notify the host so it can clear them out.
                // Otherwise, bail out as there's no need to inform the host of this.
                if (!_documentsWithTodoComments.Remove(document.Id))
                    return;
            }
            else
            {
                // Doc has some todo comments, record that, and let the host know.
                _documentsWithTodoComments.Add(document.Id);
            }

            // Now inform VS about this new information
            await ReportTodoCommentDataAsync(document.Id, todoComments, cancellationToken).ConfigureAwait(false);
        }

        private sealed class TodoCommentServiceWrapper : ITodoCommentDataService
        {
            private readonly ITodoCommentService _todoService;

            public TodoCommentServiceWrapper(ITodoCommentService todoService)
            {
                _todoService = todoService;
            }

            public async Task<ImmutableArray<TodoCommentData>> GetTodoCommentDataAsync(
                Document document, ImmutableArray<TodoCommentDescriptor> commentDescriptors, CancellationToken cancellationToken)
            {
                var comments = await _todoService.GetTodoCommentsAsync(document, commentDescriptors, cancellationToken).ConfigureAwait(false);
                return await TodoComment.ConvertAsync(document, comments, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
