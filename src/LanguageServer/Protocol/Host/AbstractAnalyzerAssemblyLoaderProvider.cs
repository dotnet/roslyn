// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;

#if NET
using System.Runtime.Loader;
#endif

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _sharedShadowCopyLoader;
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers;

    public AbstractAnalyzerAssemblyLoaderProvider(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _sharedShadowCopyLoader = new(CreateNewShadowCopyLoader);
        _externalResolvers = externalResolvers;
    }

    public IAnalyzerAssemblyLoaderInternal SharedShadowCopyLoader => _sharedShadowCopyLoader.Value;

    public virtual IAnalyzerAssemblyLoaderInternal CreateNewShadowCopyLoader()
        => DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(Path.Combine(Path.GetTempPath(), "Host", "AnalyzerAssemblyLoader"), _externalResolvers);
}
