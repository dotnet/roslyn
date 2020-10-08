// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal interface IRemoteMissingImportDiscoveryService
    {
        internal interface ICallback
        {
            ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken);
            ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken);
            ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken);
        }

        ValueTask<ImmutableArray<AddImportFixData>> GetFixesAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan span, string diagnosticId, int maxResults, bool placeSystemNamespaceFirst,
            bool searchReferenceAssemblies, ImmutableArray<PackageSource> packageSources, CancellationToken cancellationToken);
    }
}
