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
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _directLoader;

    public AbstractAnalyzerAssemblyLoaderProvider(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _shadowCopyLoader = new(() => CreateShadowCopyLoader(externalResolvers));
        _directLoader = new(() => CreateDirectLoader(externalResolvers));
    }

    public IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader()
        => _shadowCopyLoader.Value;

    public IAnalyzerAssemblyLoaderInternal GetDirectLoader()
        => _directLoader.Value;

    protected virtual IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        => DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            GetDefaultShadowCopyPath(),
            externalResolvers: externalResolvers);

    public static string GetDefaultShadowCopyPath()
        => Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader");

    protected virtual IAnalyzerAssemblyLoaderInternal CreateDirectLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        => new DefaultAnalyzerAssemblyLoader(externalResolvers);
}
