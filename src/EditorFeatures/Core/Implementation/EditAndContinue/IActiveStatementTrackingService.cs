// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.EditAndContinue
{
    internal interface IActiveStatementTrackingService : IWorkspaceService
    {
        void StartTracking();
        void EndTracking();

        bool TryGetSpan(ActiveStatementId id, SourceText source, out TextSpan span);

        /// <summary>
        /// Updates tracking spans with the latest positions of all active statements in the specified document snapshot and returns them.
        /// </summary>
        Task<ImmutableArray<ActiveStatementTrackingSpan>> GetLatestSpansAsync(Document document, ITextSnapshot snapshot, CancellationToken cancellationToken);

        /// <summary>
        /// Triggered when tracking spans have changed.
        /// </summary>
        event Action TrackingChanged;
    }
}
