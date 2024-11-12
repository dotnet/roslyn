// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    private ImmutableArray<(DocumentId documentId, SemanticModel semanticModel)> _activeSemanticModels = [];

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
        _activeSemanticModels = _activeSemanticModels.RemoveAll(
            tuple => !relatedDocumentIds.Contains(tuple.documentId));

        // If this is a semantic model for the active document (or any of its related documents), and we haven't already
        // cached it, then do so.
        if (!_activeSemanticModels.Contains((documentId, semanticModel)))
            _activeSemanticModels = _activeSemanticModels.Add((documentId, semanticModel));
    }
}
