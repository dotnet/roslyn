// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETFRAMEWORK

using System.IO;
using System.Linq;
using Microsoft.Build.Locator;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class NetCoreBuildHost : AbstractBuildHost
{
    public NetCoreBuildHost(BuildHostLogger logger, RpcServer server) : base(logger, server)
    {
    }

    protected override MSBuildLocation? FindMSBuild(string projectOrSolutionFilePath, bool includeUnloadableInstances)
    {
        VisualStudioInstance? instance;

        // Locate the right SDK for this particular project; MSBuildLocator ensures in this case the first one is the preferred one.
        // The includeUnloadableInstance parameter additionally locates SDKs from all installations regardless of whether they are
        // loadable by the BuildHost process.
        var options = new VisualStudioInstanceQueryOptions
        {
            DiscoveryTypes = DiscoveryType.DotNetSdk,
            WorkingDirectory = Path.GetDirectoryName(projectOrSolutionFilePath),
            AllowAllDotnetLocations = includeUnloadableInstances,
            AllowAllRuntimeVersions = includeUnloadableInstances,
        };

        instance = MSBuildLocator.QueryVisualStudioInstances(options).FirstOrDefault();

        if (instance != null)
        {
            return new(instance.MSBuildPath, instance.Version.ToString());
        }

        Logger.LogCritical("No compatible MSBuild instance could be found.");

        return null;
    }

    protected override bool IsMSBuildLoaded()
    {
        return MSBuildLocator.IsRegistered;
    }
}

#endif
