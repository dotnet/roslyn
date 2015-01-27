// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
{
    internal abstract class AbstractGoToDefinitionService : IGoToDefinitionService
    {
        private readonly IEnumerable<Lazy<INavigableItemsPresenter>> _presenters;

        protected abstract ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation);

        protected AbstractGoToDefinitionService(IEnumerable<Lazy<INavigableItemsPresenter>> presenters)
        {
            _presenters = presenters;
        }

        private async Task<ISymbol> FindSymbolAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = SymbolFinder.FindSymbolAtPosition(semanticModel, position, workspace, bindLiteralsToUnderlyingType: true, cancellationToken: cancellationToken);

            return FindRelatedExplicitlyDeclaredSymbol(symbol, semanticModel.Compilation);
        }

        public async Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = await FindSymbolAsync(document, position, cancellationToken).ConfigureAwait(false);

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return NavigableItemFactory.GetItemsfromPreferredSourceLocations(document.Project.Solution, symbol).ToList();
        }

        public bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken)
        {
            var symbol = FindSymbolAsync(document, position, cancellationToken).WaitAndGetResult(cancellationToken);

            if (symbol != null)
            {
                var containingTypeSymbol = GetContainingTypeSymbol(position, document, cancellationToken);

                if (GoToDefinitionHelpers.TryGoToDefinition(symbol, document.Project, _presenters, containingTypeSymbol, throwOnHiddenDefinition: true, cancellationToken: cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static ITypeSymbol GetContainingTypeSymbol(int caretPosition, Document document, CancellationToken cancellationToken)
        {
            var syntaxRoot = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var containingTypeDeclaration = syntaxFactsService.GetContainingTypeDeclaration(syntaxRoot, caretPosition);

            if (containingTypeDeclaration != null)
            {
                var semanticModel = document.GetSemanticModelAsync(cancellationToken).WaitAndGetResult(cancellationToken);
                return semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken) as ITypeSymbol;
            }

            return null;
        }
    }
}
