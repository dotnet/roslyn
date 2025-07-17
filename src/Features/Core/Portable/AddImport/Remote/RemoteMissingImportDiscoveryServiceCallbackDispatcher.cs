// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport;

/// <summary>
/// Used to supply the OOP server a callback that it can use to search for ReferenceAssemblies or nuget packages.  We
/// can't necessarily do that search directly in the OOP server as our 'SymbolSearchEngine' may actually be running in a
/// *different* process (there is no guarantee that all remote work happens in the same process).  
/// 
/// This does mean, currently, that when we call over to OOP to do a search, it will bounce back to VS, which will then
/// bounce back out to OOP to perform the Nuget/ReferenceAssembly portion of the search.  Ideally we could keep this all
/// OOP.
/// </summary>
[ExportRemoteServiceCallbackDispatcher(typeof(IRemoteMissingImportDiscoveryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RemoteMissingImportDiscoveryServiceCallbackDispatcher()
    : RemoteServiceCallbackDispatcher, IRemoteMissingImportDiscoveryService.ICallback
{
    private ISymbolSearchService GetService(RemoteServiceCallbackId callbackId)
        => (ISymbolSearchService)GetCallback(callbackId);

    public ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(RemoteServiceCallbackId callbackId, string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => GetService(callbackId).FindPackagesAsync(source, typeQuery, namespaceQuery, cancellationToken);

    public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(RemoteServiceCallbackId callbackId, string source, string name, CancellationToken cancellationToken)
        => GetService(callbackId).FindPackagesWithAssemblyAsync(source, name, cancellationToken);

    public ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(RemoteServiceCallbackId callbackId, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => GetService(callbackId).FindReferenceAssembliesAsync(typeQuery, namespaceQuery, cancellationToken);
}
