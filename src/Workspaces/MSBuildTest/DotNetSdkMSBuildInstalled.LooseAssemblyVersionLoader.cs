// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.MSBuild.UnitTests
{
    internal partial class DotNetSdkMSBuildInstalled
    {
#if NETCOREAPP

        private static class LooseVersionAssemblyLoader
        {
            private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new(StringComparer.OrdinalIgnoreCase);
            private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new();

            private static readonly object s_guard = new();
            private static readonly string[] s_extensions = new[] { "ni.dll", "ni.exe", "dll", "exe" };

            /// <summary>
            /// Register an assembly loader that will load assemblies with higher version than what was requested.
            /// </summary>
            public static void Register(string searchPath)
            {
                AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
                {
                    lock (s_guard)
                    {
                        if (s_namesToAssemblies.TryGetValue(assemblyName.FullName, out var cachedAssembly))
                        {
                            return cachedAssembly;
                        }

                        var assembly = TryResolveAssemblyFromPaths(context, assemblyName, searchPath);

                        // Cache assembly
                        if (assembly != null)
                        {
                            var name = assembly.FullName;
                            if (name is null)
                            {
                                throw new Exception($"Could not get name for assembly '{assembly}'");
                            }

                            s_pathsToAssemblies[assembly.Location] = assembly;
                            s_namesToAssemblies[name] = assembly;
                        }

                        return assembly;
                    }
                };
            }

            private static Assembly? TryResolveAssemblyFromPaths(AssemblyLoadContext context, AssemblyName assemblyName, string searchPath)
            {
                foreach (var cultureSubfolder in string.IsNullOrEmpty(assemblyName.CultureName)
                    // If no culture is specified, attempt to load directly from
                    // the known dependency paths.
                    ? new[] { string.Empty }
                    // Search for satellite assemblies in culture subdirectories
                    // of the assembly search directories, but fall back to the
                    // bare search directory if that fails.
                    : new[] { assemblyName.CultureName, string.Empty })
                {
                    foreach (var extension in s_extensions)
                    {
                        var candidatePath = Path.Combine(
                            searchPath, cultureSubfolder, $"{assemblyName.Name}.{extension}");

                        var isAssemblyLoaded = s_pathsToAssemblies.ContainsKey(candidatePath) == true;
                        if (isAssemblyLoaded || !File.Exists(candidatePath))
                        {
                            continue;
                        }

                        var candidateAssemblyName = AssemblyLoadContext.GetAssemblyName(candidatePath);
                        if (candidateAssemblyName.Version < assemblyName.Version)
                        {
                            continue;
                        }

                        try
                        {
                            var assembly = context.LoadFromAssemblyPath(candidatePath);

                            return assembly;
                        }
                        catch
                        {
                            if (assemblyName.Name != null)
                            {
                                // We were unable to load the assembly from the file path. It is likely that
                                // a different version of the assembly has already been loaded into the context.
                                // Be forgiving and attempt to load assembly by name without specifying a version.
                                return context.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
                            }
                        }
                    }
                }

                return null;
            }
        }

#endif
    }
}
