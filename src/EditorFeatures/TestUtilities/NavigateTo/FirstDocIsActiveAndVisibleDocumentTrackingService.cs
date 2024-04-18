// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;

internal sealed class FirstDocIsActiveAndVisibleDocumentTrackingService : IDocumentTrackingService
{
    private readonly Workspace _workspace;

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    private FirstDocIsActiveAndVisibleDocumentTrackingService(Workspace workspace)
        => _workspace = workspace;

    public bool SupportsDocumentTracking => true;

    public event EventHandler<DocumentId?> ActiveDocumentChanged { add { } remove { } }

    public DocumentId TryGetActiveDocument()
        => _workspace.CurrentSolution.Projects.First().DocumentIds.First();

    public ImmutableArray<DocumentId> GetVisibleDocuments()
        => ImmutableArray.Create(_workspace.CurrentSolution.Projects.First().DocumentIds.First());

    [ExportWorkspaceServiceFactory(typeof(IDocumentTrackingService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    public class Factory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public Factory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new FirstDocIsActiveAndVisibleDocumentTrackingService(workspaceServices.Workspace);
    }
}
