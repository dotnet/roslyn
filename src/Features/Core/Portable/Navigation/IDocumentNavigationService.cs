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
        Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        Task<bool> CanNavigateToLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        Task<bool> CanNavigateToPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

        Task<INavigableLocation?> GetLocationForSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, bool allowInvalidSpan, CancellationToken cancellationToken);
        Task<INavigableLocation?> GetLocationForLineAndOffsetAsync(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);
        Task<INavigableLocation?> GetLocationForPositionAsync(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        public static Task<bool> CanNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => service.CanNavigateToSpanAsync(workspace, documentId, textSpan, allowInvalidSpan: false, cancellationToken);

        public static Task<bool> CanNavigateToPositionAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.CanNavigateToPositionAsync(workspace, documentId, position, virtualSpace: 0, cancellationToken);

        public static Task<INavigableLocation?> GetLocationForSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => service.GetLocationForSpanAsync(workspace, documentId, textSpan, allowInvalidSpan: false, cancellationToken);

        public static Task<bool> TryNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
            => service.TryNavigateToSpanAsync(workspace, documentId, textSpan, options, allowInvalidSpan: false, cancellationToken);

        public static Task<bool> TryNavigateToPositionAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.TryNavigateToPositionAsync(workspace, documentId, position, virtualSpace: 0, NavigationOptions.Default, cancellationToken);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        public static async Task<bool> TryNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(
                workspace, documentId, textSpan, allowInvalidSpan, cancellationToken).ConfigureAwait(false);
            return location != null &&
                await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        public static async Task<bool> TryNavigateToLineAndOffsetAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForLineAndOffsetAsync(
                workspace, documentId, lineNumber, offset, cancellationToken).ConfigureAwait(false);
            return location != null &&
                await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        public static async Task<bool> TryNavigateToPositionAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(
                workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false);
            return location != null &&
                await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
        }
    }
}
