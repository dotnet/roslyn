﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        private class RemoteSymbolSearchService : IRemoteSymbolSearchUpdateEngine
        {
            private readonly ISymbolSearchService _symbolSearchService;
            private readonly CancellationToken _cancellationToken;

            public RemoteSymbolSearchService(
                ISymbolSearchService symbolSearchService,
                CancellationToken cancellationToken)
            {
                _symbolSearchService = symbolSearchService;
                _cancellationToken = cancellationToken;
            }

            public Task UpdateContinuouslyAsync(string sourceName, string localSettingsDirectory)
            {
                // Remote side should never call this.
                throw new NotImplementedException();
            }

            public Task<ImmutableArray<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                return _symbolSearchService.FindPackagesWithTypeAsync(
                    source, name, arity, _cancellationToken);
            }

            public Task<ImmutableArray<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string name)
            {
                return _symbolSearchService.FindPackagesWithAssemblyAsync(
                    source, name, _cancellationToken);
            }

            public Task<ImmutableArray<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                return _symbolSearchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, _cancellationToken);
            }
        }
    }
}