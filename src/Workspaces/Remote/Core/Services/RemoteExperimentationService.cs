// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
