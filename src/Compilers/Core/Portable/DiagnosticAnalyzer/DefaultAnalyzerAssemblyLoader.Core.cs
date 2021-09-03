// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        /// <summary>
        /// <p>Typically a user analyzer has a reference to the compiler and some of the compiler's
        /// dependencies such as System.Collections.Immutable. For the analyzer to correctly
        /// interoperate with the compiler that created it, we need to ensure that we always use the
        /// compiler's version of a given assembly over the analyzer's version.</p>
        ///
        /// <p>If we neglect to do this, then in the case where the user ships the compiler or its
        /// dependencies in the analyzer's bin directory, we could end up loading a separate
        /// instance of those assemblies in the process of loading the analyzer, which will surface
        /// as a failure to load the analyzer.</p>
        /// </summary>
        internal static readonly ImmutableHashSet<string> CompilerAssemblySimpleNames =
           ImmutableHashSet.Create(
               StringComparer.OrdinalIgnoreCase,
               "Microsoft.CodeAnalysis",
               "Microsoft.CodeAnalysis.CSharp",
               "Microsoft.CodeAnalysis.VisualBasic",
               "System.Collections",
               "System.Collections.Concurrent",
               "System.Collections.Immutable",
               "System.Console",
               "System.Diagnostics.Debug",
               "System.Diagnostics.StackTrace",
               "System.Diagnostics.Tracing",
               "System.IO.Compression",
               "System.IO.FileSystem",
               "System.Linq",
               "System.Linq.Expressions",
               "System.Memory",
               "System.Reflection.Metadata",
               "System.Reflection.Primitives",
               "System.Resources.ResourceManager",
               "System.Runtime",
               "System.Runtime.CompilerServices.Unsafe",
               "System.Runtime.Extensions",
               "System.Runtime.InteropServices",
               "System.Runtime.Loader",
               "System.Runtime.Numerics",
               "System.Runtime.Serialization.Primitives",
               "System.Security.Cryptography.Algorithms",
               "System.Security.Cryptography.Primitives",
               "System.Text.Encoding.CodePages",
               "System.Text.Encoding.Extensions",
               "System.Text.RegularExpressions",
               "System.Threading",
               "System.Threading.Tasks",
               "System.Threading.Tasks.Parallel",
               "System.Threading.Thread",
               "System.Threading.ThreadPool",
               "System.Xml.ReaderWriter",
               "System.Xml.XDocument",
               "System.Xml.XPath.XDocument");

        private readonly object _guard = new object();
        private readonly Dictionary<string, DirectoryLoadContext> _loadContextByDirectory = new Dictionary<string, DirectoryLoadContext>(StringComparer.Ordinal);

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            DirectoryLoadContext? loadContext;

            var fullDirectoryPath = Path.GetDirectoryName(fullPath) ?? throw new ArgumentException(message: null, paramName: nameof(fullPath));
            lock (_guard)
            {
                if (!_loadContextByDirectory.TryGetValue(fullDirectoryPath, out loadContext))
                {
                    loadContext = new DirectoryLoadContext(fullDirectoryPath, this);
                    _loadContextByDirectory[fullDirectoryPath] = loadContext;
                }
            }

            var name = AssemblyName.GetAssemblyName(fullPath);
            return loadContext.LoadFromAssemblyName(name);
        }

        internal static class TestAccessor
        {
            public static AssemblyLoadContext[] GetOrderedLoadContexts(DefaultAnalyzerAssemblyLoader loader)
            {
                return loader._loadContextByDirectory.Values.OrderBy(v => v.Directory).ToArray();
            }
        }

        private sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            internal string Directory { get; }
            private readonly DefaultAnalyzerAssemblyLoader _loader;

            public DirectoryLoadContext(string directory, DefaultAnalyzerAssemblyLoader loader)
            {
                Directory = directory;
                _loader = loader;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var simpleName = assemblyName.Name!;
                if (CompilerAssemblySimpleNames.Contains(simpleName))
                {
                    // Delegate to the compiler's load context to load the compiler or anything
                    // referenced by the compiler
                    return null;
                }

                var assemblyPath = Path.Combine(Directory, simpleName + ".dll");
                if (!_loader.IsKnownDependencyLocation(assemblyPath))
                {
                    // The analyzer didn't explicitly register this dependency. Most likely the
                    // assembly we're trying to load here is netstandard or a similar framework
                    // assembly. We assume that if that is not the case, then the parent ALC will
                    // fail to load this.
                    return null;
                }

                var pathToLoad = _loader.GetPathToLoad(assemblyPath);
                return LoadFromAssemblyPath(pathToLoad);
            }

            protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
            {
                var assemblyPath = Path.Combine(Directory, unmanagedDllName + ".dll");
                if (!_loader.IsKnownDependencyLocation(assemblyPath))
                {
                    return IntPtr.Zero;
                }

                var pathToLoad = _loader.GetPathToLoad(assemblyPath);
                return LoadUnmanagedDllFromPath(pathToLoad);
            }
        }
    }
}

#endif
