// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using System.Reflection;


#if NET
using Microsoft.CodeAnalysis.Diagnostics;
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
#if NET

    /// <summary>
    /// This loader will throw if it is asked to load a reference
    /// </summary>
    IAnalyzerAssemblyLoader FailingLoader { get; }

    /// <summary>
    /// Creates a fresh shadow copying loader that will load all <see cref="AnalyzerReference"/>s and <see
    /// cref="ISourceGenerator"/>s in a fresh <see cref="AssemblyLoadContext"/>.
    /// </summary>
    IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader();

#else

    IAnalyzerAssemblyLoader SharedShadowCopyLoader { get; }

#endif
}

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
#if NET
    private sealed class NoLoadLoader : IAnalyzerAssemblyLoader
    {
        public static readonly NoLoadLoader Instance = new();

        public void AddDependencyLocation(string fullPath)
        {
        }

        public Assembly LoadFromPath(string fullPath)
        {
            throw new NotImplementedException();
        }
    }

    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _assemblyResolvers;

    public AbstractAnalyzerAssemblyLoaderProvider(IEnumerable<IAnalyzerAssemblyResolver> assemblyResolvers)
    {
        _assemblyResolvers = [.. assemblyResolvers];
    }

    public IAnalyzerAssemblyLoader FailingLoader => NoLoadLoader.Instance;

    public IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => this.WrapLoader(AnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                pathResolvers: default,
                _assemblyResolvers));
#else

    private readonly IAnalyzerAssemblyLoaderInternal _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider()
    {
        _shadowCopyLoader = WrapLoader(AnalyzerAssemblyLoader.CreateNonLockingLoader(
                Path.Combine(Path.GetTempPath(), nameof(Roslyn), "AnalyzerAssemblyLoader"),
                pathResolvers: default));
    }

    public IAnalyzerAssemblyLoader SharedShadowCopyLoader
        => _shadowCopyLoader;

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
