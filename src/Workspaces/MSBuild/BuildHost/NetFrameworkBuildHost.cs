// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETFRAMEWORK

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Locator;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class NetFrameworkBuildHost : AbstractBuildHost
{
    public static (AbstractBuildHost, RpcServer) Create(BuildHostLogger logger, PipeStream pipeStream)
    {
        // In this case, we're just going to pick the highest VS install on the machine, in case the projects are using some newer
        // MSBuild features. Since we don't have something like a global.json we can't really know what the minimum version is.

        // TODO: we should also check that the managed tools are actually installed
        var instance = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(vs => vs.Version).FirstOrDefault();

        if (instance is null)
        {
            logger.LogCritical("No compatible MSBuild instance could be found.");
            var server = new RpcServer(pipeStream);
            return (new NoNetFrameworkFoundBuildHost(logger, server), server);
        }

        // We need to create a build host, but in the .NET Framework case we need to ensure we have the proper binding redirects for MSBuild assemblies.
        // We achieve this by creating the build host in a new AppDomain whose base path is the MSBuild directory. This ensures MSBuild assemblies
        // are resolved from the correct location. We use CreateInstanceFromAndUnwrap to load our assembly by path, since it won't be in the
        // new AppDomain's base directory.
        var appDomainSetup = new AppDomainSetup
        {
            ApplicationBase = instance.MSBuildPath,
            ConfigurationFile = Path.Combine(instance.MSBuildPath, "MSBuild.exe.config"),
        };

        var appDomain = AppDomain.CreateDomain(
            friendlyName: nameof(NetFrameworkBuildHost),
            securityInfo: null,
            info: appDomainSetup);

        // Use CreateInstanceFromAndUnwrap which takes a file path to the assembly, since our assembly
        // won't be in the new AppDomain's ApplicationBase (which points to the MSBuild directory).
        var factory = (Factory)appDomain.CreateInstanceFromAndUnwrap(
            assemblyFile: (string?)typeof(Factory).Assembly.Location,
            typeName: typeof(Factory).FullName,
            ignoreCase: false,
            bindingAttr: default,
            binder: null,
            args: [],
            culture: null,
            activationAttributes: null);

        var rpcServer = new RpcServer(pipeStream, factory.CreateMethodInvoker());
        return (factory.CreateBuildHost(logger, rpcServer), rpcServer);
    }

    internal class Factory : MarshalByRefObject
    {
        public Factory()
        {
            // Before we can actualy create an object or pass things along, we need to ensure our assembly loading is correct, so hook AssemblyResolve here.
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                // Because we caled CreateInstanceFrom we would have been loaded in the LoadFrom context of our new AppDomain, but we still need to be present
                // in the regular Load context too. This ensures we wil be loaded when the rest of the code runs.
                if (e.Name == typeof(Factory).Assembly.FullName)
                    return typeof(Factory).Assembly;

                // Since our AppDomain's ApplicationBase is not our BuildHost directory, we need to direct our dependency to our folder
                // explicitly.
                var requestedAssemblyName = new AssemblyName(e.Name);
                if (requestedAssemblyName.Name == "Microsoft.CodeAnalysis.Workspaces.MSBuild.Contracts")
                {
                    var buildHostFolder = Path.GetDirectoryName(typeof(Factory).Assembly.Location);
                    return Assembly.LoadFrom(Path.Combine(buildHostFolder, $"{requestedAssemblyName.Name}.dll"));
                }

                return null;
            };
        }

#pragma warning disable CA1822 // Mark members as static - this is being called across AppDomains, so must be instance
        public RpcMethodInvoker CreateMethodInvoker() => new RpcMethodInvoker();
        public NetFrameworkBuildHost CreateBuildHost(BuildHostLogger logger, RpcServer server) => new NetFrameworkBuildHost(logger, server);
#pragma warning restore CA1822 // Mark members as static
    }

    private NetFrameworkBuildHost(BuildHostLogger logger, RpcServer server)
        : base(logger, server)
    {
    }

    protected override MSBuildLocation? FindMSBuild(string projectOrSolutionFilePath, bool includeUnloadableInstances)
    {
        throw new System.NotImplementedException();
    }

    protected override bool IsMSBuildLoaded()
    {
        return true;
    }

    private class NoNetFrameworkFoundBuildHost(BuildHostLogger logger, RpcServer server) : AbstractBuildHost(logger, server)
    {
        protected override MSBuildLocation? FindMSBuild(string projectOrSolutionFilePath, bool includeUnloadableInstances)
        {
            // We couldn't find one, so just return null so the rest of the system can handle the failure.
            return null;
        }

        protected override bool IsMSBuildLoaded()
        {
            return false;
        }
    }
}

#endif
