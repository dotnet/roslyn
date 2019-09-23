// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService
    {
        Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);
        Task<SolutionUpdateStatus> GetSolutionUpdateStatusAsync(string sourceFilePath, CancellationToken cancellationToken);
        Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(CancellationToken cancellationToken);

        void CommitSolutionUpdate();
        void DiscardSolutionUpdate();

        void OnManagedModuleInstanceLoaded(Guid mvid);
        void OnManagedModuleInstanceUnloaded(Guid mvid);

        bool IsDebuggingSessionInProgress { get; }

        void StartDebuggingSession();
        void StartEditSession();
        void EndEditSession();
        void EndDebuggingSession();

        Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken);
        Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken);

        void ReportApplyChangesException(string message);
    }
}
