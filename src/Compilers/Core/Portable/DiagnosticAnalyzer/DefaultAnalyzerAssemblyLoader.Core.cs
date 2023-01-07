// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private readonly AssemblyLoadContext _compilerLoadContext;
        private readonly object _guard = new object();
        private readonly Dictionary<string, DirectoryLoadContext> _loadContextByDirectory = new Dictionary<string, DirectoryLoadContext>(StringComparer.Ordinal);

        internal AssemblyLoadContext CompilerLoadContext => _compilerLoadContext;

        internal DefaultAnalyzerAssemblyLoader()
            : this(null)
        {
        }

        internal DefaultAnalyzerAssemblyLoader(AssemblyLoadContext? compilerLoadContext = null)
        {
            _compilerLoadContext = compilerLoadContext ?? AssemblyLoadContext.GetLoadContext(typeof(DefaultAnalyzerAssemblyLoader).GetTypeInfo().Assembly)!;
        }

        protected override Assembly? Load(AssemblyName assemblyName, string assemblyOriginalPath)
        {
            DirectoryLoadContext? loadContext;

            var fullDirectoryPath = Path.GetDirectoryName(assemblyOriginalPath) ?? throw new ArgumentException(message: null, paramName: nameof(assemblyOriginalPath));
            lock (_guard)
            {
                if (!_loadContextByDirectory.TryGetValue(fullDirectoryPath, out loadContext))
                {
                    loadContext = new DirectoryLoadContext(fullDirectoryPath, this, _compilerLoadContext);
                    _loadContextByDirectory[fullDirectoryPath] = loadContext;
                }
            }

            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        internal static class TestAccessor
        {
            public static AssemblyLoadContext[] GetOrderedLoadContexts(DefaultAnalyzerAssemblyLoader loader)
            {
                lock (loader._guard)
                {
                    return loader._loadContextByDirectory.Values.OrderBy(v => v.Directory).ToArray();
                }
            }
        }

        private sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            internal string Directory { get; }
            private readonly DefaultAnalyzerAssemblyLoader _loader;
            private readonly AssemblyLoadContext _compilerLoadContext;

            public DirectoryLoadContext(string directory, DefaultAnalyzerAssemblyLoader loader, AssemblyLoadContext compilerLoadContext)
            {
                Directory = directory;
                _loader = loader;
                _compilerLoadContext = compilerLoadContext;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var simpleName = assemblyName.Name!;
                try
                {
                    if (_compilerLoadContext.LoadFromAssemblyName(assemblyName) is { } compilerAssembly)
                    {
                        return compilerAssembly;
                    }
                }
                catch
                {
                    // Expected to happen when the assembly cannot be resolved in the compiler / host
                    // AssemblyLoadContext. 
                }

                // Prefer registered dependencies in the same directory first.
                var assemblyPath = Path.Combine(Directory, simpleName + ".dll");
                if (_loader.IsAnalyzerDependencyPath(assemblyPath))
                {
                    var loadPath = _loader.PreparePathToLoad(assemblyPath);
                    return LoadFromAssemblyPath(loadPath);
                }

                // Next prefer dependencies that other analyzers registered. Ideally this would not
                // be necessary but msbuild target defaults have caused a number of customers to 
                // fall into this path. See discussion here for where it comes up
                // https://github.com/dotnet/roslyn/issues/56442
                if (_loader.GetBestPath(assemblyName) is string otherPath)
                {
                    var loadPath = _loader.PreparePathToLoad(otherPath);
                    return LoadFromAssemblyPath(loadPath);
                }

                // No analyzer registered this dependency. Time to fail
                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var assemblyPath = Path.Combine(Directory, unmanagedDllName + ".dll");
                if (_loader.IsAnalyzerDependencyPath(assemblyPath))
                {
                    var loadPath = _loader.PreparePathToLoad(assemblyPath);
                    return LoadUnmanagedDllFromPath(loadPath);
                }

                return IntPtr.Zero;
            }
        }
    }
}

#endif
