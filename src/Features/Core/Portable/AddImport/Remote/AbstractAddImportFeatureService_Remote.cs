// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        private async Task<ImmutableArray<AddImportFixData>> GetFixesInRemoteProcessAsync(
            RemoteHostClient.Session session, Document document, TextSpan span, 
            string diagnosticId, bool placeSystemNamespaceFirst,
            bool searchReferenceAssemblies, ImmutableArray<PackageSource> packageSources)
        {
            var result = await session.InvokeAsync<IList<AddImportFixData>>(
                nameof(IRemoteAddImportFeatureService.GetFixesAsync),
                document.Id, span, diagnosticId, placeSystemNamespaceFirst, 
                searchReferenceAssemblies, packageSources).ConfigureAwait(false);

            return result.AsImmutableOrEmpty();
        }

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

            public async Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity)
            {
                var result = await _symbolSearchService.FindPackagesWithTypeAsync(
                    source, name, arity, _cancellationToken).ConfigureAwait(false);

                return result;
            }

            public async Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string name)
            {
                var result = await _symbolSearchService.FindPackagesWithAssemblyAsync(
                    source, name, _cancellationToken).ConfigureAwait(false);

                return result;
            }

            public async Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity)
            {
                var result = await _symbolSearchService.FindReferenceAssembliesWithTypeAsync(
                    name, arity, _cancellationToken).ConfigureAwait(false);

                return result;
            }
        }
    }
}