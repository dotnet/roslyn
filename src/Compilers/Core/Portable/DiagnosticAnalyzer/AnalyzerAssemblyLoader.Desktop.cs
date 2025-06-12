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
    internal partial class AnalyzerAssemblyLoader
    {
        private bool _hookedAssemblyResolve;

        internal AnalyzerAssemblyLoader()
         : this(analyzerPathResolvers: [])
        {
        }

        internal AnalyzerAssemblyLoader(ImmutableArray<IAnalyzerPathResolver> analyzerPathResolvers)
        {
            AnalyzerPathResolvers = analyzerPathResolvers;
        }

        private partial void DisposeWorker()
        {
            EnsureResolvedUnhooked();
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

        private partial Assembly? Load(AssemblyName assemblyName, string resolvedPath)
        {
            EnsureResolvedHooked();

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
            // Called from Dispose. We don't want to throw if we're disposed.

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

                string? loadPath;
                if (assemblyName.CultureInfo is not null && simpleName.EndsWith(resourcesExtension, SimpleNameComparer.Comparison))
                {
                    // Satellite assemblies should get the best path information using the
                    // non-resource part of the assembly name. Once the path information is obtained
                    // GetSatelliteLoadPath will translate to the resource assembly path.
                    assemblyName.Name = simpleName[..^resourcesExtension.Length];
                    var (_, resolvedPath) = GetBestResolvedPath(assemblyName);
                    loadPath = resolvedPath is not null ? GetSatelliteLoadPath(resolvedPath, assemblyName.CultureInfo) : null;
                }
                else
                {
                    (_, loadPath) = GetBestResolvedPath(assemblyName);
                }

                if (loadPath is not null)
                {
                    return Assembly.LoadFrom(loadPath);
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
