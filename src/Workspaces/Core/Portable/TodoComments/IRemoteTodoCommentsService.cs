// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteTodoCommentsDiscoveryService
    {
        internal interface ICallback
        {
            ValueTask ReportTodoCommentDataAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken);
        }

        ValueTask ComputeTodoCommentsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellation);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteTodoCommentsDiscoveryService)), Shared]
    internal sealed class RemoteTodoCommentsDiscoveryCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteTodoCommentsDiscoveryService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteTodoCommentsDiscoveryCallbackDispatcher()
        {
        }

        private ITodoCommentsListener GetLogService(RemoteServiceCallbackId callbackId)
            => (ITodoCommentsListener)GetCallback(callbackId);

        public ValueTask ReportTodoCommentDataAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, ImmutableArray<TodoCommentData> data, CancellationToken cancellationToken)
            => GetLogService(callbackId).ReportTodoCommentDataAsync(documentId, data, cancellationToken);
    }
}
