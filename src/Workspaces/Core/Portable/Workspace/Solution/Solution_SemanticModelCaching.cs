// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

public partial class Solution
{
    /// <summary>
    /// Strongly held reference to the semantic models for the active document (and its related documents linked into
    /// other projects).  By strongly holding onto then, we ensure that they won't be GC'ed between feature requests
    /// from multiple features that care about it.  As the active document has the most features running on it
    /// continuously, we definitely do not want to drop this.  Note: this cached value is only to help with performance.
    /// Not with correctness.  Importantly, the concept of 'active document' is itself fundamentally racy.  That's ok
    /// though as we simply want to settle on these semantic models settling into a stable state over time.  We don't
    /// need to be perfect about it.
    /// </summary>
    private ImmutableDictionary<DocumentId, ImmutableHashSet<SemanticModel>> _activeSemanticModels = ImmutableDictionary<DocumentId, ImmutableHashSet<SemanticModel>>.Empty;

    internal void OnSemanticModelObtained(
        DocumentId documentId, SemanticModel semanticModel)
    {
        var service = this.Services.GetRequiredService<IDocumentTrackingService>();

        var activeDocumentId = service.TryGetActiveDocument();
        if (activeDocumentId is null)
        {
            // No known active document.  Clear out any cached semantic models we have.
            _activeSemanticModels = _activeSemanticModels.Clear();
            return;
        }

        var relatedDocumentIds = this.GetRelatedDocumentIds(activeDocumentId);

        // Clear out any entries for cached documents that are no longer active.
        foreach (var (existingDocId, _) in _activeSemanticModels)
        {
            if (!relatedDocumentIds.Contains(existingDocId))
                ImmutableInterlocked.TryRemove(ref _activeSemanticModels, existingDocId, out _);
        }

        // If this is a semantic model for the active document (or any of its related documents), cache it.
        if (relatedDocumentIdsSet.Contains(documentId))
        {
            ImmutableInterlocked.AddOrUpdate(
                ref _activeSemanticModels,
                documentId,
                addValueFactory: documentId => [semanticModel],
                updateValueFactory: (_, set) => set.Add(semanticModel));
        }
    }
}
