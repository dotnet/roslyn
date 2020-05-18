// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingRoslynServicesWrapper
    {
        private readonly SolutionService _solutionService;

        [Obsolete]
        public UnitTestingRoslynServicesWrapper(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfoWrapper, UnitTestingAssetStorageWrappper assetStorageWrappper)
            => _solutionService = new SolutionService(SolutionService.CreateAssetProvider(pinnedSolutionInfoWrapper.UnderlyingObject, assetStorageWrappper.UnderlyingObject));

        public UnitTestingRoslynServicesWrapper(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfoWrapper, UnitTestingAssetStorageWrapper assetStorageWrapper)
            => _solutionService = new SolutionService(SolutionService.CreateAssetProvider(pinnedSolutionInfoWrapper.UnderlyingObject, assetStorageWrapper.UnderlyingObject));

        public Task<Solution> GetSolutionAsync(UnitTestingPinnedSolutionInfoWrapper pinnedSolutionInfoWrapper, CancellationToken cancellationToken)
            => _solutionService.GetSolutionAsync(pinnedSolutionInfoWrapper.UnderlyingObject.SolutionChecksum, cancellationToken);
    }
}
