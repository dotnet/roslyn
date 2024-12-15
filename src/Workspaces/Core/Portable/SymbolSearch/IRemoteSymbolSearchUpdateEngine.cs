// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal interface IRemoteSymbolSearchUpdateService
{
    ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);
}
