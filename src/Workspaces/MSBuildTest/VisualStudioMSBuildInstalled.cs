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
        private static bool s_hasChecked;
        private static bool s_isRegistered;

        public override bool ShouldSkip
        {
            get
            {
                if (!s_hasChecked)
                {
                    var msbuildToolsPath = FindMSBuildToolsPathFromVisualStudio();

                    if (msbuildToolsPath != null)
                    {
                        RegisterMSBuildAssemblyResolution(msbuildToolsPath);
                        s_isRegistered = true;
                    }

                    s_hasChecked = true;
                }

                return !s_isRegistered;
            }
        }

        public override string SkipReason => "Could not locate Visual Studio with MSBuild installed";

        private static string FindMSBuildToolsPathFromVisualStudio()
        {
            // Only on Windows
            if (Path.DirectorySeparatorChar != '\\')
            {
                return null;
            }

            try
            {
                var configuration = Interop.GetSetupConfiguration();
                if (configuration == null)
                {
                    return null;
                }

                var instanceEnum = configuration.EnumAllInstances();

                int fetched;
                var instances = new ISetupInstance[1];
                do
                {
                    instanceEnum.Next(1, instances, out fetched);
                    if (fetched <= 0)
                    {
                        return null;
                    }

                    var instance2 = (ISetupInstance2)instances[0];
                    var state = instance2.GetState();

                    if (state == InstanceState.Complete &&
                        instance2.GetPackages().Any(package => package.GetId() == "Microsoft.VisualStudio.Component.Roslyn.Compiler"))
                    {
                        var toolsBasePath = Path.Combine(instance2.GetInstallationPath(), "MSBuild");

                        // Visual Studio 2019 and later place MSBuild in a "Current" folder.
                        var toolsPath = Path.Combine(toolsBasePath, "Current", "Bin");
                        if (Directory.Exists(toolsPath))
                        {
                            return toolsPath;
                        }

                        // Check for 15.0 to support Visual Studio 2017.
                        toolsPath = Path.Combine(toolsBasePath, "15.0", "Bin");
                        if (Directory.Exists(toolsPath))
                        {
                            return toolsPath;
                        }

                        return null;
                    }
                }
                while (fetched > 0);

                return null;
            }
            catch (COMException)
            {
                return null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
        }

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
}
