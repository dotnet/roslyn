// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SymbolSearch
{
    internal sealed class SymbolSearchUpdateNoOpEngine : ISymbolSearchUpdateEngine
    {
        public static readonly SymbolSearchUpdateNoOpEngine Instance = new();

        public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string assemblyName, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(ImmutableArray<PackageWithAssemblyResult>.Empty);

        public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(ImmutableArray<PackageWithTypeResult>.Empty);

        public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
            => ValueTaskFactory.FromResult(ImmutableArray<ReferenceAssemblyWithTypeResult>.Empty);

        public ValueTask UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory, CancellationToken cancellationToken)
            => default;
    }
}
