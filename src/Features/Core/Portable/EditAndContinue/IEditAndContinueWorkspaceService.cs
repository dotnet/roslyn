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
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService
    {
        /// <summary>
        /// Returns base active statement spans contained in each specified document.
        /// </summary>
        /// <returns>
        /// <see langword="default"/> if called outside of an edit session.
        /// The length of the returned array matches the length of <paramref name="documentIds"/> otherwise.
        /// </returns>
        Task<ImmutableArray<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>>> GetBaseActiveStatementSpansAsync(ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the active statements in the specified <paramref name="document"/> snapshot.
        /// </summary>
        /// <returns>
        /// <see langword="default"/> if called outside of an edit session, or active statements for the document can't be determined for some reason
        /// (e.g. the document has syntax errors or is out-of-sync).
        /// </returns>
        Task<ImmutableArray<(LinePositionSpan, ActiveStatementFlags)>> GetDocumentActiveStatementSpansAsync(Document document, CancellationToken cancellationToken);

        Task<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);
        Task<bool> HasChangesAsync(Solution solution, string? sourceFilePath, CancellationToken cancellationToken);
        Task<(SolutionUpdateStatus Summary, ImmutableArray<Deltas> Deltas)> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken);

        void CommitSolutionUpdate();
        void DiscardSolutionUpdate();

        bool IsDebuggingSessionInProgress { get; }
        void OnSourceFileUpdated(DocumentId documentId);

        void StartDebuggingSession(Solution solution);
        void StartEditSession(ActiveStatementProvider activeStatementsProvider);
        void EndEditSession();
        void EndDebuggingSession();

        Task<bool?> IsActiveStatementInExceptionRegionAsync(ActiveInstructionId instructionId, CancellationToken cancellationToken);
        Task<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveInstructionId instructionId, CancellationToken cancellationToken);

        void ReportApplyChangesException(Solution solution, string message);
    }
}
