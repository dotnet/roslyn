// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.CodeAnalysis.Remote.Services
{
    [Obsolete("https://github.com/dotnet/roslyn/issues/43477")]
    [ExportWorkspaceService(typeof(IExperimentationService), ServiceLayer.Host), Shared]
    internal sealed class RemoteExperimentationService : IExperimentationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteExperimentationService()
        {
        }

        public bool IsExperimentEnabled(string experimentName)
        {
            // may return null in tests
            var assetSource = RemoteWorkspaceManager.Default.TryGetAssetSource();
            if (assetSource is null)
                return false;

            var isEnabledValueTask = assetSource.IsExperimentEnabledAsync(experimentName, CancellationToken.None);
            if (isEnabledValueTask.IsCompleted)
                return isEnabledValueTask.Result;

            isEnabledValueTask.Forget();
            return false;
        }

        public void EnableExperiment(string experimentName, bool value)
        {
            // This should never be called out of proc
            throw new NotImplementedException();
        }
    }
}
