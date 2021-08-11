// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#if NETCOREAPP

using System;
using System.Collections.Generic;
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
                var currentAssembly = Assembly.GetExecutingAssembly();
                if (shouldLoadInCompilerContext())
                {
                    return AssemblyLoadContext.GetLoadContext(currentAssembly)!.LoadFromAssemblyName(assemblyName);
                }

                var simpleName = assemblyName.Name;
                var assemblyPath = Path.Combine(_directory, assemblyName.Name + ".dll");
                if (!_loader.ShouldLoadInAnalyzerContext(assemblyPath))
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

                bool shouldLoadInCompilerContext()
                {
                    return assemblyName.Name is "Microsoft.CodeAnalysis" or "Microsoft.CodeAnalysis.CSharp" or "Microsoft.CodeAnalysis.VisualBasic"
                        || currentAssembly.GetReferencedAssemblies().Any(
                            static (referencedAssemblyName, assemblyName) => AssemblyName.ReferenceMatchesDefinition(referencedAssemblyName, assemblyName), assemblyName);
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
