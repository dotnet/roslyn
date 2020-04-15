// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SymbolMapping;

namespace Microsoft.CodeAnalysis.Editor.Implementation.MetadataAsSource
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolMappingService), WorkspaceKind.MetadataAsSource)]
    [Shared]
    internal class SymbolMappingServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SymbolMappingServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SymbolMappingService();

        private sealed class SymbolMappingService : ISymbolMappingService
        {
            public Task<SymbolMappingResult?> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken)
            {
                if (!(document.Project.Solution.Workspace is MetadataAsSourceWorkspace workspace))
                {
                    throw new ArgumentException(EditorFeaturesResources.Document_must_be_contained_in_the_workspace_that_created_this_service, nameof(document));
                }

                return workspace.FileService.MapSymbolAsync(document, symbolId, cancellationToken);
            }

            public async Task<SymbolMappingResult?> MapSymbolAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return await MapSymbolAsync(document, SymbolKey.Create(symbol, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
