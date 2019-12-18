// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

#nullable enable

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DocumentRefactoring
{
    [Export(typeof(DocumentRefactoringService)), PartNotDiscoverable]
    [Export(typeof(IDocumentRefactoringService))]
    [ExportWorkspaceService(typeof(IDocumentRefactoringService)), Shared]
    internal class DocumentRefactoringService : IDocumentRefactoringService
    {
        public async Task<Solution> UpdateAfterInfoChangeAsync(Document current, Document previous, CancellationToken cancellationToken = default)
        {
            var typeModifiedDocument = await UpdateTypeToMatchCurrentDocumentNameAsync(current, previous, cancellationToken).ConfigureAwait(false);
            var namespaceModifiedDocument = await UpdateNamespaceToMatchPath(typeModifiedDocument, previous, cancellationToken).ConfigureAwait(false);

            return namespaceModifiedDocument.Project.Solution;
        }

        private static async Task<Document> UpdateTypeToMatchCurrentDocumentNameAsync(Document current, Document previous, CancellationToken cancellationToken)
        {
            var syntaxRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return current;
            }

            var moveTypeService = current.GetLanguageService<IMoveTypeService>();
            if (moveTypeService is null)
            {
                return current;
            }

            var syntaxFactsService = current.GetRequiredLanguageService<ISyntaxFactsService>()!;
            IEnumerable<(SyntaxNode Node, string Name)> typeDeclarationPairs = syntaxRoot
                .DescendantNodes()
                .Where(syntaxFactsService.IsTypeDeclaration)
                .Select(n => (n, syntaxFactsService.GetDisplayName(n, DisplayNameOptions.None)));

            if (!typeDeclarationPairs.Any())
            {
                return current;
            }

            var previousDocumentName = previous.Name;
            var matchingTypeDeclarationPair = previousDocumentName is null
                ? typeDeclarationPairs.First()
                : typeDeclarationPairs.FirstOrDefault(p => p.Name.Equals(Path.GetFileNameWithoutExtension(previousDocumentName), System.StringComparison.OrdinalIgnoreCase));

            if (matchingTypeDeclarationPair == default)
            {
                return current;
            }

            var declarationNode = matchingTypeDeclarationPair.Node;

            var originalSolution = current.Project.Solution;
            var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(current, declarationNode.Span, MoveTypeOperationKind.RenameType, cancellationToken).ConfigureAwait(false);
            return modifiedSolution.GetDocument(current.Id)!;
        }

        private static async Task<Document> UpdateNamespaceToMatchPath(Document current, Document previous, CancellationToken cancellationToken)
        {
            var syntaxRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return current;
            }

            var changeNamespaceService = current.GetLanguageService<IChangeNamespaceService>();
            if (changeNamespaceService is null)
            {
                return current;
            }

            return current;
        }
    }
}
