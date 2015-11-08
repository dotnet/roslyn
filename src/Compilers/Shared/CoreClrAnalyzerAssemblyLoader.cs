// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Roslyn.Utilities;

using static Microsoft.CodeAnalysis.AssemblyIdentityUtils;

namespace Microsoft.CodeAnalysis
{
    /// Core CLR compatible wrapper for loading analyzers.
    internal sealed class CoreClrAnalyzerAssemblyLoader : AssemblyLoadContext, IAnalyzerAssemblyLoader
    {
        private readonly Dictionary<string, Assembly> _pathsToAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Assembly> _namesToAssemblies = new Dictionary<string, Assembly>();
        private readonly List<string> _dependencyPaths = new List<string>();
        private readonly object _guard = new object();

        /// <summary>
        /// Creates a new instance of <see cref="CoreClrAnalyzerAssemblyLoader" />,
        /// sets that instance to be the default <see cref="AssemblyLoadContext" />,
        /// and returns that instance. Throws if the Default is already set or the
        /// binding model is already locked.
        /// </summary>
        public static CoreClrAnalyzerAssemblyLoader CreateAndSetDefault()
        {
            var assemblyLoader = new CoreClrAnalyzerAssemblyLoader();
            InitializeDefaultContext(assemblyLoader);
            return assemblyLoader;
        }

        public void AddDependencyLocation(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            lock (_guard)
            {
                _dependencyPaths.Add(fullPath);
            }
        }

        public Assembly LoadFromPath(string fullPath)
        {
            if (fullPath == null)
            {
                throw new ArgumentNullException(nameof(fullPath));
            }

            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                Assembly assembly;
                if (_pathsToAssemblies.TryGetValue(fullPath, out assembly))
                {
                    return assembly;
                }

                return LoadAndCache(fullPath);
            }
        }

        private static readonly string[] s_extensions = new string[] { ".dll", ".exe" };

        /// <summary>
        /// Searches and loads from the base directory of the current
        /// app context
        /// </summary>
        private Assembly AppContextLoad(AssemblyName assemblyName)
        {
            var baseDir = AppContext.BaseDirectory;
            foreach (var extension in s_extensions)
            {
                var path = Path.Combine(baseDir, assemblyName.Name + extension);

                if (File.Exists(path))
                {
                    lock (_guard)
                    {
                        return LoadAndCache(path);
                    }
                }
            }
            return null;
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            lock (_guard)
            {
                // Try and grab assembly using standard load
                Assembly assembly = AppContextLoad(assemblyName);
                if (assembly != null)
                {
                    return assembly;
                }

                string fullName = assemblyName.FullName;

                if (_namesToAssemblies.TryGetValue(fullName, out assembly))
                {
                    return assembly;
                }

                AssemblyIdentity requestedIdentity;
                if (!AssemblyIdentity.TryParseDisplayName(fullName, out requestedIdentity))
                {
                    return null;
                }

                foreach (var candidatePath in _dependencyPaths)
                {
                    if (IsAssemblyAlreadyLoaded(candidatePath) ||
                        !FileMatchesAssemblyName(candidatePath, requestedIdentity.Name))
                    {
                        continue;
                    }

                    var candidateIdentity = TryGetAssemblyIdentity(candidatePath);

                    if (requestedIdentity.Equals(candidateIdentity))
                    {
                        return LoadAndCache(candidatePath);
                    }
                }

                return null;
            }
        }

        /// <remarks>
        /// Assumes we have a lock on _guard
        /// </remarks>
        private Assembly LoadAndCache(string fullPath)
        {
            var assembly = LoadFromAssemblyPath(fullPath);
            var name = assembly.FullName;

            _pathsToAssemblies[fullPath] = assembly;
            _namesToAssemblies[name] = assembly;

            return assembly;
        }

        private bool IsAssemblyAlreadyLoaded(string path)
        {
            return _pathsToAssemblies.ContainsKey(path);
        }

        private bool FileMatchesAssemblyName(string path, string assemblySimpleName)
        {
            return Path.GetFileNameWithoutExtension(path).Equals(assemblySimpleName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
