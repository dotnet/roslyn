// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.RelatedDocuments;

internal interface IRelatedDocumentsService : ILanguageService
{
    /// <summary>
    /// Given an document, and an optional position in that document, streams a unique list of documents Ids that the
    /// language think are "related".  It is up to the language to define what "related" means.  However, common
    /// examples might be checking to see which symbols are used at that particular location and prioritizing documents
    /// those symbols are defined in.
    /// </summary>
    IAsyncEnumerable<DocumentId> GetRelatedDocumentIdsAsync(Document document, int position, CancellationToken cancellationToken);
}

internal interface IRemoteRelatedDocumentsService
{
    public interface ICallback
    {
        ValueTask ReportRelatedDocumentAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
    }

    public ValueTask GetRelatedDocumentIdsAsync(
        Checksum solutionChecksum, DocumentId documentId, int position, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
}

internal abstract class AbstractRelatedDocumentsService : IRelatedDocumentsService
{
    public async IAsyncEnumerable<DocumentId> GetRelatedDocumentIdsAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);

    }
}
