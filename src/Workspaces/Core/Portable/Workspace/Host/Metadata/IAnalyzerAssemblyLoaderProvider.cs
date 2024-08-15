// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
#if NET
    IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext = null, string isolatedRoot = "");
#else
    IAnalyzerAssemblyLoader GetShadowCopyLoader();
#endif
}
