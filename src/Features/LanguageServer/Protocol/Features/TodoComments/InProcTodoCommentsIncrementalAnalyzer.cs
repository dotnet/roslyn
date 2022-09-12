// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.TodoComments
{
    internal sealed class InProcTodoCommentsIncrementalAnalyzer : AbstractTodoCommentsIncrementalAnalyzer
    {
        private readonly TodoCommentsListener _listener;

        public InProcTodoCommentsIncrementalAnalyzer(TodoCommentsListener listener)
            => _listener = listener;

        protected override ValueTask ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TaskListItem> data, CancellationToken cancellationToken)
            => _listener.ReportTodoCommentDataAsync(documentId, data, cancellationToken);

        protected override ValueTask<TaskListOptions> GetOptionsAsync(CancellationToken cancellationToken)
            => _listener.GetOptionsAsync(cancellationToken);
    }
}
