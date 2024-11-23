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
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        internal DefaultAnalyzerAssemblyLoader()
            : base([])
        {
        }

        internal DefaultAnalyzerAssemblyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
            : base(externalResolvers)
        {
        }

#if NET

        internal DefaultAnalyzerAssemblyLoader(System.Runtime.Loader.AssemblyLoadContext? compilerLoadContext = null, AnalyzerLoadOption loadOption = AnalyzerLoadOption.LoadFromDisk, ImmutableArray<IAnalyzerAssemblyResolver>? externalResolvers = null)
            : base(compilerLoadContext, loadOption, externalResolvers ?? [])
        {
        }

#endif

        /// <summary>
        /// The default implementation is to simply load in place.
        /// </summary>
        protected override string PreparePathToLoad(string fullPath) => fullPath;

        /// <summary>
        /// The default implementation is to simply load in place.
        /// </summary>
        protected override string PrepareSatelliteAssemblyToLoad(string assemblyFilePath, string cultureName)
        {
            var directory = Path.GetDirectoryName(assemblyFilePath)!;
            var fileName = GetSatelliteFileName(Path.GetFileName(assemblyFilePath));

            return Path.Combine(directory, cultureName, fileName);
        }

        /// <summary>
        /// Return an <see cref="IAnalyzerAssemblyLoader"/> which does not lock assemblies on disk that is
        /// most appropriate for the current platform.
        /// </summary>
        /// <param name="windowsShadowPath">A shadow copy path will be created on Windows and this value 
        /// will be the base directory where shadow copy assemblies are stored. </param>
        internal static IAnalyzerAssemblyLoaderInternal CreateNonLockingLoader(string windowsShadowPath, ImmutableArray<IAnalyzerAssemblyResolver>? externalResolvers = null)
        {
#if NET
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new DefaultAnalyzerAssemblyLoader(loadOption: AnalyzerLoadOption.LoadFromStream, externalResolvers: externalResolvers);
            }
#endif

            // The shadow copy analyzer should only be created on Windows. To create on Linux we cannot use 
            // GetTempPath as it's not per-user. Generally there is no need as LoadFromStream achieves the same
            // effect
            if (!Path.IsPathRooted(windowsShadowPath))
            {
                throw new ArgumentException("Must be a full path.", nameof(windowsShadowPath));
            }

            return new ShadowCopyAnalyzerAssemblyLoader(windowsShadowPath, externalResolvers);
        }
    }
}
