// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.TodoComments;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsIncrementalAnalyzer : IncrementalAnalyzerBase
    {
        private const string DataKey = "TodoComment";

        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly RemoteEndPoint _endPoint;

        private readonly object _gate = new object();
        private string? _lastOptionText = null;
        private ImmutableArray<TodoCommentDescriptor> _lastDescriptors = default;

        public RemoteTodoCommentsIncrementalAnalyzer(RemoteEndPoint endPoint)
            => _endPoint = endPoint;

        public override bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => e.Option == TodoCommentOptions.TokenList;

        public override Task RemoveDocumentAsync(DocumentId documentId, CancellationToken cancellationToken)
        {
            // Just report this back as there being no more comments for this document.
            return _endPoint.InvokeAsync(
                nameof(ITodoCommentsListener.ReportTodoCommentDataAsync),
                new object[] { documentId, Array.Empty<TodoCommentData>() },
                cancellationToken);
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
            await ConvertAsync(
                document, todoComments, converted, cancellationToken).ConfigureAwait(false);

            // Now inform VS about this new information
            await _endPoint.InvokeAsync(
                nameof(ITodoCommentsListener.ReportTodoCommentDataAsync),
                new object[] { document.Id, converted },
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task ConvertAsync(
            Document document,
            ImmutableArray<TodoComment> todoComments,
            ArrayBuilder<TodoCommentData> converted,
            CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            foreach (var comment in todoComments)
                converted.Add(comment.CreateSerializableData(document, sourceText, syntaxTree));
        }
    }
}
