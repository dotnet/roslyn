// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch;

internal sealed class SymbolSearchUpdateNoOpEngine : ISymbolSearchUpdateEngine
{
    public static readonly SymbolSearchUpdateNoOpEngine Instance = new();

    public void Dispose()
    {
        // Nothing to do for the no-op version.
    }

    public async ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
        => ImmutableArray<PackageWithAssemblyResult>.Empty;

    public async ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => ImmutableArray<PackageResult>.Empty;

    public async ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken)
        => ImmutableArray<ReferenceAssemblyResult>.Empty;

    public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
        => default;
}
