// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// Used to supply the OOP server a callback that it can use to search for ReferenceAssemblies or
        /// nuget packages.  We can't necessarily do that search directly in the OOP server as our 
        /// 'SymbolSearchEngine' may actually be running in a *different* process (there is no guarantee
        /// that all remote work happens in the same process).  
        /// 
        /// This does mean, currently, that when we call over to OOP to do a search, it will bounce
        /// back to VS, which will then bounce back out to OOP to perform the Nuget/ReferenceAssembly
        /// portion of the search.  Ideally we could keep this all OOP.
        /// </summary>
        private sealed class RemoteAddImportServiceCallback : IRemoteMissingImportDiscoveryService.ICallback
        {
            private readonly ISymbolSearchService _symbolSearchService;

            public RemoteAddImportServiceCallback(ISymbolSearchService symbolSearchService)
                => _symbolSearchService = symbolSearchService;

            public ValueTask<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(string source, string name, int arity, CancellationToken cancellationToken)
                => _symbolSearchService.FindPackagesWithTypeAsync(source, name, arity, cancellationToken);

            public ValueTask<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(string source, string name, CancellationToken cancellationToken)
                => _symbolSearchService.FindPackagesWithAssemblyAsync(source, name, cancellationToken);

            public ValueTask<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(string name, int arity, CancellationToken cancellationToken)
                => _symbolSearchService.FindReferenceAssembliesWithTypeAsync(name, arity, cancellationToken);
        }
    }
}
