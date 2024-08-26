﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics;

/// <summary>
/// Customizes the path where to store shadow-copies of analyzer assemblies.
/// </summary>
[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.RemoteWorkspace]), Shared]
internal sealed class RemoteAnalyzerAssemblyLoaderService : IAnalyzerAssemblyLoaderProvider
{
    private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader;
    private readonly DefaultAnalyzerAssemblyLoader _directLoader;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public RemoteAnalyzerAssemblyLoaderService(
        [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        var externalResolversImmutable = externalResolvers.ToImmutableArray();
        _shadowCopyLoader = new(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"), externalResolversImmutable);
        _directLoader = new(externalResolversImmutable);
    }

    public IAnalyzerAssemblyLoader GetShadowCopyLoader()
        => _shadowCopyLoader;

    public IAnalyzerAssemblyLoader GetDirectLoader()
        => _directLoader;
}
