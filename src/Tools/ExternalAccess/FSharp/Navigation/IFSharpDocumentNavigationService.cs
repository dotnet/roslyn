// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation
{
    internal interface IFSharpDocumentNavigationService : IWorkspaceService
    {
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan);
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset);
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0);
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options = null);
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options = null);
        [Obsolete("Call overload that takes a CancellationToken", error: false)]
        bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet options = null);

        /// <inheritdoc cref="IDocumentNavigationService.CanNavigateToSpanAsync"/>
        bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        /// <inheritdoc cref="IDocumentNavigationService.CanNavigateToLineAndOffsetAsync"/>
        bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);
        /// <inheritdoc cref="IDocumentNavigationService.CanNavigateToPositionAsync"/>
        bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

        /// <inheritdoc cref="IDocumentNavigationService.TryNavigateToSpanAsync"/>
        bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);
        /// <inheritdoc cref="IDocumentNavigationService.TryNavigateToLineAndOffsetAsync"/>
        bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);
        /// <inheritdoc cref="IDocumentNavigationService.TryNavigateToPositionAsync"/>
        bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);
    }
}
