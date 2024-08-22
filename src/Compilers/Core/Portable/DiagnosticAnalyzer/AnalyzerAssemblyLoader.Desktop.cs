// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if !NETCOREAPP

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
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
    internal partial class AnalyzerAssemblyLoader
    {
        private bool _hookedAssemblyResolve;

        internal AnalyzerAssemblyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        {
            _externalResolvers = externalResolvers;
        }

        public bool IsHostAssembly(Assembly assembly)
        {
            CheckIfDisposed();

            // When an assembly is loaded from the GAC then the load result would be the same if 
            // this ran on command line compiler. So there is no consistency issue here, this 
            // is just runtime rules expressing themselves.
            if (assembly.GlobalAssemblyCache)
            {
                return true;
            }

            // When an assembly is loaded from the compiler directory then this means it's assembly
            // binding redirects taking over. For example it's moving from an older version of System.Memory
            // to the one shipping in the compiler. This is not a consistency issue.
            var compilerDirectory = Path.GetDirectoryName(typeof(AnalyzerAssemblyLoader).Assembly.Location);
            if (PathUtilities.Comparer.Equals(compilerDirectory, Path.GetDirectoryName(assembly.Location)))
            {
                return true;
            }

            return false;
        }

        private partial Assembly? Load(AssemblyName assemblyName, string assemblyOriginalPath)
        {
            EnsureResolvedHooked();
            if (ResolveAssemblyExternally(assemblyName) is { } externallyResolvedAssembly)
            {
                return externallyResolvedAssembly;
            }

            return AppDomain.CurrentDomain.Load(assemblyName);
        }

        private partial bool IsMatch(AssemblyName requestedName, AssemblyName candidateName) =>
            candidateName.Name == requestedName.Name &&
            candidateName.Version >= requestedName.Version &&
            candidateName.GetPublicKeyToken().AsSpan().SequenceEqual(requestedName.GetPublicKeyToken().AsSpan());

        internal bool EnsureResolvedHooked()
        {
            CheckIfDisposed();

            lock (_guard)
            {
                if (!_hookedAssemblyResolve)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolve;
                    _hookedAssemblyResolve = true;
                    return true;
                }
            }

            return false;
        }

        internal bool EnsureResolvedUnhooked()
        {
            CheckIfDisposed();

            lock (_guard)
            {
                if (_hookedAssemblyResolve)
                {
                    AppDomain.CurrentDomain.AssemblyResolve -= AssemblyResolve;
                    _hookedAssemblyResolve = false;
                    return true;
                }
            }

            return false;
        }

        private Assembly? AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                const string resourcesExtension = ".resources";
                var assemblyName = new AssemblyName(args.Name);
                var simpleName = assemblyName.Name;
                var isSatelliteAssembly =
                    assemblyName.CultureInfo is not null &&
                    simpleName.EndsWith(resourcesExtension, StringComparison.Ordinal);

                if (isSatelliteAssembly)
                {
                    // Satellite assemblies should get the best path information using the
                    // non-resource part of the assembly name. Once the path information is obtained
                    // GetSatelliteInfoForPath will translate to the resource assembly path.
                    assemblyName.Name = simpleName[..^resourcesExtension.Length];
                }

                var (originalPath, realPath) = GetBestPath(assemblyName);
                if (isSatelliteAssembly && originalPath is not null)
                {
                    realPath = GetRealSatelliteLoadPath(originalPath, assemblyName.CultureInfo);
                }

                if (realPath is not null)
                {
                    return Assembly.LoadFrom(realPath);
                }

                return null;
            }
            catch
            {
                // In the .NET Framework, if a handler to AssemblyResolve throws an exception, other handlers
                // are not called. To avoid any bug in our handler breaking other handlers running in the same process
                // we catch exceptions here. We do not expect exceptions to be thrown though.
                return null;
            }
        }
    }
}

#endif
