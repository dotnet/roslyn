// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteTaggerCompilationAvailableService : BrokeredServiceBase, IRemoteTaggerCompilationAvailableService
    {
        internal sealed class Factory : FactoryBase<IRemoteTaggerCompilationAvailableService>
        {
            protected override IRemoteTaggerCompilationAvailableService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteTaggerCompilationAvailableService(arguments);
        }

        public RemoteTaggerCompilationAvailableService(in ServiceConstructionArguments arguments)
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

                await CompilationAvailableTaggerEventSource.ComputeCompilationInCurrentProcessAsync(project, cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }
    }
}
