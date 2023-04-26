// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// The base implementation for <see cref="IAnalyzerAssemblyLoader"/>. This type provides caching and tracking of inputs given
    /// to <see cref="AddDependencyLocation(string)"/>.
    /// </summary>
    /// <remarks>
    /// This type generally assumes that files on disk aren't changing, since it ensure that two calls to <see cref="LoadFromPath(string)"/>
    /// will always return the same thing, per that interface's contract.
    /// </remarks>
    internal abstract partial class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
    {
        private readonly object _guard = new();

        /// <summary>
        /// Set of analyzer dependencies original full paths to the data calculated for that path
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, (AssemblyName? AssemblyName, string RealAssemblyPath)?> _analyzerAssemblyInfoMap = new();

        /// <summary>
        /// Maps analyzer dependency simple names to the set of original full paths it was loaded from. This _only_ 
        /// tracks the paths provided to the analyzer as it's a place to look for indirect loads. 
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, ImmutableHashSet<string>> _knownAssemblyPathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The implementation needs to load an <see cref="Assembly"/> with the specified <see cref="AssemblyName"/>. The
        /// <paramref name="assemblyOriginalPath"/> parameter is the original path. It may be different than
        /// <see cref="AssemblyName.CodeBase"/> as that is empty on .NET Core.
        /// </summary>
        /// <remarks>
        /// This method should return an <see cref="Assembly"/> instance or throw.
        /// </remarks>
        private partial Assembly Load(AssemblyName assemblyName, string assemblyOriginalPath);

        /// <summary>
        /// Determines if the <paramref name="candidateName"/> satisfies the request for 
        /// <paramref name="requestedName"/>. This is partial'd out as each runtime has a different 
        /// definition of matching name.
        /// </summary>
        private partial bool IsMatch(AssemblyName requestedName, AssemblyName candidateName);

        internal bool IsAnalyzerDependencyPath(string fullPath)
        {
            lock (_guard)
            {
                return _analyzerAssemblyInfoMap.ContainsKey(fullPath);
            }
        }

        public void AddDependencyLocation(string fullPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            string simpleName = PathUtilities.GetFileName(fullPath, includeExtension: false);

            lock (_guard)
            {
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(simpleName, out var paths))
                {
                    paths = ImmutableHashSet.Create(PathUtilities.Comparer, fullPath);
                    _knownAssemblyPathsBySimpleName.Add(simpleName, paths);
                }
                else
                {
                    _knownAssemblyPathsBySimpleName[simpleName] = paths.Add(fullPath);
                }

                // This type assumses the file system is static for the duration of the
                // it's instance. Repeated calls to this method, even if the underlying 
                // file system contents, should reuse the results of the first call.
                _ = _analyzerAssemblyInfoMap.TryAdd(fullPath, null);
            }
        }

        public Assembly LoadFromPath(string originalAnalyzerPath)
        {
            CompilerPathUtilities.RequireAbsolutePath(originalAnalyzerPath, nameof(originalAnalyzerPath));

            (AssemblyName? assemblyName, string _) = GetAssemblyInfoForPath(originalAnalyzerPath);

            // Not a managed assembly, nothing else to do
            if (assemblyName is null)
            {
                throw new ArgumentException($"Not a valid assembly: {originalAnalyzerPath}");
            }

            try
            {
                return Load(assemblyName, originalAnalyzerPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to load {assemblyName.Name}", ex);
            }
        }

        /// <summary>
        /// Get the <see cref="AssemblyName"/> and the path it should be loaded from for the given original 
        /// analyzer path
        /// </summary>
        /// <remarks>
        /// This is used in the implementation of the loader instead of <see cref="AssemblyName.GetAssemblyName(string)"/>
        /// because we only want information for registered paths. Using unregistered paths inside the
        /// implementation should result in errors.
        /// </remarks>
        protected (AssemblyName? AssemblyName, string RealAssemblyPath) GetAssemblyInfoForPath(string originalAnalyzerPath)
        {
            lock (_guard)
            {
                if (!_analyzerAssemblyInfoMap.TryGetValue(originalAnalyzerPath, out var tuple))
                {
                    throw new InvalidOperationException();
                }

                if (tuple is { } info)
                {
                    return info;
                }
            }

            string realPath = PreparePathToLoad(originalAnalyzerPath);
            AssemblyName? assemblyName;
            try
            {
                assemblyName = AssemblyName.GetAssemblyName(realPath);
            }
            catch
            {
                // The above can fail when the assembly doesn't exist because it's corrupted, 
                // doesn't exist on disk, or is a native DLL. Those failures are handled when 
                // the actual load is attempted. Just record the failure now.
                assemblyName = null;
            }

            lock (_guard)
            {
                _analyzerAssemblyInfoMap[originalAnalyzerPath] = (assemblyName, realPath);
            }

            return (assemblyName, realPath);
        }

        /// <summary>
        /// Return the best path for loading an assembly with the specified <see cref="AssemblyName"/>. This
        /// return is a real path to load, not an original path.
        /// </summary>
        protected string? GetBestPath(AssemblyName requestedName)
        {
            if (requestedName.Name is null)
            {
                return null;
            }

            ImmutableHashSet<string>? paths;
            lock (_guard)
            {
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(requestedName.Name, out paths))
                {
                    return null;
                }
            }

            // Sort the candidate paths by ordinal, to ensure determinism with the same inputs if you were to have
            // multiple assemblies providing the same version.
            string? bestPath = null;
            AssemblyName? bestName = null;
            foreach (var candidateOriginalPath in paths.OrderBy(StringComparer.Ordinal))
            {
                (AssemblyName? candidateName, string candidateRealPath) = GetAssemblyInfoForPath(candidateOriginalPath);
                if (candidateName is null)
                {
                    continue;
                }

                if (IsMatch(requestedName, candidateName))
                {
                    if (candidateName.Version == requestedName.Version)
                    {
                        return candidateRealPath;
                    }

                    if (bestName is null || candidateName.Version > bestName.Version)
                    {
                        bestPath = candidateRealPath;
                        bestName = candidateName;
                    }
                }
            }

            return bestPath;
        }

        /// <summary>
        /// When overridden in a derived class, allows substituting an assembly path after we've
        /// identified the context to load an assembly in, but before the assembly is actually
        /// loaded from disk. This is used to substitute out the original path with the shadow-copied version.
        /// </summary>
        protected abstract string PreparePathToLoad(string fullPath);

        /// <summary>
        /// When <see cref="PreparePathToLoad(string)"/> is overriden this returns the most recent
        /// real path calculated for the <paramref name="originalFullPath"/>
        /// </summary>
        internal string GetRealLoadPath(string originalFullPath)
        {
            lock (_guard)
            {
                if (!_analyzerAssemblyInfoMap.TryGetValue(originalFullPath, out var tuple))
                {
                    throw new InvalidOperationException($"Invalid original path: {originalFullPath}");
                }

                return tuple is { } value ? value.RealAssemblyPath : originalFullPath;
            }
        }

        internal (string OriginalAssemblyPath, string RealAssemblyPath)[] GetPathMapSnapshot()
        {
            lock (_guard)
            {
                return _analyzerAssemblyInfoMap
                    .Select(x => (x.Key, x.Value?.RealAssemblyPath ?? ""))
                    .OrderBy(x => x.Key)
                    .ToArray();
            }
        }
    }
}
