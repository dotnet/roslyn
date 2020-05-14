// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        bool CanNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan);

        /// <summary>
        /// Determines whether it is possible to navigate to the given line/offset in the specified document.
        /// </summary>
        bool CanNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset);

        /// <summary>
        /// Determines whether it is possible to navigate to the given virtual position in the specified document.
        /// </summary>
        bool CanNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0);

        /// <summary>
        /// Navigates to the given position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToSpan(Workspace workspace, DocumentId documentId, TextSpan textSpan, OptionSet options = null);

        /// <summary>
        /// Navigates to the given line/offset in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToLineAndOffset(Workspace workspace, DocumentId documentId, int lineNumber, int offset, OptionSet options = null);

        /// <summary>
        /// Navigates to the given virtual position in the specified document, opening it if necessary.
        /// </summary>
        bool TryNavigateToPosition(Workspace workspace, DocumentId documentId, int position, int virtualSpace = 0, OptionSet options = null);
    }
}
