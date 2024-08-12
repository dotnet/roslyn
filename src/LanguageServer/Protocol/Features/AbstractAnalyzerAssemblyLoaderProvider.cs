// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Abstract implementation of an anlyzer assembly loader that can be used by VS/VSCode to provide a
/// <see cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly Lazy<IAnalyzerAssemblyLoader> _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider([ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    {
        var resolvers = externalResolvers.ToImmutableArray();

        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _shadowCopyLoader = new Lazy<IAnalyzerAssemblyLoader>(() => CreateShadowCopyLoader(resolvers));
    }

    public IAnalyzerAssemblyLoader GetLoader()
        => _shadowCopyLoader.Value;

    protected virtual IAnalyzerAssemblyLoader CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        return DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"),
            externalResolvers: externalResolvers);
    }
}
