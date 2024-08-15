// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// Customizes the path where to store shadow-copies of analyzer assemblies.
/// </summary>
[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.RemoteWorkspace]), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteAnalyzerAssemblyLoaderService(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : IAnalyzerAssemblyLoaderProvider
{
#if NET
    private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader =
        CreateLoader(loadContext: null, isolatedRoot: "", externalResolvers);

    private static ShadowCopyAnalyzerAssemblyLoader CreateLoader(
        AssemblyLoadContext? loadContext, string isolatedRoot, IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return new(loadContext, Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader", isolatedRoot), externalResolvers.ToImmutableArray());
    }

    public IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext, string isolatedRoot)
    {
        if (loadContext is null && isolatedRoot is "")
            return _shadowCopyLoader;

        return CreateLoader(loadContext, isolatedRoot, externalResolvers);
    }
#else
    private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader =
        CreateLoader(isolatedRoot: "", externalResolvers);

    private static ShadowCopyAnalyzerAssemblyLoader CreateLoader(
        string isolatedRoot, IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return new(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader", isolatedRoot), externalResolvers.ToImmutableArray());
    }

    public IAnalyzerAssemblyLoader GetShadowCopyLoader(string isolatedRoot)
        => isolatedRoot == "" ? _shadowCopyLoader : CreateLoader(isolatedRoot, externalResolvers);
#endif
}
