// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteTodoCommentsIncrementalAnalyzer : AbstractTodoCommentsIncrementalAnalyzer
    {
        /// <summary>
        /// Channel back to VS to inform it of the designer attributes we discover.
        /// </summary>
        private readonly RemoteCallback<IRemoteTodoCommentsDiscoveryService.ICallback> _callback;
        private readonly RemoteServiceCallbackId _callbackId;

        public RemoteTodoCommentsIncrementalAnalyzer(RemoteCallback<IRemoteTodoCommentsDiscoveryService.ICallback> callback, RemoteServiceCallbackId callbackId)
        {
            _callback = callback;
            _callbackId = callbackId;
        }

        protected override ValueTask ReportTodoCommentDataAsync(DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken)
            => _callback.InvokeAsync(
                (callback, cancellationToken) => callback.ReportTodoCommentDataAsync(_callbackId, documentId, data, cancellationToken),
                cancellationToken);

        protected override ValueTask<TodoCommentOptions> GetOptionsAsync(CancellationToken cancellationToken)
            => _callback.InvokeAsync(
                (callback, cancellationToken) => callback.GetOptionsAsync(_callbackId, cancellationToken),
                cancellationToken);
    }
}
