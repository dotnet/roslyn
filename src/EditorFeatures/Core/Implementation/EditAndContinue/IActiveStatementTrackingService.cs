// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal interface IActiveStatementTrackingService : IWorkspaceService
    {
        ValueTask StartTrackingAsync(Solution solution, CancellationToken cancellationToken);

        void EndTracking();

        /// <summary>
        /// Triggered when tracking spans have changed.
        /// </summary>
        event Action TrackingChanged;

        /// <summary>
        /// Returns location of the tracking spans in the specified document snapshot (#line target document).
        /// </summary>
        /// <returns>Empty array if tracking spans are not available for the document.</returns>
        ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(Solution solution, DocumentId? documentId, string filePath, CancellationToken cancellationToken);

        /// <summary>
        /// Updates tracking spans with the latest positions of all active statements in the specified document snapshot (#line target document) and returns them.
        /// </summary>
        ValueTask<ImmutableArray<ActiveStatementTrackingSpan>> GetAdjustedTrackingSpansAsync(TextDocument document, ITextSnapshot snapshot, CancellationToken cancellationToken);
    }
}
