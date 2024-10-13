// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;

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

        internal AnalyzerAssemblyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
            : this(null, AnalyzerLoadOption.LoadFromDisk, externalResolvers)
        {
        }

        internal AnalyzerAssemblyLoader(AssemblyLoadContext? compilerLoadContext, AnalyzerLoadOption loadOption, ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        {
            _loadOption = loadOption;
            _compilerLoadContext = compilerLoadContext ?? AssemblyLoadContext.GetLoadContext(typeof(AnalyzerAssemblyLoader).GetTypeInfo().Assembly)!;
            _externalResolvers = [.. externalResolvers, new CompilerAnalyzerAssemblyResolver(_compilerLoadContext)];
        }

        public bool IsHostAssembly(Assembly assembly)
        {
            CheckIfDisposed();

            var alc = AssemblyLoadContext.GetLoadContext(assembly);
            return alc == _compilerLoadContext || alc == AssemblyLoadContext.Default;
        }

        private partial Assembly Load(AssemblyName assemblyName, string assemblyOriginalPath)
        {
            DirectoryLoadContext? loadContext;

            var fullDirectoryPath = Path.GetDirectoryName(assemblyOriginalPath) ?? throw new ArgumentException(message: null, paramName: nameof(assemblyOriginalPath));
            lock (_guard)
            {
                if (!_loadContextByDirectory.TryGetValue(fullDirectoryPath, out loadContext))
                {
                    loadContext = new DirectoryLoadContext(fullDirectoryPath, this);
                    _loadContextByDirectory[fullDirectoryPath] = loadContext;
                }
            }

            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        private partial bool IsMatch(AssemblyName requestedName, AssemblyName candidateName) =>
            requestedName.Name == candidateName.Name;

        internal DirectoryLoadContext[] GetDirectoryLoadContextsSnapshot()
        {
            CheckIfDisposed();

            lock (_guard)
            {
                return _loadContextByDirectory.Values.OrderBy(v => v.Directory).ToArray();
            }
        }

        private partial void DisposeWorker()
        {
            var contexts = ArrayBuilder<DirectoryLoadContext>.GetInstance();
            lock (_guard)
            {
                foreach (var (_, context) in _loadContextByDirectory)
                    contexts.Add(context);

                _loadContextByDirectory.Clear();
            }

            foreach (var context in contexts)
            {
                try
                {
                    context.Unload();
                }
                catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
                {
                }
            }

            contexts.Free();
        }

        internal sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            internal string Directory { get; }
            private readonly AnalyzerAssemblyLoader _loader;

            public DirectoryLoadContext(string directory, AnalyzerAssemblyLoader loader)
                : base(isCollectible: true)
            {
                Directory = directory;
                _loader = loader;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                if (_loader.ResolveAssemblyExternally(assemblyName) is { } externallyResolvedAssembly)
                {
                    return externallyResolvedAssembly;
                }

                // Prefer registered dependencies in the same directory first.
                var simpleName = assemblyName.Name!;
                var assemblyPath = Path.Combine(Directory, simpleName + ".dll");
                if (_loader.IsAnalyzerDependencyPath(assemblyPath))
                {
                    (_, var loadPath) = _loader.GetAssemblyInfoForPath(assemblyPath);
                    return loadCore(loadPath);
                }

                // Next if this is a resource assembly for a known assembly then load it from the 
                // appropriate sub directory if it exists
                //
                // Note: when loading from disk the .NET runtime has a fallback step that will handle
                // satellite assembly loading if the call to Load(satelliteAssemblyName) fails. This
                // loader has a mode where it loads from Stream though and the runtime will not handle
                // that automatically. Rather than bifurcate our loading behavior between Disk and
                // Stream both modes just handle satellite loading directly
                if (assemblyName.CultureInfo is not null && simpleName.EndsWith(".resources", StringComparison.Ordinal))
                {
                    var analyzerFileName = Path.ChangeExtension(simpleName, ".dll");
                    var analyzerFilePath = Path.Combine(Directory, analyzerFileName);
                    var satelliteLoadPath = _loader.GetRealSatelliteLoadPath(analyzerFilePath, assemblyName.CultureInfo);
                    if (satelliteLoadPath is not null)
                    {
                        return loadCore(satelliteLoadPath);
                    }

                    return null;
                }

                // Next prefer registered dependencies from other directories. Ideally this would not
                // be necessary but msbuild target defaults have caused a number of customers to 
                // fall into this path. See discussion here for where it comes up
                // https://github.com/dotnet/roslyn/issues/56442
                var (_, bestRealPath) = _loader.GetBestPath(assemblyName);
                if (bestRealPath is not null)
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

        /// <summary>
        /// A resolver which allows a passed in <see cref="AssemblyLoadContext"/> from the compiler 
        /// to control assembly resolution. This is important because there are many exchange types
        /// that need to unify across the multiple analyzer ALCs. These include common types from
        /// <c>Microsoft.CodeAnalysis.dll</c> etc, as well as platform assemblies provided by a 
        /// host such as visual studio.
        /// </summary>
        /// <remarks>
        /// This resolver essentially forces any assembly that was loaded as a 'core' part of the
        /// compiler to be shared across analyzers, and not loaded multiple times into each individual
        /// analyzer ALC, even if the analyzer itself shipped a copy of said assembly.
        /// </remarks>
        /// <param name="compilerContext">The <see cref="AssemblyLoadContext"/> that the core
        /// compiler assemblies are already loaded into.</param>
        internal sealed class CompilerAnalyzerAssemblyResolver(AssemblyLoadContext compilerContext) : IAnalyzerAssemblyResolver
        {
            private readonly AssemblyLoadContext _compilerAlc = compilerContext;

            public Assembly? ResolveAssembly(AssemblyName assemblyName)
            {
                try
                {
                    return _compilerAlc.LoadFromAssemblyName(assemblyName);
                }
                catch
                {
                    // The LoadFromAssemblyName method will throw if the assembly cannot be found. Need
                    // to catch this exception and return null to satisfy the interface contract.
                    return null;
                }
            }
        }
    }
}

#endif
