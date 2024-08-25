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

internal abstract class AbstractAnalyzerAssemblyLoaderProviderFactory(
    IEnumerable<IAnalyzerAssemblyResolver> externalResolvers) : IWorkspaceServiceFactory
{
    private readonly ImmutableArray<IAnalyzerAssemblyResolver> _externalResolvers = externalResolvers.ToImmutableArray();

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new DefaultAnalyzerAssemblyLoaderProvider(this, workspaceServices.Workspace.Kind ?? "Default", _externalResolvers);

    protected virtual IAnalyzerAssemblyLoaderInternal WrapLoader(IAnalyzerAssemblyLoaderInternal loader)
        => loader;

    /// <summary>
    /// Abstract implementation of an analyzer assembly loader that can be used by VS/VSCode to provide a <see
    /// cref="IAnalyzerAssemblyLoader"/> with an appropriate path.
    /// </summary>
    private sealed class DefaultAnalyzerAssemblyLoaderProvider : IAnalyzerAssemblyLoaderProvider
    {
        private readonly AbstractAnalyzerAssemblyLoaderProviderFactory _factory;
        private readonly string _workspaceKind;
        private readonly Lazy<IAnalyzerAssemblyLoaderInternal> _shadowCopyLoader;

        public DefaultAnalyzerAssemblyLoaderProvider(
            AbstractAnalyzerAssemblyLoaderProviderFactory factory,
            string workspaceKind,
            ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
        {
            _factory = factory;
            _workspaceKind = workspaceKind;
            // We use a lazy here in case creating the loader requires MEF imports in the derived constructor.
            _shadowCopyLoader = new(() => CreateShadowCopyLoader(externalResolvers));
        }

        public IAnalyzerAssemblyLoaderInternal GetShadowCopyLoader()
            => _shadowCopyLoader.Value;

        private IAnalyzerAssemblyLoaderInternal CreateShadowCopyLoader(ImmutableArray<IAnalyzerAssemblyResolver> externalResolvers)
            => _factory.WrapLoader(DefaultAnalyzerAssemblyLoader.CreateNonLockingLoader(GetDefaultShadowCopyPath(), externalResolvers));

        private string GetDefaultShadowCopyPath()
            => Path.Combine(Path.GetTempPath(), "Roslyn", _workspaceKind, "AnalyzerAssemblyLoader");
    }
}


[ExportWorkspaceServiceFactory(typeof(IAnalyzerAssemblyLoaderProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultAnalyzerAssemblyLoaderService(
    [ImportMany] IEnumerable<IAnalyzerAssemblyResolver> externalResolvers)
    : AbstractAnalyzerAssemblyLoaderProviderFactory(externalResolvers);
