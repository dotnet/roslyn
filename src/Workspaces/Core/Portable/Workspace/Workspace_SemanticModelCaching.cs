// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis;

internal partial class Workspace
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
    private SemanticModel? _activeDocumentSemanticModel;

    /// <inheritdoc cref="_activeDocumentSemanticModel"/>
    private SemanticModel? _activeDocumentNullableDisabledSemanticModel;

    internal void OnSemanticModelObtained(DocumentId documentId, SemanticModel semanticModel)
    {
        var service = this.Services.GetRequiredService<IDocumentTrackingService>();
        if (!service.SupportsDocumentTracking)
            return;

        var activeDocumentId = service.TryGetActiveDocument();
        if (activeDocumentId != documentId)
            return;

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
