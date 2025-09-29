// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteMissingImportDiscoveryService(
    in BrokeredServiceBase.ServiceConstructionArguments arguments,
    RemoteCallback<IRemoteMissingImportDiscoveryService.ICallback> callback)
    : BrokeredServiceBase(arguments), IRemoteMissingImportDiscoveryService
{
    internal sealed class Factory : FactoryBase<IRemoteMissingImportDiscoveryService, IRemoteMissingImportDiscoveryService.ICallback>
    {
        protected override IRemoteMissingImportDiscoveryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<IRemoteMissingImportDiscoveryService.ICallback> callback)
            => new RemoteMissingImportDiscoveryService(arguments, callback);
    }

    private readonly RemoteCallback<IRemoteMissingImportDiscoveryService.ICallback> _callback = callback;

    public ValueTask<ImmutableArray<AddImportFixData>> GetFixesAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        DocumentId documentId,
        TextSpan span,
        string diagnosticId,
        int maxResults,
        AddImportOptions options,
        ImmutableArray<PackageSource> packageSources,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document is null)
                return [];

            var service = document.GetRequiredLanguageService<IAddImportFeatureService>();

            var symbolSearchService = new SymbolSearchService(_callback, callbackId);

            var result = await service.GetFixesAsync(
                document, span, diagnosticId, maxResults,
                symbolSearchService, options,
                packageSources, cancellationToken).ConfigureAwait(false);

            return result;
        }, cancellationToken);
    }

    public ValueTask<ImmutableArray<AddImportFixData>> GetUniqueFixesAsync(
        Checksum solutionChecksum,
        RemoteServiceCallbackId callbackId,
        DocumentId documentId,
        TextSpan span,
        ImmutableArray<string> diagnosticIds,
        AddImportOptions options,
        ImmutableArray<PackageSource> packageSources,
        CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var document = await solution.GetDocumentAsync(documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            if (document is null)
                return [];

            var service = document.GetRequiredLanguageService<IAddImportFeatureService>();

            var symbolSearchService = new SymbolSearchService(_callback, callbackId);

            var result = await service.GetUniqueFixesAsync(
                document, span, diagnosticIds,
                symbolSearchService, options,
                packageSources, cancellationToken).ConfigureAwait(false);

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Provides an implementation of the <see cref="ISymbolSearchService"/> on the remote side so that
    /// Add-Import can find results in nuget packages/reference assemblies.  This works
    /// by remoting *from* the OOP server back to the host, which can then forward this 
    /// appropriately to wherever the real <see cref="ISymbolSearchService"/> is running.  This is necessary
    /// because it's not guaranteed that the real <see cref="ISymbolSearchService"/> will be running in 
    /// the same process that is supplying the <see cref="RemoteMissingImportDiscoveryService"/>.
    /// 
    /// Ideally we would not need to bounce back to the host for this.
    /// </summary>
    private sealed class SymbolSearchService(
        RemoteCallback<IRemoteMissingImportDiscoveryService.ICallback> callback,
        RemoteServiceCallbackId callbackId)
        : ISymbolSearchService
    {
        private readonly RemoteCallback<IRemoteMissingImportDiscoveryService.ICallback> _callback = callback;
        private readonly RemoteServiceCallbackId _callbackId = callbackId;

        public ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.FindPackagesAsync(_callbackId, source, typeQuery, namespaceQuery, cancellationToken), cancellationToken);

        public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.FindPackagesWithAssemblyAsync(_callbackId, source, assemblyName, cancellationToken), cancellationToken);

        public ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
            => _callback.InvokeAsync((callback, cancellationToken) => callback.FindReferenceAssembliesAsync(_callbackId, typeQuery, namespaceQuery, cancellationToken), cancellationToken);
    }
}
