// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface IDocumentNavigationService : IWorkspaceService
    {
        /// <summary>
        /// Determines whether it is possible to navigate to the given position in the specified document.
        /// </summary>
        bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options, bool allowInvalidSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, OptionSet options, CancellationToken cancellationToken);
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        public static bool CanNavigateToPosition(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.CanNavigateToPosition(workspace, documentId, position, virtualSpace: 0, cancellationToken);

        public static bool TryNavigateToSpan(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => service.TryNavigateToSpan(workspace, documentId, textSpan, options: null, cancellationToken);

        public static bool TryNavigateToSpan(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options, CancellationToken cancellationToken)
            => service.TryNavigateToSpan(workspace, documentId, textSpan, options, allowInvalidSpan: false, cancellationToken);

        public static bool TryNavigateToLineAndOffset(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
            => service.TryNavigateToLineAndOffset(workspace, documentId, lineNumber, offset, options: null, cancellationToken);

        public static bool TryNavigateToPosition(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.TryNavigateToPosition(workspace, documentId, position, virtualSpace: 0, options: null, cancellationToken);
    }
}
