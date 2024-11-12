// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal interface IRemoteAssetSynchronizationService
{
    /// <summary>
    /// Synchronize data to OOP proactively so that the corresponding solution is often already available when features
    /// call into it.
    /// </summary>
    ValueTask SynchronizePrimaryWorkspaceAsync(Checksum solutionChecksum, CancellationToken cancellationToken);

    /// <summary>
    /// Synchronize the text changes made by a user to a particular document as they are editing it.  By sending over
    /// the text changes as they happen, we can attempt to 'prime' the remote asset cache with a final <see
    /// cref="SourceText"/> that is built based off of retrieving the remote source text with a checksum corresponding
    /// to <paramref name="baseTextChecksum"/> and then applying the <paramref name="textChanges"/> to it.  Then, when
    /// the next remote call comes in for the new solution snapshot, it can hopefully just pluck that text out of the
    /// cache without having to sync the <em>entire</em> contents of the file over.
    /// </summary>
    ValueTask SynchronizeTextAsync(DocumentId documentId, Checksum baseTextChecksum, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken);

    /// <summary>
    /// Synchronize over what the user's current active document is that they're editing.  This can then be used by the
    /// remote side to help determine which documents are best to strongly hold onto data for, and which should just
    /// hold on weakly.  Given how much work happens on the active document, this can help avoid the remote side from
    /// continually creating and then throwing away that data.
    /// </summary>
    ValueTask SynchronizeActiveDocumentAsync(DocumentId? documentId, CancellationToken cancellationToken);
}
