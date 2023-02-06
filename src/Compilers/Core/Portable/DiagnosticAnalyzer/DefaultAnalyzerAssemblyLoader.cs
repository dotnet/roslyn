
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
    internal sealed class DefaultAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
#if NETCOREAPP

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