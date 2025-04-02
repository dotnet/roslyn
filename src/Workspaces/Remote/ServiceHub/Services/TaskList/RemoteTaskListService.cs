// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.TaskList;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed partial class RemoteTaskListService : BrokeredServiceBase, IRemoteTaskListService
{
    internal sealed class Factory : FactoryBase<IRemoteTaskListService>
    {
        protected override IRemoteTaskListService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteTaskListService(arguments);
    }

    public RemoteTaskListService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
    }

    public ValueTask<ImmutableArray<TaskListItem>> GetTaskListItemsAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TaskListItemDescriptor> descriptors, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetRequiredDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var service = document.GetRequiredLanguageService<ITaskListService>();
            return await service.GetTaskListItemsAsync(document, descriptors, cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
