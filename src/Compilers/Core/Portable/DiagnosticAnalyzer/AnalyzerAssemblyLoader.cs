// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly object _guard = new object();

        // lock _guard to read/write
        private readonly Dictionary<string, Assembly> _loadedAssembliesByPath = new Dictionary<string, Assembly>();
        private readonly Dictionary<string, AssemblyIdentity> _loadedAssemblyIdentitiesByPath = new Dictionary<string, AssemblyIdentity>();
        private readonly Dictionary<AssemblyIdentity, Assembly> _loadedAssembliesByIdentity = new Dictionary<AssemblyIdentity, Assembly>();

        // maps file name to a full path (lock _guard to read/write):
        private readonly Dictionary<string, List<string>> _knownAssemblyPathsBySimpleName = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        protected abstract Assembly LoadFromPathImpl(string fullPath);

        #region Public API

        public void AddDependencyLocation(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            string simpleName = PathUtilities.GetFileName(fullPath, includeExtension: false);

            lock (_guard)
            {
                List<string> paths;
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(simpleName, out paths))
                {
                    _knownAssemblyPathsBySimpleName.Add(simpleName, new List<string>() { fullPath });
                }
                else if (!paths.Contains(fullPath))
                {
                    paths.Add(fullPath);
                }
            }
        }

        public Assembly LoadFromPath(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            return LoadFromPathUnchecked(fullPath);
        }

        #endregion

        private Assembly LoadFromPathUnchecked(string fullPath)
        {
            return LoadFromPathUncheckedCore(fullPath);
        }

        private Assembly LoadFromPathUncheckedCore(string fullPath, AssemblyIdentity identity = null)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            // Check if we have already loaded an assembly with the same identity or from the given path.
            Assembly loadedAssembly = null;
            lock (_guard)
            {
                Assembly existingAssembly;
                if (_loadedAssembliesByPath.TryGetValue(fullPath, out existingAssembly))
                {
                    loadedAssembly = existingAssembly;
                }
                else
                {
                    identity = identity ?? GetOrAddAssemblyIdentity(fullPath);
                    if (identity != null && _loadedAssembliesByIdentity.TryGetValue(identity, out existingAssembly))
                    {
                        loadedAssembly = existingAssembly;
                    }
                }
            }

            // Otherwise, load the assembly.
            if (loadedAssembly == null)
            {
                loadedAssembly = LoadFromPathImpl(fullPath);
            }

            // Add the loaded assembly to both path and identity cache.
            return AddToCache(loadedAssembly, fullPath, identity);
        }

        private Assembly AddToCache(Assembly assembly, string fullPath, AssemblyIdentity identity)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));
            Debug.Assert(assembly != null);

            identity = AddToCache(fullPath, identity ?? AssemblyIdentity.FromAssemblyDefinition(assembly));
            Debug.Assert(identity != null);

            lock (_guard)
            {
                // The same assembly may be loaded from two different full paths (e.g. when loaded from GAC, etc.),
                // or another thread might have loaded the assembly after we checked above.
                Assembly existingAssembly;
                if (_loadedAssembliesByIdentity.TryGetValue(identity, out existingAssembly))
                {
                    assembly = existingAssembly;
                }
                else
                {
                    _loadedAssembliesByIdentity.Add(identity, assembly);
                }

                // An assembly file might be replaced by another file with a different identity.
                // Last one wins.
                _loadedAssembliesByPath[fullPath] = assembly;

                return assembly;
            }
        }

        private AssemblyIdentity GetOrAddAssemblyIdentity(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                AssemblyIdentity existingIdentity;
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out existingIdentity))
                {
                    return existingIdentity;
                }
            }

            var identity = AssemblyIdentityUtils.TryGetAssemblyIdentity(fullPath);
            return AddToCache(fullPath, identity);
        }

        private AssemblyIdentity AddToCache(string fullPath, AssemblyIdentity identity)
        {
            lock (_guard)
            {
                AssemblyIdentity existingIdentity;
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out existingIdentity) && existingIdentity != null)
                {
                    identity = existingIdentity;
                }
                else
                {
                    _loadedAssemblyIdentitiesByPath[fullPath] = identity;
                }
            }

            return identity;
        }

        public Assembly Load(string displayName)
        {
            AssemblyIdentity requestedIdentity;
            if (!AssemblyIdentity.TryParseDisplayName(displayName, out requestedIdentity))
            {
                return null;
            }

            ImmutableArray<string> candidatePaths;
            lock (_guard)
            {
                Assembly existingAssembly;
                
                // First, check if this loader already loaded the requested assembly:
                if (_loadedAssembliesByIdentity.TryGetValue(requestedIdentity, out existingAssembly))
                {
                    return existingAssembly;
                }

                // Second, check if an assembly file of the same simple name was registered with the loader:
                List<string> pathList;
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(requestedIdentity.Name, out pathList))
                {
                    return null;
                }

                Debug.Assert(pathList.Count > 0);
                candidatePaths = pathList.ToImmutableArray();
            }

            // Multiple assemblies of the same simple name but different identities might have been registered.
            // Load the one that matches the requested identity (if any).
            foreach (var candidatePath in candidatePaths)
            {
                var candidateIdentity = GetOrAddAssemblyIdentity(candidatePath);

                if (requestedIdentity.Equals(candidateIdentity))
                {
                    return LoadFromPathUncheckedCore(candidatePath, candidateIdentity);
                }
            }

            return null;
        }
    }
}
