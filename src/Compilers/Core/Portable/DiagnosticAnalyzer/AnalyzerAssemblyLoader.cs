// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal interface IAnalyzerAssemblyLoaderInternal : IAnalyzerAssemblyLoader, IDisposable
    {
        /// <summary>
        /// Is this an <see cref="Assembly"/> that the loader considers to be part of the hosting 
        /// process. Either part of the compiler itself or the process hosting the compiler.
        /// </summary>
        bool IsHostAssembly(Assembly assembly);

        /// <summary>
        /// For a given <see cref="AssemblyName"/> return the location it was originally added 
        /// from. This will return null for any value that was not directly added through the 
        /// loader.
        /// </summary>
        string? GetOriginalDependencyLocation(AssemblyName assembly);
    }

    /// <summary>
    /// The base implementation for <see cref="IAnalyzerAssemblyLoader"/>. This type provides caching and tracking of inputs given
    /// to <see cref="AddDependencyLocation(string)"/>.
    /// </summary>
    /// <remarks>
    /// This type generally assumes that files on disk aren't changing, since it ensure that two calls to <see cref="LoadFromPath(string)"/>
    /// will always return the same thing, per that interface's contract.
    /// </remarks>
    internal abstract partial class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoaderInternal
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
        /// Mapping of analyzer dependency original full path and culture to the real satellite
        /// assembly path. If the satellite assembly doesn't exist for the original analyzer and 
        /// culture, the real path value stored will be null.
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<(string OriginalAnalyzerPath, CultureInfo CultureInfo), string?> _analyzerSatelliteAssemblyRealPaths = new();

        /// <summary>
        /// Maps analyzer dependency simple names to the set of original full paths it was loaded from. This _only_ 
        /// tracks the paths provided to the analyzer as it's a place to look for indirect loads. 
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, ImmutableHashSet<string>> _knownAssemblyPathsBySimpleName = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A collection of <see cref="IAnalyzerAssemblyResolver"/>s that can be used to override the assembly resolution process.
        /// </summary>
        /// <remarks>
        /// When multiple resolvers are present they are consulted in-order, with the first resolver to return a non-null
        /// <see cref="Assembly"/> winning.</remarks>
        private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers;

        /// <summary>
        /// Whether or not we're disposed.  Once disposed, all functionality on this type should throw.
        /// </summary>
        private bool _isDisposed;

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

        private void CheckIfDisposed()
        {
#if NET
            ObjectDisposedException.ThrowIf(_isDisposed, this);
#else
            if (_isDisposed)
                throw new ObjectDisposedException(this.GetType().FullName);
#endif
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            DisposeWorker();
        }

        private partial void DisposeWorker();

        internal bool IsAnalyzerDependencyPath(string fullPath)
        {
            CheckIfDisposed();

            lock (_guard)
            {
                return _analyzerAssemblyInfoMap.ContainsKey(fullPath);
            }
        }

        public void AddDependencyLocation(string fullPath)
        {
            CheckIfDisposed();

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

                // This type assumes the file system is static for the duration of the
                // it's instance. Repeated calls to this method, even if the underlying 
                // file system contents, should reuse the results of the first call.
                _ = _analyzerAssemblyInfoMap.TryAdd(fullPath, null);
            }
        }

        public Assembly LoadFromPath(string originalAnalyzerPath)
        {
            CheckIfDisposed();

            CompilerPathUtilities.RequireAbsolutePath(originalAnalyzerPath, nameof(originalAnalyzerPath));

            (AssemblyName? assemblyName, _) = GetAssemblyInfoForPath(originalAnalyzerPath);

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
            CheckIfDisposed();

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
        /// Get the path a satellite assembly should be loaded from for the given original 
        /// analyzer path and culture
        /// </summary>
        /// <remarks>
        /// This is used during assembly resolve for satellite assemblies to determine the
        /// path from where the satellite assembly should be loaded for the specified culture.
        /// This method calls <see cref="PrepareSatelliteAssemblyToLoad"/> to ensure this path
        /// contains the satellite assembly.
        /// </remarks>
        internal string? GetRealSatelliteLoadPath(string originalAnalyzerPath, CultureInfo cultureInfo)
        {
            CheckIfDisposed();

            string? realSatelliteAssemblyPath = null;

            lock (_guard)
            {
                if (_analyzerSatelliteAssemblyRealPaths.TryGetValue((originalAnalyzerPath, cultureInfo), out realSatelliteAssemblyPath))
                {
                    return realSatelliteAssemblyPath;
                }
            }

            var actualCultureName = getSatelliteCultureName(originalAnalyzerPath, cultureInfo);
            if (actualCultureName != null)
            {
                realSatelliteAssemblyPath = PrepareSatelliteAssemblyToLoad(originalAnalyzerPath, actualCultureName);
            }

            lock (_guard)
            {
                _analyzerSatelliteAssemblyRealPaths[(originalAnalyzerPath, cultureInfo)] = realSatelliteAssemblyPath;
            }

            return realSatelliteAssemblyPath;

            // Discover the most specific culture name to use for the specified analyzer path and culture
            static string? getSatelliteCultureName(string originalAnalyzerPath, CultureInfo cultureInfo)
            {
                var path = Path.GetDirectoryName(originalAnalyzerPath)!;
                var resourceFileName = GetSatelliteFileName(Path.GetFileName(originalAnalyzerPath));

                while (cultureInfo != CultureInfo.InvariantCulture)
                {
                    var resourceFilePath = Path.Combine(path, cultureInfo.Name, resourceFileName);

                    if (File.Exists(resourceFilePath))
                    {
                        return cultureInfo.Name;
                    }

                    cultureInfo = cultureInfo.Parent;
                }

                return null;
            }
        }

        public string? GetOriginalDependencyLocation(AssemblyName assemblyName)
        {
            CheckIfDisposed();

            return GetBestPath(assemblyName).BestOriginalPath;
        }
        /// <summary>
        /// Return the best (original, real) path information for loading an assembly with the specified <see cref="AssemblyName"/>.
        /// </summary>
        protected (string? BestOriginalPath, string? BestRealPath) GetBestPath(AssemblyName requestedName)
        {
            CheckIfDisposed();

            if (requestedName.Name is null)
            {
                return (null, null);
            }

            ImmutableHashSet<string>? paths;
            lock (_guard)
            {
                if (!_knownAssemblyPathsBySimpleName.TryGetValue(requestedName.Name, out paths))
                {
                    return (null, null);
                }
            }

            // Sort the candidate paths by ordinal, to ensure determinism with the same inputs if you were to have
            // multiple assemblies providing the same version.
            string? bestRealPath = null;
            string? bestOriginalPath = null;
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
                        return (candidateOriginalPath, candidateRealPath);
                    }

                    if (bestName is null || candidateName.Version > bestName.Version)
                    {
                        bestOriginalPath = candidateOriginalPath;
                        bestRealPath = candidateRealPath;
                        bestName = candidateName;
                    }
                }
            }

            return (bestOriginalPath, bestRealPath);
        }

        protected static string GetSatelliteFileName(string assemblyFileName) =>
            Path.ChangeExtension(assemblyFileName, ".resources.dll");

        /// <summary>
        /// When overridden in a derived class, allows substituting an assembly path after we've
        /// identified the context to load an assembly in, but before the assembly is actually
        /// loaded from disk. This is used to substitute out the original path with the shadow-copied version.
        /// </summary>
        protected abstract string PreparePathToLoad(string assemblyFilePath);

        /// <summary>
        /// When overridden in a derived class, allows substituting a satellite assembly path after we've
        /// identified the context to load a satellite assembly in, but before the satellite assembly is actually
        /// loaded from disk. This is used to substitute out the original path with the shadow-copied version.
        /// </summary>
        protected abstract string PrepareSatelliteAssemblyToLoad(string assemblyFilePath, string cultureName);

        /// <summary>
        /// When <see cref="PreparePathToLoad(string)"/> is overridden this returns the most recent
        /// real path calculated for the <paramref name="originalFullPath"/>
        /// </summary>
        internal string GetRealAnalyzerLoadPath(string originalFullPath)
        {
            CheckIfDisposed();

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
            CheckIfDisposed();

            lock (_guard)
            {
                return _analyzerAssemblyInfoMap
                    .Select(x => (x.Key, x.Value?.RealAssemblyPath ?? ""))
                    .OrderBy(x => x.Key)
                    .ToArray();
            }
        }

        /// <summary>
        /// Iterates the <see cref="_externalResolvers"/> if any, to see if any of them can resolve
        /// the given <see cref="AssemblyName"/> to an <see cref="Assembly"/>.
        /// </summary>
        /// <param name="assemblyName">The name of the assembly to resolve</param>
        /// <returns>An <see langword="assembly"/> if one of the resolvers is successful, or <see langword="null"/></returns>
        internal Assembly? ResolveAssemblyExternally(AssemblyName assemblyName)
        {
            CheckIfDisposed();

            if (!_externalResolvers.IsDefaultOrEmpty)
            {
                foreach (var resolver in _externalResolvers)
                {
                    try
                    {
                        if (resolver.ResolveAssembly(assemblyName) is { } resolvedAssembly)
                        {
                            return resolvedAssembly;
                        }
                    }
                    catch
                    {
                        // Ignore if the external resolver throws
                    }
                }
            }
            return null;
        }
    }
}
