// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Tagging;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteCompilationAvailableService : BrokeredServiceBase, IRemoteCompilationAvailableService
    {
        internal sealed class Factory : FactoryBase<IRemoteCompilationAvailableService>
        {
            protected override IRemoteCompilationAvailableService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteCompilationAvailableService(arguments);
        }

        public RemoteCompilationAvailableService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask ComputeCompilationAsync(
            PinnedSolutionInfo solutionInfo,
            ProjectId projectId,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                var project = solution.GetRequiredProject(projectId);

                await CompilationAvailableHelpers.ComputeCompilationInCurrentProcessAsync(project, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
