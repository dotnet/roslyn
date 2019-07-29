// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal class VisualStudioMSBuildInstalled : ExecutionCondition
    {
        private static bool s_isRegistered;

        private readonly Version _minimumVersion;

        public VisualStudioMSBuildInstalled() : this(new Version(15, 0))
        {

        }
        protected VisualStudioMSBuildInstalled(Version minimumVersion)
        {
            _minimumVersion = minimumVersion;
        }

        public override bool ShouldSkip
        {
            get
            {
                if (VisualStudioMSBuildLocator.TryFindMSBuildToolsPath(out var versionAndPath))
                {
                    if (versionAndPath.version < _minimumVersion)
                    {
                        return true;
                    }

                    if (!s_isRegistered)
                    {
                        RegisterMSBuildAssemblyResolution(versionAndPath.path);

                        s_isRegistered = true;
                    }

                    return false;
                }

                return true;
            }
        }

        public override string SkipReason => $"Could not locate Visual Studio with MSBuild {_minimumVersion} or higher installed";

        private static void RegisterMSBuildAssemblyResolution(string msbuildToolsPath)
        {
            if (s_isRegistered)
            {
                throw new InvalidOperationException("Attempted to register twice!");
            }

            var assemblyNames = new[]
            {
                "Microsoft.Build",
                "Microsoft.Build.Framework",
                "Microsoft.Build.Tasks.Core",
                "Microsoft.Build.Utilities.Core"
            };

            var builder = ImmutableDictionary.CreateBuilder<string, Assembly>(StringComparer.OrdinalIgnoreCase);

            foreach (var assemblyName in assemblyNames)
            {
                var assemblyFilePath = Path.Combine(msbuildToolsPath, assemblyName + ".dll");
                var assembly = File.Exists(assemblyFilePath)
                    ? Assembly.LoadFrom(assemblyFilePath)
                    : null;

                if (assembly != null)
                {
                    builder.Add(assemblyName, assembly);
                }
            }

            var assemblyMap = builder.ToImmutable();

            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                var assemblyName = new AssemblyName(e.Name);

                if (assemblyMap.TryGetValue(assemblyName.Name, out var assembly))
                {
                    return assembly;
                }

                return null;
            };
        }
    }

    internal class VisualStudio16_2OrHigherMSBuildInstalled : VisualStudioMSBuildInstalled
    {
        public VisualStudio16_2OrHigherMSBuildInstalled() : base(new Version(16, 2))
        {
        }
    }

    internal static class VisualStudioMSBuildLocator
    {
        private static Lazy<(Version version, string path)> s_versionAndPath = new Lazy<(Version version, string path)>(FindMSBuildToolsPathFromVisualStudioCore);

        public static bool TryFindMSBuildToolsPath(out (Version version, string path) versionAndPath)
        {
            versionAndPath = s_versionAndPath.Value;
            return versionAndPath.path != null;
        }

        private static (Version version, string path) FindMSBuildToolsPathFromVisualStudioCore()
        {
            // Only on Windows
            if (Path.DirectorySeparatorChar != '\\')
            {
                return (null, null);
            }

            try
            {
                var configuration = Interop.GetSetupConfiguration();
                if (configuration == null)
                {
                    return (null, null);
                }

                var instanceEnum = configuration.EnumAllInstances();
                var instances = new ISetupInstance[1];

                (Version version, string path) found = (null, null);

                while (true)
                {
                    instanceEnum.Next(1, instances, out var fetched);
                    if (fetched <= 0)
                    {
                        break;
                    }

                    var instance2 = (ISetupInstance2)instances[0];
                    var state = instance2.GetState();
                    if (state == InstanceState.Complete &&
                        instance2.GetPackages().Any(package => package.GetId() == "Microsoft.VisualStudio.Component.Roslyn.Compiler"))
                    {
                        var instanceVersionString = instance2.GetInstallationVersion();

                        if (!Version.TryParse(instanceVersionString, out var instanceVersion))
                        {
                            // We'll throw an exception here -- this means we have some build with a new style of version numbers, which is probably the high version we want to pick but
                            // we won't know it
                            throw new Exception($"Unable to parse version string '{instanceVersionString}'");
                        }

                        var toolsBasePath = Path.Combine(instance2.GetInstallationPath(), "MSBuild");
                        string instanceMsBuildPath = null;

                        // Visual Studio 2019 and later place MSBuild in a "Current" folder.
                        var toolsPath = Path.Combine(toolsBasePath, "Current", "Bin");
                        if (Directory.Exists(toolsPath))
                        {
                            instanceMsBuildPath = toolsPath;
                        }
                        else
                        {
                            // Check for 15.0 to support Visual Studio 2017. We have this in an else block because in 2019 there's also this folder for compat reasons
                            toolsPath = Path.Combine(toolsBasePath, "15.0", "Bin");
                            if (Directory.Exists(toolsPath))
                            {
                                instanceMsBuildPath = toolsPath;
                            }
                        }

                        // We found some version; we will always use the highest possible version because we want to support the running of the most tests
                        // possible -- we can't load multiple versions sanely unless we tried multiple AppDomains
                        if (instanceMsBuildPath != null && (found.version == null || instanceVersion > found.version))
                        {
                            found.version = instanceVersion;
                            found.path = instanceMsBuildPath;
                        }
                    }
                }

                return found;
            }
            catch (COMException)
            {
                return (null, null);
            }
            catch (DllNotFoundException)
            {
                return (null, null);
            }
        }
    }
}
