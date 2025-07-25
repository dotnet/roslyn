// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation;

internal static class INavigableLocationExtensions
{
    extension(INavigableLocation? location)
    {
        public async Task<bool> TryNavigateToAsync(
IThreadingContext threadingContext, NavigationOptions options, CancellationToken cancellationToken)
        {
            if (location == null)
                return false;

            // This switch is currently unnecessary.  However, it helps support a future where location.NavigateTo becomes
            // async and must be on the UI thread.
            await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await location.NavigateToAsync(options, cancellationToken).ConfigureAwait(false);
        }
    }
}

internal static class IDocumentNavigationServiceExtensions
{
    extension(IDocumentNavigationService service)
    {
        public async Task<bool> TryNavigateToSpanAsync(
IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, allowInvalidSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryNavigateToSpanAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryNavigateToSpanAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForSpanAsync(workspace, documentId, textSpan, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, NavigationOptions.Default, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryNavigateToPositionAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, int virtualSpace, bool allowInvalidPosition, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(workspace, documentId, position, virtualSpace, allowInvalidPosition, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public Task<bool> TryNavigateToPositionAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
        {
            return service.TryNavigateToPositionAsync(threadingContext, workspace, documentId, position, NavigationOptions.Default, cancellationToken);
        }

        public async Task<bool> TryNavigateToPositionAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int position, NavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await service.GetLocationForPositionAsync(
                workspace, documentId, position, cancellationToken).ConfigureAwait(false);
            return await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryNavigateToLineAndOffsetAsync(
    IThreadingContext threadingContext, Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken)
        {
            // Navigation should not change the context of linked files and Shared Projects.
            documentId = workspace.GetDocumentIdInCurrentContext(documentId);

            var document = workspace.CurrentSolution.GetTextDocument(documentId);
            if (document is null)
                return false;

            // DocumentId+Line+Column come from sources that are not snapshot based.  In other words, the data may
            // correspond to some point in time in the past.  As such, we have to try to clamp it against the current
            // view of the document text.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            var linePosition = new LinePosition(lineNumber, offset);
            var linePositionSpan = new LinePositionSpan(linePosition, linePosition);
            var clampedSpan = linePositionSpan.GetClampedTextSpan(text);

            // This operation is fundamentally racey.  Between getting the clamped span and navigating the document may
            // have changed.  So allow for invalid spans here.
            var location = await service.GetLocationForSpanAsync(
                workspace, documentId, clampedSpan, allowInvalidSpan: true, cancellationToken).ConfigureAwait(false);

            return location != null && await location.TryNavigateToAsync(threadingContext, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
