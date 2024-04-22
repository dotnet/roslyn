// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(IAnalyzerAssemblyResolver)), Shared]
    internal class RazorAnalyzerAssemblyResolver : IAnalyzerAssemblyResolver
    {
        internal static Func<AssemblyName, Assembly?>? AssemblyResolver;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorAnalyzerAssemblyResolver()
        {
        }

        public Assembly? ResolveAssembly(AssemblyName assemblyName) => AssemblyResolver?.Invoke(assemblyName);
    }
}
