// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        private class RemoteSymbolSearchService : IRemoteSymbolSearchUpdateEngine
        {
            private readonly ISymbolSearchService _symbolSearchService;
            private readonly CancellationToken _shutdownCancellation;

            public RemoteSymbolSearchService(
                ISymbolSearchService symbolSearchService,
                CancellationToken shutdownCancellationToken)
            {
                _symbolSearchService = symbolSearchService;
                _shutdownCancellation = shutdownCancellationToken;
            }

            public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
            {
                // Remote side should never call this.
                throw new NotImplementedException();
            }

            public Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                return _symbolSearchService.FindPackagesWithTypeAsync(
                    source, name, arity, cancellationToken);
            }

            public Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string name, CancellationToken cancellationToken)
            {
                return _symbolSearchService.FindPackagesWithAssemblyAsync(
                    source, name, cancellationToken);
            }

            public Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                return _symbolSearchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, cancellationToken);
            }
        }
    }
}
