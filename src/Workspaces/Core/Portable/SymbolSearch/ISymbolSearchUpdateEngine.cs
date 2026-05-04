// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.SymbolSearch;

/// <summary>
/// Service that allows you to query the SymbolSearch database and which keeps 
/// the database up to date.  
/// </summary>
internal interface ISymbolSearchUpdateEngine : IDisposable
{
    ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken);

    ValueTask<ImmutableArray<PackageResult>> FindPackagesAsync(
        string source, TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
        string source, string assemblyName, CancellationToken cancellationToken);
    ValueTask<ImmutableArray<ReferenceAssemblyResult>> FindReferenceAssembliesAsync(
        TypeQuery typeQuery, NamespaceQuery namespaceQuery, CancellationToken cancellationToken);
}
