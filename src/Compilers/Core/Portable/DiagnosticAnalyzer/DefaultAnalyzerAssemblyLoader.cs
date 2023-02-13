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
#if NET6_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCode(TrimWarningMessages.AnalyzerReflectionLoadMessage)]
#endif
    internal sealed class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
#if NETCOREAPP

        // Called from a netstandard2.0 project, so need to ensure a parameterless constructor is available.
        internal DefaultAnalyzerAssemblyLoader()
            : this(null)
        {
        }

        internal DefaultAnalyzerAssemblyLoader(System.Runtime.Loader.AssemblyLoadContext? compilerLoadContext = null)
            : base(compilerLoadContext)
        {
        }

#endif

        /// <summary>
        /// The default implementation is to simply load in place.
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        protected override string PreparePathToLoad(string fullPath) => fullPath;
    }
}
