// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    [ExportWorkspaceService(typeof(DotNetWatchEditAndContinueWorkspaceService))]
    [Shared]
    internal sealed class DotNetWatchEditAndContinueWorkspaceService : IWorkspaceService
    {
        private readonly SolutionActiveStatementSpanProvider _nullSolutionActiveStatementSpanProvider = (_, _) => new(ImmutableArray<TextSpan>.Empty);
        private readonly IEditAndContinueWorkspaceService _workspaceService;

        [ImportingConstructor]
        [Obsolete("This exported object must be obtained through the MEF export provider.", error: true)]
        public DotNetWatchEditAndContinueWorkspaceService(IEditAndContinueWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public Task OnSourceFileUpdatedAsync(Document document, CancellationToken cancellationToken)
            => _workspaceService.OnSourceFileUpdatedAsync(document, cancellationToken);

        public void CommitSolutionUpdate() => _workspaceService.CommitSolutionUpdate();

        public void DiscardSolutionUpdate() => _workspaceService.DiscardSolutionUpdate();

        public void EndDebuggingSession() => _workspaceService.EndDebuggingSession(out _);

        public void StartDebuggingSession(Solution solution) => _workspaceService.StartDebuggingSession(solution);

        public void StartEditSession() => _workspaceService.StartEditSession(StubManagedEditAndContinueDebuggerService.Instance, out _);

        public void EndEditSession() => _workspaceService.EndEditSession(out _);

        public async ValueTask<DotNetWatchManagedModuleUpdates> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var (updates, _) = await _workspaceService.EmitSolutionUpdateAsync(solution, _nullSolutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            var forwardingUpdates = new DotNetWatchManagedModuleUpdates(
                (DotNetWatchManagedModuleUpdateStatus)updates.Status,
                ImmutableArray.CreateRange(updates.Updates, u => new DotNetWatchManagedModuleUpdate(u.Module, u.ILDelta, u.MetadataDelta, u.PdbDelta, u.UpdatedMethods)));

            return (forwardingUpdates);
        }
    }
}
