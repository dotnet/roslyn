// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.UnitTests;

using Workspace = CodeAnalysis.Workspace;

[ExportWorkspaceService(typeof(IDocumentNavigationService), ServiceLayer.Test), Shared, PartNotDiscoverable]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MockDocumentNavigationService() : AbstractDocumentNavigationService
{
    public override async Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken)
        => true;

    public override Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, CancellationToken cancellationToken)
        => NavigableLocation.TestAccessor.Create(true);
}
