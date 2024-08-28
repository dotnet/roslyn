// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(IAnalyzerAssemblyResolver)), Shared]
    internal class RazorAnalyzerAssemblyResolver : IAnalyzerAssemblyResolver
    {
        private static Func<AssemblyName, Assembly?>? s_assemblyResolver;

        /// <summary>
        /// We use this as a heuristic to catch a case where we set the resolver too 
        /// late and the resolver has already been asked to resolve a razor assembly.
        /// 
        /// Note this isn't perfectly accurate but is only used to trigger an assert
        /// in debug builds. 
        /// </summary>
        private static bool s_razorRequested = false;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorAnalyzerAssemblyResolver()
        {
        }

        internal static Func<AssemblyName, Assembly?>? AssemblyResolver
        {
            get => s_assemblyResolver;
            set
            {
                Debug.Assert(s_assemblyResolver == null, "Assembly resolver should not be set multiple times.");
                Debug.Assert(!s_razorRequested, "A razor assembly has already been requested before setting the resolver.");
                s_assemblyResolver = value;
            }
        }

        public string? RedirectPath(string fullPath) => null;

        public Assembly? ResolveAssembly(AssemblyName assemblyName, string assemblyOriginalDirectory)
        {
            s_razorRequested |= assemblyName.FullName.Contains("Razor");
            return s_assemblyResolver?.Invoke(assemblyName);
        }
    }
}
