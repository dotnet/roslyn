// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Projects
{
    /// <summary>
    /// Discovers project information for remote directories
    /// </summary>
    [Export(typeof(RemoteProjectInfoProvider))]
    internal class RemoteProjectInfoProvider
    {
        private readonly IEnumerable<IRemoteProjectInfoProvider> _remoteProjectInfoProviders;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteProjectInfoProvider([ImportMany] IEnumerable<IRemoteProjectInfoProvider> remoteProjectInfoProviders)
            => _remoteProjectInfoProviders = remoteProjectInfoProviders ?? throw new ArgumentNullException(nameof(remoteProjectInfoProviders));

        public async Task<IReadOnlyCollection<ProjectInfo>> GetRemoteProjectInfosAsync(CancellationToken cancellationToken)
        {
            var projectInfos = new List<ProjectInfo>();
            foreach (var remoteProjectInfoProvider in _remoteProjectInfoProviders)
            {
                try
                {
                    foreach (var projectInfo in await remoteProjectInfoProvider.GetRemoteProjectInfosAsync(cancellationToken).ConfigureAwait(false))
                    {
                        projectInfos.Add(projectInfo);
                    }
                }
                catch (Exception)
                {
                    // Continue with the other providers even if one of them fails. 
                    continue;
                }
            }

            return projectInfos;
        }
    }
}
