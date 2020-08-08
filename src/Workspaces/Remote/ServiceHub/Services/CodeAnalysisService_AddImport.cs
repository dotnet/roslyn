// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.Packaging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService : IRemoteAddImportFeatureService
    {
        public Task<IList<AddImportFixData>> GetFixesAsync(
            PinnedSolutionInfo solutionInfo, DocumentId documentId, TextSpan span, string diagnosticId, int maxResults, bool placeSystemNamespaceFirst,
            bool searchReferenceAssemblies, IList<PackageSource> packageSources, CancellationToken cancellationToken)
        {
            return RunServiceAsync(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var document = solution.GetDocument(documentId);

                    var service = document.GetLanguageService<IAddImportFeatureService>();

                    var symbolSearchService = new SymbolSearchService(EndPoint);

                    var result = await service.GetFixesAsync(
                        document, span, diagnosticId, maxResults, placeSystemNamespaceFirst,
                        symbolSearchService, searchReferenceAssemblies,
                        packageSources.ToImmutableArray(), cancellationToken).ConfigureAwait(false);

                    return (IList<AddImportFixData>)result;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Provides an implementation of the ISymbolSearchService on the remote side so that
        /// Add-Import can find results in nuget packages/reference assemblies.  This works
        /// by remoting *from* the OOP server back to the host, which can then forward this 
        /// appropriately to wherever the real ISymbolSearchService is running.  This is necessary
        /// because it's not guaranteed that the real ISymbolSearchService will be running in 
        /// the same process that is supplying the <see cref="CodeAnalysisService"/>.
        /// 
        /// Ideally we would not need to bounce back to the host for this.
        /// </summary>
        private sealed class SymbolSearchService : ISymbolSearchService
        {
            private readonly RemoteEndPoint _endPoint;

            public SymbolSearchService(RemoteEndPoint endPoint)
            {
                _endPoint = endPoint;
            }

            public async Task<IList<PackageWithTypeResult>> FindPackagesWithTypeAsync(
                string source, string name, int arity, CancellationToken cancellationToken)
            {
                return await _endPoint.InvokeAsync<IList<PackageWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithTypeAsync),
                    new object[] { source, name, arity },
                    cancellationToken).ConfigureAwait(false);
            }

            public async Task<IList<PackageWithAssemblyResult>> FindPackagesWithAssemblyAsync(
                string source, string assemblyName, CancellationToken cancellationToken)
            {
                return await _endPoint.InvokeAsync<IList<PackageWithAssemblyResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindPackagesWithAssemblyAsync),
                    new object[] { source, assemblyName },
                    cancellationToken).ConfigureAwait(false);
            }

            public async Task<IList<ReferenceAssemblyWithTypeResult>> FindReferenceAssembliesWithTypeAsync(
                string name, int arity, CancellationToken cancellationToken)
            {
                return await _endPoint.InvokeAsync<IList<ReferenceAssemblyWithTypeResult>>(
                    nameof(IRemoteSymbolSearchUpdateEngine.FindReferenceAssembliesWithTypeAsync),
                    new object[] { name, arity },
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
