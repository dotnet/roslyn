// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IRemoteEditAndContinueService
    {
        internal interface IStartEditSessionCallback
        {
            Task<ImmutableArray<ActiveStatementDebugInfo.Data>> GetActiveStatementsAsync(CancellationToken cancellationToken);
            Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken);
            Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken);
        }

        Task<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, string? sourceFilePath, CancellationToken cancellationToken);
        Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas.Data> Deltas)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken);
        Task CommitUpdateAsync(CancellationToken cancellationToken);
        Task DiscardUpdatesAsync(CancellationToken cancellationToken);

        Task StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken);
        Task StartEditSessionAsync(CancellationToken cancellationToken);
        Task EndEditSessionAsync(CancellationToken cancellationToken);
        Task EndDebuggingSessionAsync(CancellationToken cancellationToken);

        Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
        Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken);

        Task<bool?> IsActiveStatementInExceptionRegionAsync(Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken);
        Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, Guid moduleId, int methodToken, int methodVersion, int ilOffset, CancellationToken cancellationToken);
    }
}
