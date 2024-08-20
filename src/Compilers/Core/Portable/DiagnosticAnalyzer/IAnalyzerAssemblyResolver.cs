// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Allows a host to override how assembly resolution is performed by the <see cref="AnalyzerAssemblyLoader"/>.
    /// </summary>
    public interface IAnalyzerAssemblyResolver
    {
        /// <summary>
        /// Attempts to resolve an assembly by name.
        /// </summary>
        /// <param name="assemblyName">The assembly to resolve</param>
        /// <returns>The resolved assembly, or <see langword="null"/></returns>
        Assembly? ResolveAssembly(AssemblyName assemblyName, string assemblyOriginalDirectory);
    }
}
