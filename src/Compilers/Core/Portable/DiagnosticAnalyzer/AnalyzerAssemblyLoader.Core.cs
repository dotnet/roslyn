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
    internal sealed partial class AnalyzerAssemblyLoader
    {
        internal static IAnalyzerAssemblyResolver DiskAnalyzerAssemblyResolver => DiskResolver.Instance;
        internal static IAnalyzerAssemblyResolver StreamAnalyzerAssemblyResolver => StreamResolver.Instance;

        /// <summary>
        /// Map of resolved directory paths to load contexts that manage their assemblies.
        /// </summary>
        private readonly Dictionary<string, DirectoryLoadContext> _loadContextByDirectory = new Dictionary<string, DirectoryLoadContext>(GeneratedPathComparer);

        public IAnalyzerAssemblyResolver CompilerAnalyzerAssemblyResolver { get; }
        public AssemblyLoadContext CompilerLoadContext { get; }
        public ImmutableArray<IAnalyzerAssemblyResolver> AnalyzerAssemblyResolvers { get; }

        internal AnalyzerAssemblyLoader()
            : this(pathResolvers: [])
        {
        }

        internal AnalyzerAssemblyLoader(ImmutableArray<IAnalyzerPathResolver> pathResolvers)
            : this(pathResolvers, assemblyResolvers: [DiskAnalyzerAssemblyResolver], compilerLoadContext: null)
        {
        }

        /// <summary>
        /// Create a new <see cref="AnalyzerAssemblyLoader"/> with the given resolvers.
        /// </summary>
        /// <param name="compilerLoadContext">This is the <see cref="AssemblyLoadContext"/> where the compiler resides. This parameter
        /// is primarily used for testing purposes but is also useful in hosted scenarios where the compiler may be loaded outside
        /// the default context. When null this will be the <see cref="AssemblyLoadContext"/> the compiler currently resides
        /// in </param>
        /// <exception cref="ArgumentException"></exception>
        internal AnalyzerAssemblyLoader(
            ImmutableArray<IAnalyzerPathResolver> pathResolvers,
            ImmutableArray<IAnalyzerAssemblyResolver> assemblyResolvers,
            AssemblyLoadContext? compilerLoadContext)
        {
            if (assemblyResolvers.Length == 0)
            {
                throw new ArgumentException("Cannot be empty", nameof(assemblyResolvers));
            }

            CompilerLoadContext = compilerLoadContext ?? AssemblyLoadContext.GetLoadContext(typeof(SyntaxTree).GetTypeInfo().Assembly)!;
            CompilerAnalyzerAssemblyResolver = new CompilerResolver(CompilerLoadContext);
            AnalyzerPathResolvers = pathResolvers;

            // The CompilerAnalyzerAssemblyResolver must be first here as the host is _always_ given a chance
            // to resolve the assembly before any other resolver. This is crucial to allow for items like
            // unification of System.Collections.Immutable or other core assemblies for a host.
            AnalyzerAssemblyResolvers = [CompilerAnalyzerAssemblyResolver, .. assemblyResolvers];
        }

        public bool IsHostAssembly(Assembly assembly)
        {
            CheckIfDisposed();

            var alc = AssemblyLoadContext.GetLoadContext(assembly);
            return alc == CompilerLoadContext || alc == AssemblyLoadContext.Default;
        }

        private partial Assembly Load(AssemblyName assemblyName, string resolvedPath)
        {
            DirectoryLoadContext? loadContext;

            var fullDirectoryPath = Path.GetDirectoryName(resolvedPath) ?? throw new ArgumentException(message: null, paramName: nameof(resolvedPath));
            lock (_guard)
            {
                if (!_loadContextByDirectory.TryGetValue(fullDirectoryPath, out loadContext))
                {
                    loadContext = new DirectoryLoadContext(fullDirectoryPath, this);
                    CodeAnalysisEventSource.Log.CreateAssemblyLoadContext(fullDirectoryPath, loadContext.ToString());
                    _loadContextByDirectory[fullDirectoryPath] = loadContext;
                }
            }

            return loadContext.LoadFromAssemblyName(assemblyName);
        }

        /// <summary>
        /// Is this a registered analyzer file path that the loader knows about.
        /// 
        /// Note: this is using resolved paths, not the original file paths
        /// </summary>
        private bool IsRegisteredAnalyzerPath(string resolvedPath)
        {
            CheckIfDisposed();

            lock (_guard)
            {
                return _resolvedToOriginalPathMap.ContainsKey(resolvedPath);
            }
        }

        private string? GetAssemblyLoadPath(AssemblyName assemblyName, string directory)
        {
            // Prefer registered dependencies in the same directory first.
            var simpleName = assemblyName.Name!;
            var assemblyPath = Path.Combine(directory, simpleName + ".dll");
            if (IsRegisteredAnalyzerPath(assemblyPath))
            {
                return assemblyPath;
            }

            // Next if this is a resource assembly for a known assembly then load it from the 
            // appropriate sub directory if it exists
            //
            // Note: when loading from disk the .NET runtime has a fallback step that will handle
            // satellite assembly loading if the call to Load(satelliteAssemblyName) fails. This
            // loader has a mode where it loads from Stream though and the runtime will not handle
            // that automatically. Rather than bifurcate our loading behavior between Disk and
            // Stream both modes just handle satellite loading directly
            if (assemblyName.CultureInfo is not null && simpleName.EndsWith(".resources", SimpleNameComparer.Comparison))
            {
                var analyzerFileName = Path.ChangeExtension(simpleName, ".dll");
                var analyzerFilePath = Path.Combine(directory, analyzerFileName);
                return GetSatelliteLoadPath(analyzerFilePath, assemblyName.CultureInfo);
            }

            // Next prefer registered dependencies from other directories. Ideally this would not
            // be necessary but msbuild target defaults have caused a number of customers to 
            // fall into this path. See discussion here for where it comes up
            // https://github.com/dotnet/roslyn/issues/56442
            var (_, bestResolvedPath) = GetBestResolvedPath(assemblyName);
            if (bestResolvedPath is not null)
            {
                return bestResolvedPath;
            }

            // No analyzer registered this dependency. Time to fail
            return null;
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
            lock (_guard)
            {
                _loadContextByDirectory.Clear();
            }
        }

        internal sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            internal string Directory { get; }
            private readonly AnalyzerAssemblyLoader _loader;

            public DirectoryLoadContext(string directory, AnalyzerAssemblyLoader loader)
                : base(isCollectible: false)
            {
                Directory = directory;
                _loader = loader;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                foreach (var resolver in _loader.AnalyzerAssemblyResolvers)
                {
                    var assembly = resolver.Resolve(_loader, assemblyName, this, Directory);
                    if (assembly is not null)
                    {
                        CodeAnalysisEventSource.Log.ResolvedAssembly(Directory, assemblyName.ToString(), resolver.GetType().Name, assembly.Location, GetLoadContext(assembly)!.ToString());
                        return assembly;
                    }
                }

                CodeAnalysisEventSource.Log.ResolveAssemblyFailed(Directory, assemblyName.ToString());
                return null;
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var assemblyPath = Path.Combine(Directory, unmanagedDllName + ".dll");
                if (_loader.IsRegisteredAnalyzerPath(assemblyPath))
                {
                    return LoadUnmanagedDllFromPath(assemblyPath);
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
        private sealed class CompilerResolver(AssemblyLoadContext compilerContext) : IAnalyzerAssemblyResolver
        {
            private readonly AssemblyLoadContext _compilerAlc = compilerContext;

            public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory)
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

        private sealed class DiskResolver : IAnalyzerAssemblyResolver
        {
            public static readonly IAnalyzerAssemblyResolver Instance = new DiskResolver();
            public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory)
            {
                var assemblyPath = loader.GetAssemblyLoadPath(assemblyName, directory);
                return assemblyPath is not null ? directoryContext.LoadFromAssemblyPath(assemblyPath) : null;
            }
        }

        /// <summary>
        /// This loads the assemblies from a <see cref="Stream"/> which is advantageous because it does
        /// not lock the underlying assembly on disk.
        /// </summary>
        /// <remarks>
        /// This should be avoided on Windows. Yes <see cref="DiskResolver"/> locks files on disks but it also
        /// amortizes the cost of AV scanning the assemblies. When loading from <see cref="Stream"/>
        /// the AV will scan the assembly every single time. That cost is significant and easily shows up in
        /// performance profiles.
        /// </remarks>
        private sealed class StreamResolver : IAnalyzerAssemblyResolver
        {
            public static readonly IAnalyzerAssemblyResolver Instance = new StreamResolver();
            public Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory)
            {
                var assemblyPath = loader.GetAssemblyLoadPath(assemblyName, directory);
                if (assemblyPath is null)
                {
                    return null;
                }

                using var stream = File.Open(assemblyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                return directoryContext.LoadFromStream(stream);
            }
        }
    }
}

#endif
