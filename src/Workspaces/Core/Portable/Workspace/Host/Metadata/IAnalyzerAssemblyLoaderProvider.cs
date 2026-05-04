// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

#if NET
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
    IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader { get; }

#if NET
    /// <summary>
    /// Creates a fresh shadow copying loader that will load all <see cref="AnalyzerReference"/>s and <see
    /// cref="ISourceGenerator"/>s in a fresh <see cref="AssemblyLoadContext"/>.
    /// </summary>
    IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader();
#endif
}

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
#if NET
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _assemblyResolvers;
    private readonly ImmutableArray<IAnalyzerPathResolver> _assemblyPathResolvers;

    public AbstractAnalyzerAssemblyLoaderProvider(IEnumerable<IAnalyzerAssemblyResolver> assemblyResolvers, IEnumerable<IAnalyzerPathResolver> assemblyPathResolvers)
    {
        _assemblyResolvers = [.. assemblyResolvers];
        _shadowCopyLoader = new(CreateNewShadowCopyLoader);
        _assemblyPathResolvers = [.. assemblyPathResolvers];
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => this.WrapLoader(AnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                _assemblyPathResolvers,
                _assemblyResolvers));
#else
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider()
    {
        _shadowCopyLoader = new(CreateNewShadowCopyLoader);
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => this.WrapLoader(AnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                pathResolvers: default));
#endif

    protected virtual IAnalyzerAssemblyLoaderInternal WrapLoader(IAnalyzerAssemblyLoaderInternal loader)
        => loader;
}

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
internal sealed class DefaultAnalyzerAssemblyLoaderProvider : AbstractAnalyzerAssemblyLoaderProvider
{
#if NET
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultAnalyzerAssemblyLoaderProvider([ImportMany] IEnumerable<IAnalyzerAssemblyResolver> assemblyResolvers, [ImportMany] IEnumerable<IAnalyzerPathResolver> assemblyPathResolvers)
        : base(assemblyResolvers, assemblyPathResolvers)
    {
    }
#else
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public DefaultAnalyzerAssemblyLoaderProvider()
    {
    }
#endif
}
