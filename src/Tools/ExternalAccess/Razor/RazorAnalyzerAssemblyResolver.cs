// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(IAnalyzerAssemblyResolver)), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class RazorAnalyzerAssemblyResolver() : IAnalyzerAssemblyResolver
    {
        private static Func<AssemblyName, Assembly?>? s_assemblyResolver;

        private static readonly HashSet<AssemblyName> s_assembliesRequested = [];

        private static readonly object s_resolverLock = new();

        /// <summary>
        /// Attempts to set the assembly resolver. Will only succeed if the <paramref name="canaryAssembly"/> has not already been requested to load and the resolver has not been set previously.
        /// </summary>
        /// <param name="resolver">The resolver function to set.</param>
        /// <param name="canaryAssembly">The name of an assembly that is checked to see if it has been requested to load. Setting the resolver will fail if this has already been requested.</param>
        /// <returns><c>true</c> when the resolver was successfully set.</returns>
        internal static bool TrySetAssemblyResolver(Func<AssemblyName, Assembly?> resolver, AssemblyName canaryAssembly)
        {
            lock (s_resolverLock)
            {
                if (s_assemblyResolver is null && !s_assembliesRequested.Contains(canaryAssembly))
                {
                    s_assemblyResolver = resolver;
                    return true;
                }
                return false;
            }
        }

        public Assembly? ResolveAssembly(AssemblyName assemblyName)
        {
            Func<AssemblyName, Assembly?>? resolver = null;
            lock (s_resolverLock)
            {
                s_assembliesRequested.Add(assemblyName);
                resolver = s_assemblyResolver;
            }

            return resolver?.Invoke(assemblyName);
        }
    }
}
