// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeLens;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeLens
{
    [ExportWorkspaceService(typeof(ICodeLensReferencesService), layer: ServiceLayer.Host), Shared]
    internal sealed class VisualStudioCodeLensReferencesService : ICodeLensReferencesService
    {
        public async Task<ReferenceCount> GetReferenceCountAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode, int maxSearchResults,
            CancellationToken cancellationToken)
        {
            var remoteService = solution.Workspace.Services.GetService<IRemoteCodeLensReferencesService>();
            return remoteService == null
                ? await
                    CodeLensReferencesServiceFactory.Instance.GetReferenceCountAsync(solution, documentId, syntaxNode,
                        maxSearchResults, cancellationToken).ConfigureAwait(false)
                : await
                    remoteService.GetReferenceCountAsync(solution, documentId, syntaxNode, maxSearchResults,
                        cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReferenceLocationDescriptor>> FindReferenceLocationsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteService = solution.Workspace.Services.GetService<IRemoteCodeLensReferencesService>();
            return remoteService == null
                ? await
                    CodeLensReferencesServiceFactory.Instance.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false)
                : await
                    remoteService.FindReferenceLocationsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IEnumerable<ReferenceMethodDescriptor>> FindReferenceMethodsAsync(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteService = solution.Workspace.Services.GetService<IRemoteCodeLensReferencesService>();
            return remoteService == null
                ? await
                    CodeLensReferencesServiceFactory.Instance.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false)
                : await
                    remoteService.FindReferenceMethodsAsync(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }

        public async Task<string> GetFullyQualifiedName(Solution solution, DocumentId documentId, SyntaxNode syntaxNode,
            CancellationToken cancellationToken)
        {
            var remoteService = solution.Workspace.Services.GetService<IRemoteCodeLensReferencesService>();
            return remoteService == null
                ? await
                    CodeLensReferencesServiceFactory.Instance.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false)
                : await
                    remoteService.GetFullyQualifiedName(solution, documentId, syntaxNode, cancellationToken).ConfigureAwait(false);
        }
    }
}
