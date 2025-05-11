// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed class SymbolSearchUpdateEngineProxy(RemoteHostClient client) : ISymbolSearchUpdateEngine
{
    private readonly RemoteServiceConnection<IRemoteSymbolSearchUpdateService> _connection = client.CreateConnection<IRemoteSymbolSearchUpdateService>(callbackTarget: null);

    public void Dispose()
        => _connection.Dispose();

    public async ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
    {
        var result = await _connection.TryInvokeAsync(
            (service, cancellationToken) => service.FindPackagesAsync(source, typeQuery, namespaceQuery, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result.HasValue ? result.Value : [];
    }

    public async ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
        string source, string assemblyName, CancellationToken cancellationToken)
    {
        var result = await _connection.TryInvokeAsync(
            (service, cancellationToken) => service.FindPackagesWithAssemblyAsync(source, assemblyName, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result.HasValue ? result.Value : [];
    }

    public async ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(
        TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
    {
        var result = await _connection.TryInvokeAsync(
            (service, cancellationToken) => service.FindReferenceAssembliesAsync(typeQuery, namespaceQuery, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result.HasValue ? result.Value : [];
    }

    public async ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
    {
        _ = await _connection.TryInvokeAsync(
            (service, cancellationToken) => service.UpdateContinuouslyAsync(sourceName, localSettingsDirectory, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }
}
