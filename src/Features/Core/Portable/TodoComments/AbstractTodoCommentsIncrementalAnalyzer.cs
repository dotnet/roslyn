// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        private readonly object _gate = new object();
        private string? _lastOptionText = null;
        private ImmutableArray<TodoCommentDescriptor> _lastDescriptors = default;

        protected AbstractTodoCommentsIncrementalAnalyzer()
        {
        }

        protected abstract Task ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken);

        public override bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => e.Option == TodoCommentOptions.TokenList;

        public override Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            // Just report this back as there being no more comments for this document.
            return ReportTodoCommentDataAsync(documentId, ImmutableArray<TodoCommentData>.Empty, cancellationToken);
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

            // Now inform VS about this new information
            await ReportTodoCommentDataAsync(document.Id, converted.ToImmutable(), cancellationToken).ConfigureAwait(false);
        }
    }
}
