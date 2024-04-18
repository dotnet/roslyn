// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote;

[ExportWorkspaceService(typeof(IDocumentTrackingService), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteDocumentTrackingService() : IDocumentTrackingService
{
    private DocumentId? _activeDocument;

    public event EventHandler<DocumentId?>? ActiveDocumentChanged;

    public ImmutableArray<DocumentId> GetVisibleDocuments()
        => [];

    public DocumentId? TryGetActiveDocument()
        => _activeDocument;

    internal void SetActiveDocument(DocumentId? documentId)
    {
        _activeDocument = documentId;
        ActiveDocumentChanged?.Invoke(this, documentId);
    }
}
