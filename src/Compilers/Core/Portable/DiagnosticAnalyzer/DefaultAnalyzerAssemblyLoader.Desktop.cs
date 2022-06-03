// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Loads analyzer assemblies from their original locations in the file system.
    /// Assemblies will only be loaded from the locations specified when the loader
    /// is instantiated.
    /// </summary>
    /// <remarks>
    /// This type is meant to be used in scenarios where it is OK for the analyzer
    /// assemblies to be locked on disk for the lifetime of the host; for example,
    /// csc.exe and vbc.exe. In scenarios where support for updating or deleting
    /// the analyzer on disk is required a different loader should be used.
    /// </remarks>
    internal class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private readonly object _guard = new();

        private readonly Dictionary<AssemblyIdentity, Assembly> _loadedAssembliesByIdentity = new();
        private readonly Dictionary<string, AssemblyIdentity?> _loadedAssemblyIdentitiesByPath = new();

        private int _hookedAssemblyResolve;

        protected override Assembly LoadFromPathUncheckedImpl(string fullPath)
        {
            if (Interlocked.CompareExchange(ref _hookedAssemblyResolve, 0, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }

            AssemblyIdentity? identity;

            lock (_guard)
            {
                identity = GetOrAddAssemblyIdentity(fullPath);
                if (identity != null && _loadedAssembliesByIdentity.TryGetValue(identity, out var existingAssembly))
                {
                    return existingAssembly;
                }
            }

            var pathToLoad = GetPathToLoad(fullPath);
            var loadedAssembly = Assembly.LoadFrom(pathToLoad);

            lock (_guard)
            {
                identity ??= identity ?? AssemblyIdentity.FromAssemblyDefinition(loadedAssembly);

                // The same assembly may be loaded from two different full paths (e.g. when loaded from GAC, etc.),
                // or another thread might have loaded the assembly after we checked above.
                if (_loadedAssembliesByIdentity.TryGetValue(identity, out var existingAssembly))
                {
                    loadedAssembly = existingAssembly;
                }
                else
                {
                    _loadedAssembliesByIdentity.Add(identity, loadedAssembly);
                }

                return loadedAssembly;
            }
        }

        private Assembly? CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // In the .NET Framework, if a handler to AssemblyResolve throws an exception, other handlers
            // are not called. To avoid any bug in our handler breaking other handlers running in the same process
            // we catch exceptions here. We do not expect exceptions to be thrown though.
            try
            {
                return GetOrLoad(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
            }
            catch
            {
                return null;
            }
        }

        private AssemblyIdentity? GetOrAddAssemblyIdentity(string fullPath)
        {
            Debug.Assert(PathUtilities.IsAbsolute(fullPath));

            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity))
                {
                    return existingIdentity;
                }
            }

            var identity = AssemblyIdentityUtils.TryGetAssemblyIdentity(fullPath);

            lock (_guard)
            {
                if (_loadedAssemblyIdentitiesByPath.TryGetValue(fullPath, out var existingIdentity) && existingIdentity != null)
                {
                    // Somebody else beat us, so used the cached value
                    identity = existingIdentity;
                }
                else
                {
                    _loadedAssemblyIdentitiesByPath[fullPath] = identity;
                }
            }

            return identity;
        }

        private Assembly? GetOrLoad(string displayName)
        {
            if (!AssemblyIdentity.TryParseDisplayName(displayName, out var requestedIdentity))
            {
                return null;
            }

            ImmutableHashSet<string> candidatePaths;
            lock (_guard)
            {

                // First, check if this loader already loaded the requested assembly:
                if (_loadedAssembliesByIdentity.TryGetValue(requestedIdentity, out var existingAssembly))
                {
                    return existingAssembly;
                }
                // Second, check if an assembly file of the same simple name was registered with the loader:
                candidatePaths = GetPaths(requestedIdentity.Name);
                if (candidatePaths is null)
                {
                    return null;
                }

                Debug.Assert(candidatePaths.Count > 0);
            }

            // Find the highest version that satisfies the original request. We'll match for the highest version we can, assuming it
            // actually matches the original request
            string? bestPath = null;
            Version? bestIdentityVersion = null;

            // Sort the candidate paths by ordinal, to ensure determinism with the same inputs if you were to have multiple assemblies
            // providing the same version.
            foreach (var candidatePath in candidatePaths.OrderBy(StringComparer.Ordinal))
            {
                var candidateIdentity = GetOrAddAssemblyIdentity(candidatePath);

                if (candidateIdentity is not null &&
                    candidateIdentity.Version >= requestedIdentity.Version &&
                    candidateIdentity.PublicKeyToken.SequenceEqual(requestedIdentity.PublicKeyToken))
                {
                    if (bestIdentityVersion is null || candidateIdentity.Version > bestIdentityVersion)
                    {
                        bestPath = candidatePath;
                        bestIdentityVersion = candidateIdentity.Version;
                    }
                }
            }

            if (bestPath != null)
            {
                return LoadFromPathUnchecked(bestPath);
            }

            return null;
        }
    }
}

#endif
