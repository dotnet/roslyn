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

namespace Microsoft.CodeAnalysis.RelatedDocuments;

internal interface IRemoteRelatedDocumentsService
{
    ValueTask GetRelatedDocumentIdsAsync(
        Checksum solutionChecksum, DocumentId documentId, int position, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

    public interface ICallback
    {
        ValueTask ReportRelatedDocumentAsync(RemoteServiceCallbackId callbackId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
    }
}

[ExportRemoteServiceCallbackDispatcher(typeof(IRemoteRelatedDocumentsService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RelatedDocumentsServiceServerCallbackDispatcher() : RemoteServiceCallbackDispatcher, IRemoteRelatedDocumentsService.ICallback
{
    private new RelatedDocumentsServiceCallback GetCallback(RemoteServiceCallbackId callbackId)
        => (RelatedDocumentsServiceCallback)base.GetCallback(callbackId);

    public ValueTask ReportRelatedDocumentAsync(RemoteServiceCallbackId callbackId, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
        => GetCallback(callbackId).ReportRelatedDocumentAsync(documentIds);
}

internal sealed class RelatedDocumentsServiceCallback(
    Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> onRelatedDocumentFoundAsync,
    CancellationToken cancellationToken)
{
    public ValueTask ReportRelatedDocumentAsync(ImmutableArray<DocumentId> documentIds)
        => onRelatedDocumentFoundAsync(documentIds, cancellationToken);
}
