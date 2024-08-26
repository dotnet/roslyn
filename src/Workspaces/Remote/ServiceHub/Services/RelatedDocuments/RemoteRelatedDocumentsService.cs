// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            await foreach (var relatedDocument in service.GetRelatedDocumentIdsAsync(document, position, cancellationToken))
            {
                await callback.InvokeAsync((callback, cancellationToken) => callback.ReportRelatedDocumentAsync(
                    callbackId, relatedDocument, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }, cancellationToken);
    }
}
