// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService, IActiveStatementSpanProvider
    {
        Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);
        Task<bool> HasChangesAsync(Solution solution, string? sourceFilePath, CancellationToken cancellationToken);
        Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken);

        void CommitSolutionUpdate();
        void DiscardSolutionUpdate();

        bool IsDebuggingSessionInProgress { get; }
        void OnSourceFileUpdated(DocumentId documentId);

        void StartDebuggingSession(Solution solution);
        void StartEditSession(ActiveStatementProvider activeStatementsProvider, IDebuggeeModuleMetadataProvider debuggeeModuleMetadataProvider);
        void EndEditSession();
        void EndDebuggingSession();

        Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken);
        Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveInstructionId instructionId, CancellationToken cancellationToken);

        void ReportApplyChangesException(Solution solution, string message);
    }
}
