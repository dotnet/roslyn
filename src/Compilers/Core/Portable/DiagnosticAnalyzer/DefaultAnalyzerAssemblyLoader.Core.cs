// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        internal static readonly ImmutableArray<string> CompilerAssemblySimpleNames =
            ImmutableArray.Create(
                "Microsoft.CodeAnalysis",
                "Microsoft.CodeAnalysis.CSharp",
                "Microsoft.CodeAnalysis.VisualBasic",
                "System.Collections",
                "System.Collections.Concurrent",
                "System.Collections.Immutable",
                "System.Console",
                "System.Diagnostics.Debug",
                "System.Diagnostics.StackTrace",
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
        private readonly Dictionary<string, AssemblyLoadContext> _loadContextByDirectory = new Dictionary<string, AssemblyLoadContext>(StringComparer.Ordinal);

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            AssemblyLoadContext? loadContext;

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

        private bool ShouldLoadInAnalyzerContext(string? assemblySimpleName, string fullPath)
        {
            foreach (var compilerAssemblySimpleName in CompilerAssemblySimpleNames)
            {
                if (string.Equals(compilerAssemblySimpleName, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return IsKnownDependencyLocation(fullPath);
        }

        private sealed class DirectoryLoadContext : AssemblyLoadContext
        {
            private readonly string _directory;
            private readonly DefaultAnalyzerAssemblyLoader _loader;

            public DirectoryLoadContext(string directory, DefaultAnalyzerAssemblyLoader loader)
            {
                _directory = directory;
                _loader = loader;
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var simpleName = assemblyName.Name;
                var assemblyPath = Path.Combine(_directory, simpleName + ".dll");
                if (!_loader.ShouldLoadInAnalyzerContext(simpleName, assemblyPath))
                {
                    return null;
                }

                var pathToLoad = _loader.GetPathToLoad(assemblyPath);
                try
                {
                    return LoadFromAssemblyPath(pathToLoad);
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
