// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal static class RenameTrackingDismisser
{
    internal static void DismissRenameTracking(Workspace workspace, DocumentId documentId)
        => RenameTrackingTaggerProvider.ResetRenameTrackingState(workspace, documentId);

    internal static void DismissRenameTracking(Workspace workspace, IEnumerable<DocumentId> documentIds)
    {
        foreach (var docId in documentIds)
        {
            DismissRenameTracking(workspace, docId);
        }
    }

    internal static bool DismissVisibleRenameTracking(Workspace workspace, DocumentId documentId)
        => RenameTrackingTaggerProvider.ResetVisibleRenameTrackingState(workspace, documentId);
}
