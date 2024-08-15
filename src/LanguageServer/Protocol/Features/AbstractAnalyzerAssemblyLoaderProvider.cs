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
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers;

    public AbstractAnalyzerAssemblyLoaderProvider(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _shadowCopyLoader = new Lazy<IAnalyzerAssemblyLoader>(() => CreateShadowCopyLoader(isolatedRoot: ""));
        _externalResolvers = externalResolvers;
    }

    public IAnalyzerAssemblyLoader GetShadowCopyLoader(string isolatedRoot = "")
        => isolatedRoot == "" ? _shadowCopyLoader.Value : CreateShadowCopyLoader(isolatedRoot);

    protected virtual IAnalyzerAssemblyLoader CreateShadowCopyLoader(string isolatedRoot)
    {
        return DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader", isolatedRoot),
            _externalResolvers);
    }
}
