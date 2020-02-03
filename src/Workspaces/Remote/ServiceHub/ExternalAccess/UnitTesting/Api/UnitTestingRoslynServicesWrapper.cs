// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRoslynServicesWrapper
    {
        internal RoslynServices UnderlyingObject { get; }

        public UnitTestingRoslynServicesWrapper(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfoWrapper, UnitTestingAssetStorageWrappper assetStorageWrappper)
            => UnderlyingObject = new RoslynServices(pinnedSolutionInfoWrapper.UnderlyingObject.ScopeId, assetStorageWrappper.UnderlyingObject, RoslynServices.HostServices);

        public Task<Solution> GetSolutionAsync(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfoWrapper, CancellationToken cancellationToken)
            => UnderlyingObject.SolutionService.GetSolutionAsync(pinnedSolutionInfoWrapper.UnderlyingObject.SolutionChecksum, cancellationToken);
    }
}
