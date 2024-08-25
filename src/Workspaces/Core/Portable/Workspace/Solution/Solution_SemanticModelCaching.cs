// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

public partial class Solution
{
    /// <summary>
    /// Strongly held reference to the semantic model for the active document.  By strongly holding onto it, we ensure
    /// that it won't be GC'ed between feature requests from multiple features that care about it.  As the active
    /// document has the most features running on it continuously, we definitely do not want to drop this.  Note: this
    /// cached value is only to help with performance.  Not with correctness.  Importantly, the concept of 'active
    /// document' is itself fundamentally racy.  That's ok though as we simply want to settle on these semantic models
    /// settling into a stable state over time.  We don't need to be perfect about it.  They are intentionally not
    /// locked either as we would only have contention right when switching to a new active document, and we would still
    /// latch onto the new document very quickly.
    /// </summary>
    /// <remarks>
    /// It is fine for these fields to never be read.  The purpose is simply to keep a strong reference around so that
    /// they will not be GC'ed as long as the active document stays the same.
    /// </remarks>
#pragma warning disable IDE0052 // Remove unread private members
    private SemanticModel? _activeDocumentSemanticModel;

    /// <inheritdoc cref="_activeDocumentSemanticModel"/>
    private SemanticModel? _activeDocumentNullableDisabledSemanticModel;
#pragma warning restore IDE0052 // Remove unread private members

    internal void OnSemanticModelObtained(DocumentId documentId, SemanticModel semanticModel)
    {
        var service = this.Services.GetRequiredService<IDocumentTrackingService>();

        var activeDocumentId = service.TryGetActiveDocument();
        if (activeDocumentId is null)
        {
            // no active document?  then clear out any caches we have.
            _activeDocumentSemanticModel = null;
            _activeDocumentNullableDisabledSemanticModel = null;
        }
        else if (activeDocumentId != documentId)
        {
            // We have an active document, but we just obtained the semantic model for some other doc.  Nothing to do
            // here, we don't want to cache this.
            return;
        }
        else
        {
            // Ok.  We just obtained the semantic model for the active document.  Make a strong reference to it so that
            // other features that wake up for this active document are sure to be able to reuse the same one.
#pragma warning disable RSEXPERIMENTAL001 // sym-shipped usage of experimental API
            if (semanticModel.NullableAnalysisIsDisabled)
                _activeDocumentNullableDisabledSemanticModel = semanticModel;
            else
                _activeDocumentSemanticModel = semanticModel;
#pragma warning restore RSEXPERIMENTAL001
        }
    }
}
