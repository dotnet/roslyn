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

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
    IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader { get; }

    IAnalyzerAssemblyLoaderInternal SharedDirectLoader { get; }
}

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers;
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _directLoader;

    public AbstractAnalyzerAssemblyLoaderProvider(IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        _externalResolvers = externalResolvers.ToImmutableArray();
        _shadowCopyLoader = new(CreateShadowCopyLoader);
        _directLoader = new(CreateDirectLoader);
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoaderInternal SharedDirectLoader
        => _directLoader.Value;

    private IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader()
        => this.WrapLoader(DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                _externalResolvers));

    private IAnalyzerAssemblyLoaderInternal CreateDirectLoader()
        => this.WrapLoader(new DefaultAnalyzerAssemblyLoader(_externalResolvers));

    protected virtual IAnalyzerAssemblyLoaderInternal WrapLoader(IAnalyzerAssemblyLoaderInternal loader)
        => loader;
}

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderProvider(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : AbstractAnalyzerAssemblyLoaderProvider(externalResolvers);
