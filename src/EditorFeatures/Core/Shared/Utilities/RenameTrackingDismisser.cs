// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class RenameTrackingDismisser
    {
        internal static Task DismissRenameTrackingAsync(IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            => RenameTrackingTaggerProvider.ResetRenameTrackingStateAsync(threadingContext, workspace, documentId, cancellationToken);

        internal static async Task DismissRenameTrackingAsync(
            IThreadingContext threadingContext,
            Workspace workspace,
            IEnumerable<DocumentId> documentIds,
            CancellationToken cancellationToken)
        {
            foreach (var docId in documentIds)
                await DismissRenameTrackingAsync(threadingContext, workspace, docId, cancellationToken).ConfigureAwait(false);
        }

        internal static Task<bool> DismissVisibleRenameTrackingAsync(IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
            => RenameTrackingTaggerProvider.ResetVisibleRenameTrackingStateAsync(threadingContext, workspace, documentId, cancellationToken);
    }
}
