// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET

using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This interface allows hosts to control exactly how a given <see cref="AssemblyName"/> is resolved to an
    /// <see cref="Assembly"/> instance. This is useful for hosts that need to load assemblies in a custom way like
    /// Razor or stream based loading.
    /// </summary>
    internal interface IAnalyzerAssemblyResolver
    {
        /// <summary>
        /// Resolve an <see cref="Assembly"/> for the given parameters.
        /// </summary>
        /// <remarks>
        /// The <see cref="AnalyzerAssemblyLoader"/> will partition analyzers into the directories they live
        /// in and will create a separate <see cref="AssemblyLoadContext"/> for each directory. That instance
        /// and the directory name represent <paramref name="directoryContext" /> and <paramref name="directory" />.
        /// 
        /// This is invoked as part of <see cref="AssemblyLoadContext.Load(AssemblyName)"/>. Exceptions in
        /// the implementation of this interface will escape from that method and be registered as the result
        /// of load.
        /// </remarks>
        /// <param name="loader">The <see cref="AnalyzerAssemblyLoader"/> instance that is performing the load</param>
        /// <param name="assemblyName">The name of the assembly to be loaded</param>
        /// <param name="directoryContext">The <see cref="AssemblyLoadContext"/> for the <paramref name="directory"/></param>
        /// <param name="directory">The resolved directory where the assembly is being loaded from</param>
        /// <returns>The <see cref="Assembly"/> resolved or null if it's not handled by this instance</returns>
        Assembly? Resolve(AnalyzerAssemblyLoader loader, AssemblyName assemblyName, AssemblyLoadContext directoryContext, string directory);
    }
}

#endif
