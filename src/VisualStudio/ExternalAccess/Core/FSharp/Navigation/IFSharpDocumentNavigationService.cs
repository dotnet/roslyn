// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Navigation;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
#endif

internal interface IFSharpDocumentNavigationService : IWorkspaceService
{
    /// <inheritdoc cref="IDocumentNavigationService.CanNavigateToSpanAsync"/>
    bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
    /// <inheritdoc cref="IDocumentNavigationService.CanNavigateToPositionAsync"/>
    bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

    /// <inheritdoc cref="IDocumentNavigationService.GetLocationForSpanAsync"/>
    bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
    /// <inheritdoc cref="IDocumentNavigationService.GetLocationForPositionAsync"/>
    bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);
}
