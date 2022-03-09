// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation
{
    internal static class INavigableLocationExtensions
    {
        public static async Task<bool> TryNavigateToAsync(
            this INavigableLocation? location, IThreadingContext threadingContext, NavigationOptions options, CancellationToken cancellationToken)
        {
            if (location == null)
                return false;

            // This switch is currently unnecessary.  Howevver, it helps support a future where location.NavigateTo becomes
            // async and must be on the UI thread.
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, allowInvalidSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToPositionAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(workspace, documentId, position, virtualSpace, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToPositionAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(
                workspace, documentId, position, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToLineAndOffsetAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForLineAndOffsetAsync(
                workspace, documentId, lineNumber, offset, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
