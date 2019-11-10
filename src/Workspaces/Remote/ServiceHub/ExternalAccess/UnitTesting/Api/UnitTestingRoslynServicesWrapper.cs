// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
