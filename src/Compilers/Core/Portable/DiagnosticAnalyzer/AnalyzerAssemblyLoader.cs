// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.ErrorReporting;
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
    /// Instances of this type will be accessed from multiple threads. All method implementations are expected 
    /// to be idempotent.
    /// </summary>
    internal interface IAnalyzerPathResolver
    {
        /// <summary>
        /// Is this path handled by this instance?
        /// </summary>
        bool IsAnalyzerPathHandled(string analyzerPath);

        /// <summary>
        /// This method is used to allow compiler hosts to intercept an analyzer path and redirect it to a
        /// a different location.
        /// </summary>
        /// <remarks>
        /// This will only be called for paths that return true from <see cref="IsAnalyzerPathHandled(string)"/>.
        /// </remarks>
        string GetRealAnalyzerPath(string analyzerPath);

        /// <summary>
        /// This method is used to allow compiler hosts to intercept an analyzer satellite path and redirect it to a
        /// a different location.
        /// </summary>
        /// <remarks>
        /// This will only be called for paths that return true from <see cref="IsAnalyzerPathHandled(string)"/>.
        /// </remarks>
        string? GetRealSatellitePath(string analyzerPath, CultureInfo cultureInfo);
    }

    /// <summary>
    /// The base implementation for <see cref="IAnalyzerAssemblyLoader"/>. This type provides caching and tracking of inputs given
    /// to <see cref="AddDependencyLocation(string)"/>.
    /// </summary>
    /// <remarks>
    /// This type generally assumes that files on disk aren't changing, since it ensure that two calls to <see cref="LoadFromPath(string)"/>
    /// will always return the same thing, per that interface's contract.
    /// </remarks>
    internal sealed partial class AnalyzerAssemblyLoader : IAnalyzerAssemblyLoaderInternal
    {
        private readonly object _guard = new();

        /// <summary>
        /// This is a map between the original full path and what <see cref="IAnalyzerPathResolver"/>, if
        /// any, handles it.
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, (IAnalyzerPathResolver? Resolver, string RealPath, AssemblyName? AssemblyName)> _originalPathInfoMap = new();

        /// <summary>
        /// This is a map between assembly simple names and the collection of original paths that map to them
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, HashSet<string>> _assemblySimpleNameToOriginalPathListMap = new();

        /// <summary>
        /// Map from real paths to the original ones
        /// </summary>
        /// <remarks>
        /// Access must be guarded by <see cref="_guard"/>
        /// </remarks>
        private readonly Dictionary<string, string> _realToOriginalPathMap = new();

        /// <summary>
        /// Whether or not we're disposed.  Once disposed, all functionality on this type should throw.
        /// </summary>
        private bool _isDisposed;

        public ImmutableArray<IAnalyzerPathResolver> AnalyzerPathResolvers { get; }

        /// <summary>
        /// The implementation needs to load an <see cref="Assembly"/> with the specified <see cref="AssemblyName"/> from
        /// the specified path.
        /// </summary>
        /// <remarks>
        /// This method should return an <see cref="Assembly"/> instance or throw.
        /// </remarks>
        private partial Assembly Load(AssemblyName assemblyName, string assemblyRealPath);

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

        public void AddDependencyLocation(string originalPath)
        {
            CheckIfDisposed();

            CompilerPathUtilities.RequireAbsolutePath(originalPath, nameof(originalPath));
            var simpleName = PathUtilities.GetFileName(originalPath, includeExtension: false);
            string realPath = originalPath;
            IAnalyzerPathResolver? resolver = null;
            foreach (var current in AnalyzerPathResolvers)
            {
                if (current.IsAnalyzerPathHandled(originalPath))
                {
                    resolver = current;
                    realPath = resolver.GetRealAnalyzerPath(originalPath);
                    break;
                }
            }

            var assemblyName = readAssemblyName(realPath);

            lock (_guard)
            {
                if (_originalPathInfoMap.TryAdd(originalPath, (resolver, realPath, assemblyName)))
                {
                    // In the case multiple original paths map to the same real path then the first on
                    // wins.
                    //
                    // An example reason to map multiple original paths to the same real path would be to
                    // unify references.
                    _realToOriginalPathMap.TryAdd(realPath, originalPath);

                    if (!_assemblySimpleNameToOriginalPathListMap.TryGetValue(simpleName, out var set))
                    {
                        set = new();
                        _assemblySimpleNameToOriginalPathListMap[simpleName] = set;
                    }

                    _ = set.Add(originalPath);
                }
            }

            static AssemblyName? readAssemblyName(string filePath)
            {
                AssemblyName? assemblyName;
                try
                {
                    assemblyName = AssemblyName.GetAssemblyName(filePath);
                }
                catch
                {
                    // The above can fail when the assembly doesn't exist because it's corrupted, 
                    // doesn't exist on disk, or is a native DLL. Those failures are handled when 
                    // the actual load is attempted. Just record the failure now.
                    assemblyName = null;
                }

                return assemblyName;
            }
        }

        /// <summary>
        /// Called from the consumer of <see cref="AnalyzerAssemblyLoader"/> to load an analyzer assembly from disk. It
        /// should _not_ be called from the implementation.
        /// </summary>
        public Assembly LoadFromPath(string originalPath)
        {
            CheckIfDisposed();

            CompilerPathUtilities.RequireAbsolutePath(originalPath, nameof(originalPath));
            var (realPath, assemblyName) = GetRealAnalyzerPathAndName(originalPath);
            if (assemblyName is null)
            {
                // Not a managed assembly, nothing else to do
                throw new ArgumentException($"Not a valid assembly: {originalPath}");
            }

            try
            {
                return Load(assemblyName, realPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to load {assemblyName.Name}: {ex.Message}", ex);
            }
        }

        public (string RealPath, AssemblyName? AssemblyName) GetRealAnalyzerPathAndName(string originalPath)
        {
            lock (_guard)
            {
                if (!_originalPathInfoMap.TryGetValue(originalPath, out var info))
                {
                    throw new ArgumentException("Path not registered: " + originalPath, nameof(originalPath));
                }

                return (info.RealPath, info.AssemblyName);
            }
        }

        public string GetRealAnalyzerPath(string originalPath) =>
            GetRealAnalyzerPathAndName(originalPath).RealPath;

        public string? GetRealSatellitePath(string originalPath, CultureInfo cultureInfo)
        {
            CheckIfDisposed();

            IAnalyzerPathResolver? resolver;
            lock (_guard)
            {
                if (!_originalPathInfoMap.TryGetValue(originalPath, out var info))
                {
                    throw new ArgumentException("Path not registered: " + originalPath, nameof(originalPath));
                }

                resolver = info.Resolver;
            }

            if (resolver is not null)
            {
                return resolver.GetRealSatellitePath(originalPath, cultureInfo);
            }

            return GetSatelliteAssemblyPath(originalPath, cultureInfo);
        }

        /// <summary>
        /// Get the path a satellite assembly should be loaded from for the given real 
        /// analyzer path and culture
        /// </summary>
        private string? GetSatelliteLoadPath(string analyzerFilePath, CultureInfo cultureInfo)
        {
            string? originalPath;

            lock (_guard)
            {
                if (!_realToOriginalPathMap.TryGetValue(analyzerFilePath, out originalPath))
                {
                    return null;
                }
            }

            return GetRealSatellitePath(originalPath, cultureInfo);
        }

        /// <summary>
        /// This method mimics the .NET lookup rules for sattelite assemblies and will return the ideal
        /// resource assembly for the given culture.
        /// </summary>
        internal static string? GetSatelliteAssemblyPath(string assemblyFilePath, CultureInfo cultureInfo)
        {
            var assemblyFileName = Path.GetFileName(assemblyFilePath);
            var satelliteAssemblyName = Path.ChangeExtension(assemblyFileName, ".resources.dll");
            var path = Path.GetDirectoryName(assemblyFilePath);
            if (path is null)
            {
                return null;
            }

            while (cultureInfo != CultureInfo.InvariantCulture)
            {
                var filePath = Path.Combine(path, cultureInfo.Name, satelliteAssemblyName);
                if (File.Exists(filePath))
                {
                    return filePath;
                }

                cultureInfo = cultureInfo.Parent;
            }

            return null;
        }

        public string? GetOriginalDependencyLocation(AssemblyName assemblyName)
        {
            CheckIfDisposed();

            return GetBestPath(assemblyName).BestOriginalPath;
        }

        /// <summary>
        /// Return the best (original, real) path information for loading an assembly with the specified <see cref="AssemblyName"/>.
        /// </summary>
        private (string? BestOriginalPath, string? BestRealPath) GetBestPath(AssemblyName requestedName)
        {
            CheckIfDisposed();

            if (requestedName.Name is null)
            {
                return (null, null);
            }

            var originalPaths = new List<string>();
            lock (_guard)
            {
                if (!_assemblySimpleNameToOriginalPathListMap.TryGetValue(requestedName.Name, out var set))
                {
                    return (null, null);
                }

                originalPaths = set.OrderBy(x => x).ToList();
            }

            string? bestRealPath = null;
            string? bestOriginalPath = null;
            AssemblyName? bestName = null;
            foreach (var candidateOriginalPath in originalPaths)
            {
                var (candidateRealPath, candidateName) = GetRealAnalyzerPathAndName(candidateOriginalPath);
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

        internal (string OriginalAssemblyPath, string RealAssemblyPath)[] GetPathMapSnapshot()
        {
            CheckIfDisposed();

            lock (_guard)
            {
                return _realToOriginalPathMap.Select(x => (x.Value, x.Key)).ToArray();
            }
        }
    }
}
