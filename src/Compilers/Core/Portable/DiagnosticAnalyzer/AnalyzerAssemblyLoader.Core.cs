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
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal enum AnalyzerLoadOption
    {
        /// <summary>
        /// Once the assembly path is chosen, load it directly from disk at that location
        /// </summary>
        LoadFromDisk,

        /// <summary>
        /// Once the assembly path is chosen, read the contents of disk and load from memory
        /// </summary>
        /// <remarks>
        /// While Windows supports this option it comes with a significant performance penalty due
        /// to anti virus scans. It can have a load time of 300-500ms while loading from disk 
        /// is generally 1-2ms. Use this with caution on Windows.
        /// </remarks>
        LoadFromStream
    }

    internal partial class AnalyzerAssemblyLoader
    {
        private readonly AssemblyLoadContext _compilerLoadContext;
        private readonly Dictionary<string, DirectoryLoadContext> _loadContextByDirectory = new Dictionary<string, DirectoryLoadContext>(StringComparer.Ordinal);
        private readonly AnalyzerLoadOption _loadOption;

        internal AssemblyLoadContext CompilerLoadContext => _compilerLoadContext;
        internal AnalyzerLoadOption AnalyzerLoadOption => _loadOption;

        internal AnalyzerAssemblyLoader()
            : this(null, AnalyzerLoadOption.LoadFromDisk)
        {
        }

        internal AnalyzerAssemblyLoader(AssemblyLoadContext? compilerLoadContext, AnalyzerLoadOption loadOption)
        {
            _loadOption = loadOption;
            _compilerLoadContext = compilerLoadContext ?? AssemblyLoadContext.GetLoadContext(typeof(AnalyzerAssemblyLoader).GetTypeInfo().Assembly)!;
        }

        private partial Assembly Load(AssemblyName assemblyName, string assemblyOriginalPath)
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

        private partial bool IsMatch(AssemblyName requestedName, AssemblyName candidateName) =>
            requestedName.Name == candidateName.Name;

        internal DirectoryLoadContext[] GetDirectoryLoadContextsSnapshot()
        {
            lock (_guard)
            {
                return _loadContextByDirectory.Values.OrderBy(v => v.Directory).ToArray();
            }
        }

        internal void UnloadAll()
        {
            List<DirectoryLoadContext> contexts;
            lock (_guard)
            {
                contexts = _loadContextByDirectory.Values.ToList();
                _loadContextByDirectory.Clear();
            }

            foreach (var context in contexts)
            {
                context.Unload();
            }
        }

        internal sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            internal string Directory { get; }
            private readonly AnalyzerAssemblyLoader _loader;
            private readonly AssemblyLoadContext _compilerLoadContext;

            public DirectoryLoadContext(string directory, AnalyzerAssemblyLoader loader, AssemblyLoadContext compilerLoadContext)
                : base(isCollectible: true)
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
                    (_, var loadPath) = _loader.GetAssemblyInfoForPath(assemblyPath);
                    return loadCore(loadPath);
                }

                // Next prefer registered dependencies from other directories. Ideally this would not
                // be necessary but msbuild target defaults have caused a number of customers to 
                // fall into this path. See discussion here for where it comes up
                // https://github.com/dotnet/roslyn/issues/56442
                if (_loader.GetBestPath(assemblyName) is string bestRealPath)
                {
                    return loadCore(bestRealPath);
                }

                // No analyzer registered this dependency. Time to fail
                return null;

                Assembly loadCore(string assemblyPath)
                {
                    if (_loader.AnalyzerLoadOption == AnalyzerLoadOption.LoadFromDisk)
                    {
                        return LoadFromAssemblyPath(assemblyPath);
                    }
                    else
                    {
                        using var stream = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        return LoadFromStream(stream);
                    }
                }
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var assemblyPath = Path.Combine(Directory, unmanagedDllName + ".dll");
                if (_loader.IsAnalyzerDependencyPath(assemblyPath))
                {
                    (_, var loadPath) = _loader.GetAssemblyInfoForPath(assemblyPath);
                    return LoadUnmanagedDllFromPath(loadPath);
                }

                return IntPtr.Zero;
            }
        }
    }
}

#endif
