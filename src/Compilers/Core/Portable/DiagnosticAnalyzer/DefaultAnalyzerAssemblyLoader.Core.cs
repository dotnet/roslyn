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
            if (!ShouldLoad(fullPath))
            {
                throw new InvalidOperationException();
            }

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

            // We allow analyzer loaders to "group" assemblies by directory,
            // and then perform a substitution before the assembly is actually loaded.
            var pathToLoad = GetPathToLoad(fullPath);
            return loadContext.LoadFromAssemblyPath(pathToLoad);
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
                // Any compilers assembly or any assembly referenced by the compilers needs to "win"
                // over a user-specified version of that assembly.
                var currentAssembly = Assembly.GetExecutingAssembly();
                if (assemblyName.Name is "Microsoft.CodeAnalysis" or "Microsoft.CodeAnalysis.CSharp" or "Microsoft.CodeAnalysis.VisualBasic"
                    || currentAssembly.GetReferencedAssemblies().Any(
                        static (referencedAssemblyName, assemblyName) => AssemblyName.ReferenceMatchesDefinition(referencedAssemblyName, assemblyName), assemblyName))
                {
                    return AssemblyLoadContext.GetLoadContext(currentAssembly)!.LoadFromAssemblyName(assemblyName);
                }

                var simpleName = assemblyName.Name;
                var assemblyPath = Path.Combine(_directory, assemblyName.Name + ".dll");
                if (!_loader.ShouldLoad(assemblyPath))
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
