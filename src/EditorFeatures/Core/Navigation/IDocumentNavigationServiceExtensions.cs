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
        public static async Task<bool> NavigateToAsync(this INavigableLocation? location, IThreadingContext threadingContext, CancellationToken cancellationToken)
        {
            if (location == null)
                return false;

            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await location.NavigateToAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        private static Task<bool> SwitchToMainThreadAndNavigateAsync(IThreadingContext threadingContext, INavigableLocation? location, CancellationToken cancellationToken)
        {
            return location.NavigateToAsync(threadingContext, cancellationToken);
        }

        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, options, allowInvalidSpan, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, options, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToSpanAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToPositionAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(workspace, documentId, position, virtualSpace, options, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToPositionAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(
                workspace, documentId, position, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<bool> TryNavigateToLineAndOffsetAsync(
            this IDocumentNavigationService service, IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForLineAndOffsetAsync(
                workspace, documentId, lineNumber, offset, options, cancellationToken).ConfigureAwait(false);
            return await SwitchToMainThreadAndNavigateAsync(threadingContext, location, cancellationToken).ConfigureAwait(false);
        }
    }
}
