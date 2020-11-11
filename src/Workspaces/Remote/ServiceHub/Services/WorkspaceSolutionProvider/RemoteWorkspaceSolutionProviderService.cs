// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteWorkspaceSolutionProviderService : BrokeredServiceBase, IRemoteWorkspaceSolutionProviderService
    {
        internal sealed class Factory : FactoryBase<IRemoteWorkspaceSolutionProviderService>
        {
            protected override IRemoteWorkspaceSolutionProviderService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteWorkspaceSolutionProviderService(arguments);
        }

        public const string ServiceName = ServiceDescriptors.ServiceNameTopLevelPrefix + ServiceDescriptors.ServiceNameComponentLevelPrefix + "WorkspaceSolutionProvider";

        internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceName, ServiceDescriptors.GetFeatureDisplayName);

        public RemoteWorkspaceSolutionProviderService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public new async ValueTask<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
            => await base.GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

        public ValueTask<Workspace> GetWorkspaceAsync(string workspaceKind, CancellationToken cancellationToken)
        {
            // other workspace kinds are currently not supported:
            Contract.ThrowIfFalse(workspaceKind == WorkspaceKind.RemoteWorkspace);
            return new(GetWorkspace());
        }
    }
}
