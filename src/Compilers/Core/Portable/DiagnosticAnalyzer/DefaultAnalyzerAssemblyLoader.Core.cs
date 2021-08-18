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
                "System.Runtime",
                "System.Resources.ResourceManager",
                "System.Runtime.Extensions",
                "System.Diagnostics.Debug",
                "System.Collections.Immutable",
                "System.Collections",
                "System.Reflection.Metadata",
                "System.IO.FileSystem",
                "System.Collections.Concurrent",
                "System.Threading.ThreadPool",
                "System.Diagnostics.StackTrace",
                "System.Linq",
                "System.Security.Cryptography.Algorithms",
                "System.Threading",
                "System.Threading.Tasks.Parallel",
                "System.Threading.Tasks",
                "System.Xml.XDocument",
                "System.Threading.Thread",
                "System.Security.Cryptography.Primitives",
                "System.Runtime.InteropServices",
                "System.Reflection.Primitives",
                "System.Text.RegularExpressions",
                "System.Xml.ReaderWriter",
                "System.Runtime.Loader",
                "System.IO.Compression",
                "System.Memory",
                "System.Runtime.Numerics",
                "System.Runtime.Serialization.Primitives",
                "System.Console",
                "System.Xml.XPath.XDocument",
                "System.Text.Encoding.Extensions",
                "System.Text.Encoding.CodePages",
                "System.Runtime.CompilerServices.Unsafe",
                "Microsoft.CodeAnalysis.CSharp",
                "System.Linq.Expressions",
                "Microsoft.CodeAnalysis.VisualBasic");

        private readonly object _guard = new object();
        private readonly Dictionary<string, AssemblyLoadContext> _loadContextByDirectory = new Dictionary<string, AssemblyLoadContext>();

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
