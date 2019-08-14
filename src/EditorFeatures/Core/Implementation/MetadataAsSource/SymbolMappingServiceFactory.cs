// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public SymbolMappingServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SymbolMappingService();
        }

        private sealed class SymbolMappingService : ISymbolMappingService
        {
            public Task<SymbolMappingResult> MapSymbolAsync(Document document, SymbolKey symbolId, CancellationToken cancellationToken)
            {
                if (!(document.Project.Solution.Workspace is MetadataAsSourceWorkspace workspace))
                {
                    throw new ArgumentException(EditorFeaturesResources.Document_must_be_contained_in_the_workspace_that_created_this_service, nameof(document));
                }

                return workspace.FileService.MapSymbolAsync(document, symbolId, cancellationToken);
            }

            public async Task<SymbolMappingResult> MapSymbolAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
            {
                var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                return await MapSymbolAsync(document, SymbolKey.Create(symbol, cancellationToken), cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
