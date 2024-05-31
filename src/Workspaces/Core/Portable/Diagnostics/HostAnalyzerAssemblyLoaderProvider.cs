// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Similar to <see cref="DefaultAnalyzerAssemblyLoader"/> but uses a path appropriate for VS/VSCode host workspace.
/// </summary>
[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.Host]), Shared]
internal class HostAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly DefaultAnalyzerAssemblyLoader _loader;

    private readonly IAnalyzerAssemblyLoader _shadowCopyLoader;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public HostAnalyzerAssemblyLoaderProvider([ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        var resolvers = externalResolvers.ToImmutableArray();
        _loader = new(resolvers);
        _shadowCopyLoader = DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"),
            externalResolvers: resolvers);
    }

    public IAnalyzerAssemblyLoader GetLoader(bool shadowCopy)
        => shadowCopy ? _shadowCopyLoader : _loader;
}
