// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK

using System.Linq;
using Microsoft.Build.Locator;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class MonoBuildHost : AbstractBuildHost
{
    public MonoBuildHost(BuildHostLogger logger, RpcServer server) : base(logger, server)
    {
    }

    protected override MSBuildLocation? FindMSBuild(string projectOrSolutionFilePath, bool includeUnloadableInstances)
    {
        // We're running on Mono, but not all Mono installations have a usable MSBuild installation, so let's see if we have one that we can use.
        var monoMSBuildDirectory = MonoMSBuildDiscovery.GetMonoMSBuildDirectory();
        if (monoMSBuildDirectory != null)
        {
            var monoMSBuildVersion = MonoMSBuildDiscovery.GetMonoMSBuildVersion();
            if (monoMSBuildVersion != null)
            {
                return new(monoMSBuildDirectory, monoMSBuildVersion);
            }
        }

        Logger.LogCritical("No Mono MSBuild installation could be found; see https://www.mono-project.com/ for installation instructions.");
        return null;
    }

    protected override bool IsMSBuildLoaded()
    {
        return MSBuildLocator.IsRegistered;
    }
}

#endif
