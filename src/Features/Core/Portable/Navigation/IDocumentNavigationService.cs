// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal interface IDocumentNavigationService : IWorkspaceService
    {
        /// <summary>
        /// Determines whether it is possible to navigate to the given position in the specified document.
        /// </summary>
        /// <remarks>Legal to call from any thread.</remarks>
        Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        Task<bool> CanNavigateToLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        Task<bool> TryNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        Task<bool> TryNavigateToLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        Task<bool> TryNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken);
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        public static Task<bool> CanNavigateToPositionAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.CanNavigateToPositionAsync(workspace, documentId, position, virtualSpace: 0, cancellationToken);

        public static Task<bool> TryNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => service.TryNavigateToSpanAsync(workspace, documentId, textSpan, NavigationOptions.Default, cancellationToken);

        public static Task<bool> TryNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
            => service.TryNavigateToSpanAsync(workspace, documentId, textSpan, options, allowInvalidSpan: false, cancellationToken);

        public static Task<bool> TryNavigateToLineAndOffsetAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
            => service.TryNavigateToLineAndOffsetAsync(workspace, documentId, lineNumber, offset, NavigationOptions.Default, cancellationToken);

        public static Task<bool> TryNavigateToPositionAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.TryNavigateToPositionAsync(workspace, documentId, position, virtualSpace: 0, NavigationOptions.Default, cancellationToken);
    }
}
