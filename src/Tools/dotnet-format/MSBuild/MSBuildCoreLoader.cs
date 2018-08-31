// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// LICENSING NOTE: The license for this file is from the originating 
// source and not the general https://github.com/dotnet/roslyn license.
// See https://github.com/Microsoft/MSBuildLocator/blob/6631a6dbf9be72b2426e260c99dc0f345e79b8e5/src/MSBuildLocator/MSBuildLocator.cs

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Microsoft.CodeAnalysis.Tools.MSBuild
{
    internal static class MSBuildCoreLoader
    {
        private const string MSBuildPublicKeyToken = "b03f5f7f11d50a3a";

        private static readonly string[] s_msBuildAssemblies =
        {
            "Microsoft.Build",
            "Microsoft.Build.Framework",
            "Microsoft.Build.Tasks.Core",
            "Microsoft.Build.Utilities.Core"
        };

        /// <summary>
        /// Loads the MSBuild assemblies from the DotNet CLI sdk path.
        /// </summary>
        public static void LoadDotnetInstance()
        {
            // Workaround for https://github.com/Microsoft/msbuild/issues/3352
            LoadMSBuildAssemblies(MSBuildEnvironment.GetDotnetBasePath());
        }

        /// <summary>
        /// Load MSBuild assemblies.
        /// </summary>
        /// <param name="msbuildPath">
        /// Path to the directory containing a deployment of MSBuild binaries.
        /// </param>
        public static void LoadMSBuildAssemblies(string msbuildPath)
        {
            var loadedMSBuildAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(IsMSBuildAssembly);
            if (loadedMSBuildAssemblies.Any())
            {
                var loadedAssemblyList = string.Join(Environment.NewLine, loadedMSBuildAssemblies.Select(a => a.GetName()));

                var error = $"{typeof(MSBuildCoreLoader)}.{nameof(LoadMSBuildAssemblies)} was called, but MSBuild assemblies were already loaded." + Environment.NewLine +
                    $"Ensure that {nameof(LoadDotnetInstance)} is called before any method that directly references types in the Microsoft.Build namespace has been called." +
                    Environment.NewLine + "Loaded MSBuild assemblies: " + loadedAssemblyList;

                throw new InvalidOperationException(error);
            }

            foreach (var msBuildAssembly in s_msBuildAssemblies)
            {
                var targetAssembly = Path.Combine(msbuildPath, msBuildAssembly + ".dll");
                Assembly.LoadFrom(targetAssembly);
            }
        }

        private static bool IsMSBuildAssembly(Assembly assembly)
        {
            return IsMSBuildAssembly(assembly.GetName());
        }

        private static bool IsMSBuildAssembly(AssemblyName assemblyName)
        {
            if (!s_msBuildAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            var publicKeyToken = assemblyName.GetPublicKeyToken();
            if (publicKeyToken == null || publicKeyToken.Length == 0)
            {
                return false;
            }

            var sb = new StringBuilder(capacity: MSBuildPublicKeyToken.Length);
            foreach (var b in publicKeyToken)
            {
                sb.Append($"{b:x2}");
            }

            return sb.ToString().Equals(MSBuildPublicKeyToken, StringComparison.OrdinalIgnoreCase);
        }
    }
}
