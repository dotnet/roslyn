// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.ServiceHub.Framework;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteTodoCommentsIncrementalAnalyzer : AbstractTodoCommentsIncrementalAnalyzer
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly ITodoCommentsListener _callback;

        public RemoteTodoCommentsIncrementalAnalyzer(ITodoCommentsListener callback)
            => _callback = callback;

        protected override async Task ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken)
        {
            try
            {
                await _callback.ReportTodoCommentDataAsync(documentId, data, cancellationToken).ConfigureAwait(false);
            }
            catch (ConnectionLostException)
            {
                // The client might have terminated without signalling the cancellation token.
                // Ignore this failure to avoid reporting Watson from the solution crawler.
                // Same effect as if cancellation had been requested.
            }
        }
    }
}
