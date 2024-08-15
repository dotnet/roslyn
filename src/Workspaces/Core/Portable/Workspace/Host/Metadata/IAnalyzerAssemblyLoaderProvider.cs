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
    /// In .Net core, gives back a shadow copying loader that will load all <see cref="AnalyzerReference"/> in the
    /// requested <paramref name="loadContext"/> (or a default one if unspecified).  <paramref name="isolatedRoot"/> can
    /// be used to ensure a dedicated directory for the shadow copying to happen in, to prevent any collisions
    /// whatsoever.  For example, given two <see cref="AnalyzerReference"/>s with the same MVID, but loaded into
    /// different <see cref="AssemblyLoadContext"/>s; different paths would be desired to prevent collisions.
    /// </summary>
    IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext = null, string isolatedRoot = "");
#else
    /// <summary>
    /// In .Net Framework, returns a single shadow copuy loader which will be used to load all <see
    /// cref="AnalyzerReference"/>s.  On .Net framework there are no assembly load contexts, so no isolation or
    /// reloading of references is possible.
    /// </summary>
    IAnalyzerAssemblyLoader GetShadowCopyLoader();
#endif
}
