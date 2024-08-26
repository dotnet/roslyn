// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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

internal abstract class AbstractRelatedDocumentsService : IRelatedDocumentsService
{
    public async IAsyncEnumerable<DocumentId> GetRelatedDocumentIdsAsync(
        Document document, int position, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var project = document.Project;
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var channel = Channel.CreateUnbounded<DocumentId>();

            // Intentionally don't await this.  We're kicking off this work and want it run concurrently.
            _ = CallRemoteClientAsync(client, channel, document, position, cancellationToken).ReportNonFatalErrorAsync();

            // The remote call will write into the channel.  Read from that here and return the results out to the
            // consumer.
            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
                yield return result;
        }
        else
        {
            await foreach (var result in GetRelatedDocumentIdsInCurrentProcessAsync(document, position, cancellationToken))
                yield return result;
        }
    }

    private static async Task CallRemoteClientAsync(
        RemoteHostClient client,
        Channel<DocumentId> channel,
        Document document,
        int position,
        CancellationToken cancellationToken)
    {
        // Forward responses from the remote side over to the channel.
        var callback = new RemoteRelatedDocumentsServiceCallback(
            (documentId, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                channel.Writer.TryWrite(documentId);
                return ValueTaskFactory.CompletedTask;
            },
            cancellationToken);

        Exception? exception = null;
        try
        {
            var result = await client.TryInvokeAsync<IRemoteRelatedDocumentsService>(
                document.Project,
                (service, solutionChecksum, callbackId, cancellationToken) => service.GetRelatedDocumentIdsAsync(solutionChecksum, document.Id, position, callbackId, cancellationToken),
                callback,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when ((exception = ex) != null)
        {
            throw ExceptionUtilities.Unreachable();
        }
        finally
        {
            // Complete the channel so that the reading side knows we're done.
            channel.Writer.TryComplete(exception);
        }
    }

    protected abstract IAsyncEnumerable<DocumentId> GetRelatedDocumentIdsInCurrentProcessAsync(
        Document document, int position, CancellationToken cancellationToken);
}
