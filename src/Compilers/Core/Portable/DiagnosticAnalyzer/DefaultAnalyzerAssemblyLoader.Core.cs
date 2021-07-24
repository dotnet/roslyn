// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private readonly object _guard = new object();
        private readonly Dictionary<string, AssemblyLoadContext> _loadContextByDirectory = new Dictionary<string, AssemblyLoadContext>();

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            AssemblyLoadContext? loadContext;

            var fullDirectoryPath = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException();
            lock (_guard)
            {
                if (!_loadContextByDirectory.TryGetValue(fullDirectoryPath, out loadContext))
                {
                    loadContext = new DirectoryLoadContext(fullDirectoryPath);
                    loadContext.Resolving += (context, name) =>
                    {
                        Debug.Assert(ReferenceEquals(context, loadContext));
                        return Load(name.FullName);
                    };
                    _loadContextByDirectory[fullDirectoryPath] = loadContext;
                }
            }

            return loadContext.LoadFromAssemblyPath(fullPath);
        }

        private class DirectoryLoadContext : AssemblyLoadContext
        {
            private readonly string _directory;
            public DirectoryLoadContext(string fullDirectoryPath)
            {
                _directory = fullDirectoryPath;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                // When we want to provide an analyzer with a possibly shared dependency, such as the compiler assemblies,
                // we want to first search the assembly load context that this class is loaded into.
                // This method of obtaining the context tries to account for the possibility that
                // multiple versions of the compiler assemblies themselves could be hosted in different assembly load contexts in a single process.
                var sharedContext = AssemblyLoadContext.GetLoadContext(typeof(DirectoryLoadContext).Assembly);

                var alreadyLoadedAssembly = sharedContext?.Assemblies.FirstOrDefault(
                    assembly => AssemblyName.ReferenceMatchesDefinition(assemblyName, assembly.GetName()));
                if (alreadyLoadedAssembly is not null)
                {
                    return alreadyLoadedAssembly;
                }

                var simpleName = assemblyName.Name;
                var assemblyPath = Path.Combine(_directory, assemblyName.Name + ".dll");
                try
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var assemblyPath = Path.Combine(_directory, unmanagedDllName + ".dll");
                try
                {
                    return LoadUnmanagedDllFromPath(assemblyPath);
                }
                catch (DllNotFoundException)
                {
                    return IntPtr.Zero;
                }
            }
        }
    }
}

#endif
