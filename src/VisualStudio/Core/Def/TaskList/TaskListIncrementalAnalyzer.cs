// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.VisualStudio.LanguageServices.TaskList
{
    internal sealed class TaskListIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        private readonly object _gate = new();
        private ImmutableArray<string> _lastTokenList = ImmutableArray<string>.Empty;
        private ImmutableArray<TaskListItemDescriptor> _lastDescriptors = ImmutableArray<TaskListItemDescriptor>.Empty;

        /// <summary>
        /// Set of documents that we have reported an non-empty set of todo comments for.  Used so that we don't bother
        /// notifying the host about documents with empty-todo lists (the common case). Note: no locking is needed for
        /// this set as the incremental analyzer is guaranteed to make all calls sequentially to us.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsWithTaskListItems = new();
        private readonly IGlobalOptionService _globalOptions;
        private readonly VisualStudioTaskListService _listener;

        public TaskListIncrementalAnalyzer(
            IGlobalOptionService globalOptions,
            VisualStudioTaskListService listener)
        {
            _globalOptions = globalOptions;
            _listener = listener;
        }

        public override Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            // Remove the doc id from what we're tracking to prevent unbounded growth in the set.

            // If the doc that is being removed is not in the set of docs we've told the host has todo comments,
            // then no need to notify the host at all about it.
            if (!_documentsWithTaskListItems.Remove(documentId))
                return Task.CompletedTask;

            // Otherwise, report that there should now be no todo comments for this doc.
            return _listener.ReportTaskListItemsAsync(documentId, ImmutableArray<TaskListItem>.Empty, cancellationToken).AsTask();
        }

        private ImmutableArray<TaskListItemDescriptor> GetDescriptors(ImmutableArray<string> tokenList)
        {
            lock (_gate)
            {
                if (!tokenList.SequenceEqual(_lastTokenList))
                {
                    _lastDescriptors = TaskListItemDescriptor.Parse(tokenList);
                    _lastTokenList = tokenList;
                }

                return _lastDescriptors;
            }
        }

        public override async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<ITaskListService>();
            if (service == null)
                return;

            var options = _globalOptions.GetTaskListOptions();
            var descriptors = GetDescriptors(options.Descriptors);

            // We're out of date.  Recompute this info.
            var items = await service.GetTaskListItemsAsync(
                document, descriptors, cancellationToken).ConfigureAwait(false);

            if (items.IsEmpty)
            {
                // Remove this doc from the set of docs with todo comments in it. If this was a doc that previously
                // had todo comments in it, then fall through and notify the host so it can clear them out.
                // Otherwise, bail out as there's no need to inform the host of this.
                if (!_documentsWithTaskListItems.Remove(document.Id))
                    return;
            }
            else
            {
                // Doc has some todo comments, record that, and let the host know.
                _documentsWithTaskListItems.Add(document.Id);
            }

            // Now inform VS about this new information
            await _listener.ReportTaskListItemsAsync(document.Id, items, cancellationToken).ConfigureAwait(false);
        }
    }
}
