// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler
{
    internal interface IUnitTestingDocumentTrackingService : IWorkspaceService
    {
        bool SupportsDocumentTracking { get; }

        /// <summary>
        /// Get the <see cref="DocumentId"/> of the active document. May be null if there is no active document
        /// or the active document is not in the workspace.
        /// </summary>
        DocumentId? TryGetActiveDocument();

        /// <summary>
        /// Get a read only collection of the <see cref="DocumentId"/>s of all the visible documents in the workspace.
        /// </summary>
        ImmutableArray<DocumentId> GetVisibleDocuments();

        /// <summary>
        /// Raised when a text buffer that's not part of a workspace is changed.
        /// </summary>
        event EventHandler<EventArgs> NonRoslynBufferTextChanged;
    }
}
