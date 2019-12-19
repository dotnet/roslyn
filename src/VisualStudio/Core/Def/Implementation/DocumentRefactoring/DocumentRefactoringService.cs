// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CodeRefactorings.MoveType;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.DocumentRefactoring
{
    // PartNotDiscoverable export is used for unit tests
    [Export(typeof(DocumentRefactoringService)), PartNotDiscoverable]
    [Export(typeof(IDocumentRefactoringService)), Shared]
    internal class DocumentRefactoringService : IDocumentRefactoringService
    {
        public async Task<Solution> UpdateAfterInfoChangeAsync(Document current, Document previous, CancellationToken cancellationToken = default)
        {
            var typeModifiedSolution = await UpdateTypeToMatchCurrentDocumentNameAsync(current, previous, cancellationToken).ConfigureAwait(false);
            var typeModifiedDocument = typeModifiedSolution.GetRequiredDocument(current.Id);
            var namespaceModifiedSolution = await UpdateNamespaceToMatchPath(typeModifiedDocument, previous, cancellationToken).ConfigureAwait(false);

            return namespaceModifiedSolution;
        }

        private static async Task<Solution> UpdateTypeToMatchCurrentDocumentNameAsync(Document current, Document previous, CancellationToken cancellationToken)
        {
            var solution = current.Project.Solution;

            var syntaxRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return solution;
            }

            var moveTypeService = current.GetLanguageService<IMoveTypeService>();
            if (moveTypeService is null)
            {
                return solution;
            }

            var syntaxFactsService = current.GetRequiredLanguageService<ISyntaxFactsService>();
            var typeDeclarations = syntaxRoot
                .DescendantNodes()
                .Where(syntaxFactsService.IsTypeDeclaration);

            if (!typeDeclarations.Any())
            {
                return solution;
            }

            var previousDocumentName = previous.Name;
            var declarationNode = previousDocumentName is null
                ? typeDeclarations.First()
                : typeDeclarations.FirstOrDefault(p => syntaxFactsService.GetDisplayName(p, DisplayNameOptions.None).Equals(Path.GetFileNameWithoutExtension(previousDocumentName), System.StringComparison.OrdinalIgnoreCase));

            if (declarationNode == default)
            {
                return solution;
            }

            var originalSolution = current.Project.Solution;
            var modifiedSolution = await moveTypeService.GetModifiedSolutionAsync(current, declarationNode.Span, MoveTypeOperationKind.RenameType, cancellationToken).ConfigureAwait(false);
            return modifiedSolution;
        }

        private static async Task<Solution> UpdateNamespaceToMatchPath(Document current, Document previous, CancellationToken cancellationToken)
        {
            var solution = current.Project.Solution;

            var syntaxRoot = await current.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot is null)
            {
                return solution;
            }

            var changeNamespaceService = current.GetLanguageService<IChangeNamespaceService>();
            if (changeNamespaceService is null)
            {
                return solution;
            }

            var syntaxFacts = current.GetRequiredLanguageService<ISyntaxFactsService>();

            var namespaces = syntaxRoot
                .DescendantNodes()
                .Where(syntaxFacts.IsNamespaceDeclaration);

            foreach (var @namespace in namespaces)
            {
                var oldTargetNamespace = changeNamespaceService.GetTargetNamespaceFromDocument(previous);
                if (!Equals(oldTargetNamespace, syntaxFacts.GetDisplayName(@namespace, DisplayNameOptions.IncludeNamespaces)))
                {
                    continue;
                }

                if (await changeNamespaceService.CanChangeNamespaceAsync(current, @namespace, cancellationToken).ConfigureAwait(false))
                {
                    var targetNamespace = changeNamespaceService.GetTargetNamespaceFromDocument(current);
                    if (targetNamespace == null)
                    {
                        continue;
                    }

                    solution = await changeNamespaceService.ChangeNamespaceAsync(current, @namespace, targetNamespace, cancellationToken).ConfigureAwait(false);
                }
            }

            return solution;
        }
    }
}
