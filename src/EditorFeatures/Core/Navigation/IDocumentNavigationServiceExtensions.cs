// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.Editor.Navigation
{
    internal static class IDocumentNavigationServiceExtensions
    {
        private static async Task<bool> SwitchToMainThreadAndNavigateAsync(IThreadingContext threadingContext, INavigableLocation? location, CancellationToken cancellationToken)
        {
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return location != null &&
                await location.NavigateToAsync(cancellationToken).ConfigureAwait(false);
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
    }
}
