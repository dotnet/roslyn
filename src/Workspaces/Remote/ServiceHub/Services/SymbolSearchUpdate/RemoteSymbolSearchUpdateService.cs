// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.VisualStudio.LanguageServices.Storage;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteSymbolSearchUpdateService : BrokeredServiceBase, IRemoteSymbolSearchUpdateService
{
    internal sealed class Factory : FactoryBase<IRemoteSymbolSearchUpdateService>
    {
        protected override IRemoteSymbolSearchUpdateService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteSymbolSearchUpdateService(arguments);
    }

    private readonly ISymbolSearchUpdateEngine _updateEngine;

    public RemoteSymbolSearchUpdateService(in ServiceConstructionArguments arguments)
        : base(arguments)
    {
        _updateEngine = SymbolSearchUpdateEngineFactory.CreateEngineInProcess(FileDownloader.Factory.Instance);
    }

    public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
            _updateEngine.UpdateContinuouslyAsync(sourceName, localSettingsDirectory, cancellationToken),
            cancellationToken);
    }

    public ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
            _updateEngine.FindPackagesAsync(source, typeQuery, namespaceQuery, cancellationToken),
            cancellationToken);
    }

    public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
            _updateEngine.FindPackagesWithAssemblyAsync(source, assemblyName, cancellationToken),
            cancellationToken);
    }

    public ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
    {
        return RunServiceAsync(cancellationToken =>
            _updateEngine.FindReferenceAssembliesAsync(typeQuery, namespaceQuery, cancellationToken),
            cancellationToken);
    }
}
