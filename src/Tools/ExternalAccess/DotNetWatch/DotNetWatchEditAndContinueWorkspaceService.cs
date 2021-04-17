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
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal sealed class DotNetWatchEditAndContinueWorkspaceService : IWorkspaceService
    {
        [ExportWorkspaceServiceFactory(typeof(DotNetWatchEditAndContinueWorkspaceService)), Shared]
        private sealed class Factory : IWorkspaceServiceFactory
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public Factory()
            {
            }

            [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
            public IWorkspaceService? CreateService(HostWorkspaceServices workspaceServices)
            {
                return new DotNetWatchEditAndContinueWorkspaceService(workspaceServices.GetRequiredService<IEditAndContinueWorkspaceService>());
            }
        }

        private readonly ActiveStatementSpanProvider _nullSolutionActiveStatementSpanProvider = (_, _, _) => new(ImmutableArray<ActiveStatementSpan>.Empty);
        private readonly IEditAndContinueWorkspaceService _workspaceService;

        public DotNetWatchEditAndContinueWorkspaceService(IEditAndContinueWorkspaceService workspaceService)
        {
            _workspaceService = workspaceService;
        }

        public ValueTask OnSourceFileUpdatedAsync(Document document, CancellationToken cancellationToken)
            => _workspaceService.OnSourceFileUpdatedAsync(document, cancellationToken);

        public void CommitSolutionUpdate() => _workspaceService.CommitSolutionUpdate(out _);

        public void DiscardSolutionUpdate() => _workspaceService.DiscardSolutionUpdate();

        public void EndDebuggingSession() => _workspaceService.EndDebuggingSession(out _);

        public void StartDebuggingSession(Solution solution)
            => _workspaceService.StartDebuggingSessionAsync(solution, StubManagedEditAndContinueDebuggerService.Instance, captureMatchingDocuments: false, CancellationToken.None).GetAwaiter().GetResult();

        public void StartEditSession() { }

        public void EndEditSession() { }

        public async ValueTask<DotNetWatchManagedModuleUpdatesWrapper> EmitSolutionUpdateAsync(Solution solution, CancellationToken cancellationToken)
        {
            var results = await _workspaceService.EmitSolutionUpdateAsync(solution, _nullSolutionActiveStatementSpanProvider, cancellationToken).ConfigureAwait(false);

            return new DotNetWatchManagedModuleUpdatesWrapper(in results.ModuleUpdates);
        }
    }
}
