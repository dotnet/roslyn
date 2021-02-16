// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal abstract partial class AbstractTodoCommentsIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        private readonly object _gate = new();
        private string? _lastOptionText = null;
        private ImmutableArray<TodoCommentDescriptor> _lastDescriptors = default;

        /// <summary>
        /// Set of documents that we have reported an empty set of todo comments for.  Don't both re-reporting these
        /// documents as long as we keep getting no todo comments produced for them.
        /// </summary>
        private readonly HashSet<DocumentId> _documentsReportedWithEmptyTodoComments = new();

        protected AbstractTodoCommentsIncrementalAnalyzer()
        {
        }

        protected abstract ValueTask ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken);

        public override bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => e.Option == TodoCommentOptions.TokenList;

        public override Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            // Remove the doc id from what we're tracking to prevent unbounded growth in the set.
            lock (_gate)
            {
                // If the doc was already in the set, then we know the client believes there are zero comments for it.
                // So we don't have to redundantly tell it that again.
                if (_documentsReportedWithEmptyTodoComments.Remove(documentId))
                    return Task.CompletedTask;
            }

            // Otherwise, report that there should now be no todo comments for this doc.
            return ReportTodoCommentDataAsync(documentId, ImmutableArray<TodoCommentData>.Empty, cancellationToken).AsTask();
        }

        private ImmutableArray<TodoCommentDescriptor> GetTodoCommentDescriptors(Document document)
        {
            var optionText = document.Project.Solution.Options.GetOption<string>(TodoCommentOptions.TokenList);

            lock (_gate)
            {
                if (optionText != _lastOptionText)
                {
                    _lastDescriptors = TodoCommentDescriptor.Parse(optionText);
                    _lastOptionText = optionText;
                }

                return _lastDescriptors;
            }
        }

        public override async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            var todoCommentService = document.GetLanguageService<ITodoCommentService>();
            if (todoCommentService == null)
                return;

            var descriptors = GetTodoCommentDescriptors(document);

            // We're out of date.  Recompute this info.
            var todoComments = await todoCommentService.GetTodoCommentsAsync(
                document, descriptors, cancellationToken).ConfigureAwait(false);

            // Convert the roslyn-level results to the more VS oriented line/col data.
            using var _ = ArrayBuilder<TodoCommentData>.GetInstance(out var converted);
            await TodoComment.ConvertAsync(
                document, todoComments, converted, cancellationToken).ConfigureAwait(false);

            var data = converted.ToImmutable();
            lock (_gate)
            {
                if (data.IsEmpty)
                {
                    // If we already reported this doc has no todo comments, don't bother doing it again. Otherwise,
                    // notify the client.
                    if (!_documentsReportedWithEmptyTodoComments.Add(document.Id))
                    {
                        return;
                    }
                }
                else
                {
                    // Doc has some todo comments, remove the 'do not report' list and notify the client.
                    _documentsReportedWithEmptyTodoComments.Remove(document.Id);
                }
            }

            // Now inform VS about this new information
            await ReportTodoCommentDataAsync(document.Id, data, cancellationToken).ConfigureAwait(false);
        }
    }
}
