// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Diagnostics.Redirecting;

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
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers;
    private readonly ImmutableArray<IAnalyzerAssemblyRedirector> _externalRedirectors;
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider(IEnumerable<IAnalyzerAssemblyResolver> externalResolvers, IEnumerable<IAnalyzerAssemblyRedirector> externalRedirectors)
    {
        _externalResolvers = externalResolvers.ToImmutableArray();
        _externalRedirectors = externalRedirectors.ToImmutableArray();
        _shadowCopyLoader = new(CreateNewShadowCopyLoader);
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => this.WrapLoader(DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                _externalResolvers, _externalRedirectors));

    protected virtual IAnalyzerAssemblyLoaderInternal WrapLoader(IAnalyzerAssemblyLoaderInternal loader)
        => loader;
}

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderProvider(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers,
    [ImportMany] IEnumerable<IAnalyzerAssemblyRedirector> externalRedirectors)
    : AbstractAnalyzerAssemblyLoaderProvider(externalResolvers, externalRedirectors);
