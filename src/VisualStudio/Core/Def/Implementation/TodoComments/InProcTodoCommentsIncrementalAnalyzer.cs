// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TodoComments
{
    internal sealed class InProcTodoCommentsIncrementalAnalyzer : AbstractTodoCommentsIncrementalAnalyzer
    {
        private readonly ITodoCommentsListener _listener;

        public InProcTodoCommentsIncrementalAnalyzer(ITodoCommentsListener listener)
            => _listener = listener;

        protected override Task ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken)
            => _listener.ReportTodoCommentDataAsync(documentId, data, cancellationToken);
    }
}
