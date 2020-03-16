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
using Microsoft.CodeAnalysis.Text;
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
        private ParsedTodoCommentDescriptors? _lastDescriptorInfo;

        public RemoteTodoCommentsIncrementalAnalyzer(Workspace workspace, RemoteEndPoint endPoint)
        {
            _endPoint = endPoint;
        }

        public override bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            => e.Option == TodoCommentOptions.TokenList;

        private ParsedTodoCommentDescriptors GetParsedTodoCommentDescriptors(Document document)
        {
            var optionText = document.Project.Solution.Options.GetOption(TodoCommentOptions.TokenList);

            lock (_gate)
            {
                if (_lastDescriptorInfo == null || _lastDescriptorInfo.Value.OptionText != optionText)
                    _lastDescriptorInfo = ParsedTodoCommentDescriptors.Parse(optionText);

                return _lastDescriptorInfo.Value;
            }
        }

        public override async Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
        {
            var todoCommentService = document.GetLanguageService<ITodoCommentService>();
            if (todoCommentService == null)
                return;

            var descriptorInfo = GetParsedTodoCommentDescriptors(document);

            // We're out of date.  Recompute this info.
            var todoComments = await todoCommentService.GetTodoCommentsAsync(
                document, descriptorInfo.Descriptors, cancellationToken).ConfigureAwait(false);

            // Convert the roslyn-level results to the more VS oriented line/col data.
            using var _ = ArrayBuilder<TodoCommentInfo>.GetInstance(out var converted);
            await ConvertAsync(
                document, todoComments, converted, cancellationToken).ConfigureAwait(false);

            // Now inform VS about this new information
            await _endPoint.InvokeAsync(
                nameof(ITodoCommentsServiceCallback.ReportTodoCommentsAsync),
                new object[] { document.Id, converted },
                cancellationToken).ConfigureAwait(false);
        }

        private async Task ConvertAsync(
            Document document,
            ImmutableArray<TodoComment> todoComments,
            ArrayBuilder<TodoCommentInfo> converted,
            CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            foreach (var comment in todoComments)
                converted.Add(Convert(document, sourceText, syntaxTree, comment));
        }

        private TodoCommentInfo Convert(
            Document document, SourceText text, SyntaxTree tree, TodoComment comment)
        {
            // make sure given position is within valid text range.
            var textSpan = new TextSpan(Math.Min(text.Length, Math.Max(0, comment.Position)), 0);

            var location = tree.GetLocation(textSpan);
            // var location = tree == null ? Location.Create(document.FilePath, textSpan, text.Lines.GetLinePositionSpan(textSpan)) : tree.GetLocation(textSpan);
            var originalLineInfo = location.GetLineSpan();
            var mappedLineInfo = location.GetMappedLineSpan();

            return new TodoCommentInfo
            {
                Priority = comment.Descriptor.Priority,
                Message = comment.Message,
                DocumentId = document.Id,
                OriginalLine = originalLineInfo.StartLinePosition.Line,
                OriginalColumn = originalLineInfo.StartLinePosition.Character,
                OriginalFilePath = document.FilePath,
                MappedLine = mappedLineInfo.StartLinePosition.Line,
                MappedColumn = mappedLineInfo.StartLinePosition.Character,
                MappedFilePath = mappedLineInfo.GetMappedFilePathIfExist(),
            };
        }
    }
}
