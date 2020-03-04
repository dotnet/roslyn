// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Services
{
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal sealed class RemoteExperimentationService : IExperimentationService
    {
        public bool IsExperimentEnabled(string experimentName)
        {
            var assetSource = AssetStorage.Default.AssetSource;
            return assetSource?.IsExperimentEnabledAsync(experimentName, CancellationToken.None).Result ?? false;
        }
    }
}
