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
        /// <remarks>Only legal to call on the UI thread.</remarks>
        bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given position in the specified document.
        /// </summary>
        /// <remarks>Legal to call from any thread.</remarks>
        Task<bool> CanNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        /// <remarks>Only legal to call on the UI thread.</remarks>
        bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        /// <remarks>Only legal to call on the UI thread.</remarks>
        bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        Task<bool> TryNavigateToSpanAsync(Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, bool allowInvalidSpan, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        /// <remarks>Only legal to call on the UI thread.</remarks>
        bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, NavigationOptions options, CancellationToken cancellationToken);

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace, NavigationOptions options, CancellationToken cancellationToken);
    }

    internal static class IDocumentNavigationServiceExtensions
    {
        /// <remarks>Only legal to call on the UI thread.</remarks>
        public static bool CanNavigateToPosition(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.CanNavigateToPosition(workspace, documentId, position, virtualSpace: 0, cancellationToken);

        /// <remarks>Only legal to call on the UI thread.</remarks>
        public static bool TryNavigateToSpan(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, CancellationToken cancellationToken)
            => service.TryNavigateToSpan(workspace, documentId, textSpan, NavigationOptions.Default, cancellationToken);

        /// <remarks>Only legal to call on the UI thread.</remarks>
        public static bool TryNavigateToSpan(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
            => service.TryNavigateToSpan(workspace, documentId, textSpan, options, allowInvalidSpan: false, cancellationToken);

        /// <remarks>Legal to call from any thread.</remarks>
        public static Task<bool> TryNavigateToSpanAsync(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, TextSpan textSpan, NavigationOptions options, CancellationToken cancellationToken)
            => service.TryNavigateToSpanAsync(workspace, documentId, textSpan, options, allowInvalidSpan: false, cancellationToken);

        /// <remarks>Only legal to call on the UI thread.</remarks>
        public static bool TryNavigateToLineAndOffset(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int lineNumber, int offset, CancellationToken cancellationToken)
            => service.TryNavigateToLineAndOffset(workspace, documentId, lineNumber, offset, NavigationOptions.Default, cancellationToken);

        /// <remarks>Only legal to call on the UI thread.</remarks>
        public static bool TryNavigateToPosition(this IDocumentNavigationService service, Workspace workspace, DocumentId documentId, int position, CancellationToken cancellationToken)
            => service.TryNavigateToPosition(workspace, documentId, position, virtualSpace: 0, NavigationOptions.Default, cancellationToken);
    }
}
