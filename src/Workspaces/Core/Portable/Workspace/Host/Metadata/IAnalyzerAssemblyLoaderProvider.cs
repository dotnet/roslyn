// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

internal interface IAnalyzerAssemblyLoaderProvider : IWorkspaceService
{
    IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader();
}

/// <summary>
/// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
/// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
/// </summary>
internal abstract class AbstractAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
{
    private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;

    public AbstractAnalyzerAssemblyLoaderProvider(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
    {
        // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
        _shadowCopyLoader = new(() => CreateShadowCopyLoader(externalResolvers));
    }

    public IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader()
        => _shadowCopyLoader.Value;

    protected virtual IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        => DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(
            GetDefaultShadowCopyPath(),
            externalResolvers: externalResolvers);

    public static string GetDefaultShadowCopyPath()
        => Path.Combine(Path.GetTempPath(), "Roslyn", "AnalyzerAssemblyLoader");
}

[ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderService(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : AbstractAnalyzerAssemblyLoaderProvider(externalResolvers.ToImmutableArray());
