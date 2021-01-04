﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IRemoteEditAndContinueService
    {
        internal interface ICallback
        {
            ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask<ManagedEditAndContinueAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);
            ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken);

            ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
            ValueTask<ImmutableArray<TextSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        }

        ValueTask<ImmutableArray<DiagnosticData>> GetDocumentDiagnosticsAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);
        ValueTask<bool> HasChangesAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, string? sourceFilePath, CancellationToken cancellationToken);

        ValueTask<(ManagedModuleUpdates Updates, ImmutableArray<DiagnosticData> Diagnostics)> EmitSolutionUpdateAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);

        ValueTask CommitSolutionUpdateAsync(CancellationToken cancellationToken);
        ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken);

        ValueTask StartDebuggingSessionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> StartEditSessionAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> EndEditSessionAsync(CancellationToken cancellationToken);
        ValueTask<ImmutableArray<DocumentId>> EndDebuggingSessionAsync(CancellationToken cancellationToken);

        ValueTask<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);
        ValueTask<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetAdjustedActiveStatementSpansAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, DocumentId documentId, CancellationToken cancellationToken);

        ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(PinnedSolutionInfo solutionInfo, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(PinnedSolutionInfo solutionInfo, RemoteServiceCallbackId callbackId, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask OnSourceFileUpdatedAsync(PinnedSolutionInfo solutionInfo, DocumentId documentId, CancellationToken cancellationToken);
    }
}
