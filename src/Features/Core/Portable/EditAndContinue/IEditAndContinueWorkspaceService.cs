// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal interface IEditAndContinueWorkspaceService : IWorkspaceService, IActiveStatementSpanProvider
    {
        ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);
        ValueTask<bool> HasChangesAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, string? sourceFilePath, CancellationToken cancellationToken);
        ValueTask<EmitSolutionUpdateResults> EmitSolutionUpdateAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken);

        void CommitSolutionUpdate(out ImmutableArray<DocumentId> documentsToReanalyze);
        void DiscardSolutionUpdate();

        void OnSourceFileUpdated(Document document);

        ValueTask StartDebuggingSessionAsync(Solution solution, IManagedEditAndContinueDebuggerService debuggerService, bool captureMatchingDocuments, CancellationToken cancellationToken);
        void BreakStateEntered(out ImmutableArray<DocumentId> documentsToReanalyze);
        void EndDebuggingSession(out ImmutableArray<DocumentId> documentsToReanalyze);

        ValueTask<bool?> IsActiveStatementInExceptionRegionAsync(Solution solution, ManagedInstructionId instructionId, CancellationToken cancellationToken);
        ValueTask<LinePositionSpan?> GetCurrentActiveStatementPositionAsync(Solution solution, ActiveStatementSpanProvider activeStatementSpanProvider, ManagedInstructionId instructionId, CancellationToken cancellationToken);
    }
}
