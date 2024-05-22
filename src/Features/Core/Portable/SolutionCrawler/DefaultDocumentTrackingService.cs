// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SolutionCrawler;

[ExportWorkspaceService(typeof(IDocumentTrackingService), ServiceLayer.Default)]
[Shared]
internal sealed class DefaultDocumentTrackingService : IDocumentTrackingService
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultDocumentTrackingService()
    {
    }

    public bool SupportsDocumentTracking => false;

    public event EventHandler<DocumentId?> ActiveDocumentChanged { add { } remove { } }
    public event EventHandler<EventArgs> NonRoslynBufferTextChanged { add { } remove { } }

    public ImmutableArray<DocumentId> GetVisibleDocuments()
        => [];

    public DocumentId? TryGetActiveDocument()
        => null;
}
