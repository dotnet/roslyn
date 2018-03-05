// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal static class RenameTrackingDismisser
    {
        internal static void DismissRenameTracking(Workspace workspace, DocumentId documentId)
        {
            RenameTrackingTaggerProvider.ResetRenameTrackingState(workspace, documentId);
        }

        internal static void DismissRenameTracking(Workspace workspace, IEnumerable<DocumentId> documentIds)
        {
            foreach (var docId in documentIds)
            {
                DismissRenameTracking(workspace, docId);
            }
        }

        internal static bool DismissVisibleRenameTracking(Workspace workspace, DocumentId documentId)
        {
            return RenameTrackingTaggerProvider.ResetVisibleRenameTrackingState(workspace, documentId);
        }
    }
}
