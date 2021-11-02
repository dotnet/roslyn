// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly object _guard = new();

        // lock _guard to read/write
        private readonly Dictionary<string, Assembly> _loadedAssembliesByPath = new();

        // maps file name to a full path (lock _guard to read/write):
        private readonly Dictionary<string, HashSet<string>> _knownAssemblyPathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);

        protected abstract Assembly LoadFromPathImpl(string fullPath);

        #region Public API

        public void AddDependencyLocation(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            string simpleName = PathUtilities.GetFileName(fullPath, includeExtension: false);

            lock (_guard)
            {
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(simpleName, out var paths))
                {
                    paths = new HashSet<string>(PathUtilities.Comparer);
                    _knownAssemblyPathsBySimpleName.Add(simpleName, paths);
                }

                paths.Add(fullPath);
            }
        }

        public Assembly LoadFromPath(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            return LoadFromPathUnchecked(fullPath);
        }

        #endregion

        protected Assembly LoadFromPathUnchecked(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            // Check if we have already loaded an assembly from the given path.
            Assembly? loadedAssembly = null;
            lock (_guard)
            {
                if (_loadedAssembliesByPath.TryGetValue(fullPath, out var existingAssembly))
                {
                    loadedAssembly = existingAssembly;
                }
            }

            // Otherwise, load the assembly.
            if (loadedAssembly == null)
            {
                loadedAssembly = LoadFromPathImpl(fullPath);
            }

            // Add the loaded assembly to the path cache.
            lock (_guard)
            {
                _loadedAssembliesByPath[fullPath] = loadedAssembly;
            }

            return loadedAssembly;
        }

        protected HashSet<string>? GetPaths(string simpleName)
        {
            _knownAssemblyPathsBySimpleName.TryGetValue(simpleName, out var paths);
            return paths;
        }

        /// <summary>
        /// When overridden in a derived class, allows substituting an assembly path after we've
        /// identified the context to load an assembly in, but before the assembly is actually
        /// loaded from disk. This is used to substitute out the original path with the shadow-copied version.
        /// </summary>
        protected virtual string GetPathToLoad(string fullPath)
        {
            return fullPath;
        }
    }
}
