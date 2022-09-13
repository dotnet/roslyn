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

namespace Microsoft.CodeAnalysis.TaskList
{
    /// <summary>
    /// Interface to allow host (VS) to inform the OOP service to start incrementally analyzing and
    /// reporting results back to the host.
    /// </summary>
    internal interface IRemoteTaskListService
    {
        internal interface ICallback
        {
            ValueTask ReportTaskListItemsAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, ImmutableArray<TaskListItem> data, CancellationToken cancellationToken);
            ValueTask<TaskListOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        }

        ValueTask ComputeTaskListItemsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellation);
        ValueTask ReanalyzeAsync(CancellationToken cancellationToken);

        ValueTask<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken);
    }

    [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteTaskListService)), Shared]
    internal sealed class RemoteTaskListCallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteTaskListService.ICallback
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteTaskListCallbackDispatcher()
        {
        }

        private ITaskListListener GetListener(RemoteServiceCallbackId callbackId)
            => (ITaskListListener)GetCallback(callbackId);

        public ValueTask ReportTaskListItemsAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, ImmutableArray<TaskListItem> data, CancellationToken cancellationToken)
            => GetListener(callbackId).ReportTaskListItemsAsync(documentId, data, cancellationToken);

        public ValueTask<TaskListOptions> GetOptionsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
            => GetListener(callbackId).GetOptionsAsync(cancellationToken);
    }
}
