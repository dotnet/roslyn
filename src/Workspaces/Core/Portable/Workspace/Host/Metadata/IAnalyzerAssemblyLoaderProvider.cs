// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// This is an implementation of <see cref="IAnalyzerAssemblyLoader"/> which will never 
/// actually load the <see cref="Assembly"/>. It is used in places where we need to create
/// a <see cref="AnalyzerReference"/> for other purposes, like communicating the underlying
/// file path, but do not actually want to load it.
/// </summary>
internal sealed class NoLoadAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    internal static NoLoadAnalyzerAssemblyLoader Instance { get; } = new();

    private NoLoadAnalyzerAssemblyLoader()
    {
    }

    public void AddDependencyLocation(string fullPath)
    {
    }

    public Assembly LoadFromPath(string fullPath) =>
        throw ExceptionUtilities.Unreachable();
}

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
#if NET

    /// <summary>
    /// Creates a fresh shadow copying loader that will load all <see cref="AnalyzerReference"/>s and <see
    /// cref="ISourceGenerator"/>s in a fresh <see cref="AssemblyLoadContext"/>.
    /// </summary>
    IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader();

#else

    /// <summary>
    /// This is the shared instance that should be used by all <see cref="AnalyzerReference"/> in the 
    /// current process.
    /// </summary>
    /// <remarks>
    /// This is not available in .NET Core because there is no single shared loader and instead we 
    /// partition into AssemblyLoadContext based on the usage context.
    /// </remarks>
    IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader { get; }

#endif
}

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
#if NET
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _assemblyResolvers;

    public AbstractAnalyzerAssemblyLoaderProvider(IEnumerable<IAnalyzerAssemblyResolver> assemblyResolvers)
    {
        _assemblyResolvers = [.. assemblyResolvers];
    }

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => this.WrapLoader(AnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                pathResolvers: default,
                _assemblyResolvers));
#else

    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider()
    {
        _shadowCopyLoader = new(CreateNewShadowCopyLoader);
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader =>
        _shadowCopyLoader.Value;

    private IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
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
    public DefaultAnalyzerAssemblyLoaderProvider([ImportMany] IEnumerable<IAnalyzerAssemblyResolver> assemblyResolvers)
        : base(assemblyResolvers)
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
