// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.RelatedDocuments;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteRelatedDocumentsService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments,
    RemoteCallback<IRemoteRelatedDocumentsService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteRelatedDocumentsService
{
    internal sealed class Factory : FactoryBase<IRemoteRelatedDocumentsService, IRemoteRelatedDocumentsService.ICallback>
    {
        protected override IRemoteRelatedDocumentsService CreateService(
            in ServiceConstructionArguments arguments, RemoteCallback<IRemoteRelatedDocumentsService.ICallback> callback)
            => new RemoteRelatedDocumentsService(arguments, callback);
    }

    private readonly RemoteCallback<IRemoteRelatedDocumentsService.ICallback> _callback = callback;

    private Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> GetCallbackFunction(RemoteServiceCallbackId callbackId)
        // When the callback is invoked on our side (the remote side), forward the values back to the host.
        => (documentIds, cancellationToken) => _callback.InvokeAsync(
            (callback, cancellationToken) => callback.ReportRelatedDocumentAsync(callbackId, documentIds, cancellationToken),
            cancellationToken);

    public ValueTask GetRelatedDocumentIdsAsync(
        Checksum solutionChecksum,
        DocumentId documentId,
        int position,
        RemoteServiceCallbackId callbackId,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = solution.GetRequiredDocument(documentId);
            var service = document.GetRequiredLanguageService<IRelatedDocumentsService>();

            await service.GetRelatedDocumentIdsAsync(
                document, position, GetCallbackFunction(callbackId), cancellationToken).ConfigureAwait(false);
        }, cancellationToken);
    }
}
