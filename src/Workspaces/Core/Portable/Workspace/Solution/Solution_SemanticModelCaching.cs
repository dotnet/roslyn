// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis;

public partial class Solution
{
    /// <summary>
    /// Strongly held reference to the semantic models for the active document (and its related documents linked into
    /// other projects).  By strongly holding onto them, we ensure that they won't be GC'ed between feature requests
    /// from multiple features that care about it.  As the active document has the most features running on it
    /// continuously, we definitely do not want to drop this.  Note: this cached value is only to help with performance.
    /// Not with correctness.  Importantly, the concept of 'active document' is itself fundamentally racy.  That's ok
    /// though as we simply want to settle on these semantic models settling into a stable state over time.  We don't
    /// need to be perfect about it.
    /// </summary>
    private ImmutableArray<(DocumentId documentId, SemanticModel semanticModel)> _activeSemanticModels = [];

    internal void OnSemanticModelObtained(
        DocumentId documentId, SemanticModel semanticModel)
    {
        var service = this.Services.GetRequiredService<IDocumentTrackingService>();

        // Operate on a local reference to the immutable array to ensure a consistent view of it.
        var localArray = _activeSemanticModels;

        // No need to do anything if we're already caching this pair.
        if (localArray.Contains((documentId, semanticModel)))
            return;

        var activeDocumentId = service.TryGetActiveDocument();
        var relatedDocumentIds = activeDocumentId is null ? [] : this.GetRelatedDocumentIds(activeDocumentId);

        // Remove any cached values for documents that are no longer the active document.
        localArray = localArray.RemoveAll(
            tuple => !relatedDocumentIds.Contains(tuple.documentId));

        // Now cache this doc/semantic model pair if it's in the related document set.
        if (relatedDocumentIds.Contains(documentId))
            localArray = localArray.Add((documentId, semanticModel));

        // Note: this code is racy. We could have two threads executing the code above, while only one thread will win
        // here.  We accept that as this code is just intended to help just by making some strong references to semantic
        // models to prevent them from being GC'ed.  We don't need to be perfect about it.
        _activeSemanticModels = localArray;
    }
}
