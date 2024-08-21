// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
#if NET
    /// <summary>
    /// In .Net core, gives back a fresh shadow copying loader that will load all <see cref="AnalyzerReference"/> in the
    /// requested <paramref name="loadContext"/> (or a default one if <see langword="null"/> is passed in).
    /// </summary>
    IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext);
#else
    /// <summary>
    /// In .Net Framework, returns a single shadow copy loader which will be used to load all <see
    /// cref="AnalyzerReference"/>s.  On .Net framework there are no assembly load contexts, so no isolation or
    /// reloading of references is possible.
    /// </summary>
    IAnalyzerAssemblyLoader GetShadowCopyLoader();
#endif
}

internal static class IAnalyzerAssemblyLoaderProviderExtensions
{
    public static IAnalyzerAssemblyLoader GetDefaultShadowCopyLoader(this IAnalyzerAssemblyLoaderProvider provider)
    {
#if NET
        return provider.GetShadowCopyLoader(loadContext: null);
#else
        return provider.GetShadowCopyLoader();
#endif
    }
}
