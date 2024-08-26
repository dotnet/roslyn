// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Abstract implementation of an anlyzer assembly loader that can be used by VS/VSCode to provide a
/// <see cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly Lazy<IAnalyzerAssemblyLoader> _shadowCopyLoader;
    private readonly Lazy<IAnalyzerAssemblyLoader> _directLoader;

    public AbstractAnalyzerAssemblyLoaderProvider(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _shadowCopyLoader = new Lazy<IAnalyzerAssemblyLoader>(() => CreateShadowCopyLoader(externalResolvers));
        _directLoader = new Lazy<IAnalyzerAssemblyLoader>(() => CreateDirectLoader(externalResolvers));
    }

    public IAnalyzerAssemblyLoader GetShadowCopyLoader()
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoader GetDirectLoader()
        => _directLoader.Value;

    protected virtual IAnalyzerAssemblyLoader CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"),
            externalResolvers: externalResolvers);
    }

    protected virtual IAnalyzerAssemblyLoader CreateDirectLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return new DefaultAnalyzerAssemblyLoader(externalResolvers);
    }
}
