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
    private static string GetPath()
        // Intentionally using the same path that the host uses by default.  We can load from the same shadow copy
        // locations as they're already appropriately isolated.
        => AbstractAnalyzerAssemblyLoaderProvider.GetPath();

    private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader = CreateLoader(
#if NET
        loadContext: null,
#endif
        externalResolvers);

    private static ShadowCopyAnalyzerAssemblyLoader CreateLoader(
#if NET
        AssemblyLoadContext? loadContext,
#endif
        IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return new(
#if NET
            loadContext,
#endif
            GetPath(),
            externalResolvers.ToImmutableArray());
    }

#if NET

    public IAnalyzerAssemblyLoader GetShadowCopyLoader(AssemblyLoadContext? loadContext)
        => loadContext is null
            ? _shadowCopyLoader
            : CreateLoader(loadContext, externalResolvers);

#else

    public IAnalyzerAssemblyLoader GetShadowCopyLoader()
        => _shadowCopyLoader;

#endif
}
