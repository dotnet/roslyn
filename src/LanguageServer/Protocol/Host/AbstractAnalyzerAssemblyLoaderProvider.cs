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
        _sharedShadowCopyLoader = new(CreateShadowCopyLoader);
        _externalResolvers = externalResolvers;
    }

    internal static string GetPath()
        => Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader");

    protected virtual IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader()
        => DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(GetPath(), _externalResolvers);

#if NET

    public IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader(bool getSharedLoader)
        // If no load context is provided, return the default shared instance.  Otherwise, create a fresh instance that
        // will load within its own dedicated ALC.
        => getSharedLoader
            ? _sharedShadowCopyLoader.Value
            : CreateShadowCopyLoader();

#else

    public IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader()
        => _sharedShadowCopyLoader.Value;

#endif
}
