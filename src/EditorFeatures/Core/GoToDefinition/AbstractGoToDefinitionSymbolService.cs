// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.GoToDefinition
{
    internal abstract class AbstractGoToDefinitionSymbolService : IGoToDefinitionSymbolService
    {
        protected abstract ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation);

        // If the cursor is positioned on the keyword "override", returns the position for the declared/overridding member.
        protected abstract Task<int?> GetDeclarationPositionIfOverride(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        public async Task<(ISymbol, TextSpan)> GetSymbolAndBoundSpanAsync(Document document, int position, CancellationToken cancellationToken)
        {
            // If the cursor is on the keyword for overriding, the referenced symbol is the overridden symbol 
            var overriddenSymbol = await FindOverriddenSymbolIfOverride(document, position, cancellationToken).ConfigureAwait(false);

            var workspace = document.Project.Solution.Workspace;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticInfo = await SymbolFinder.GetSemanticInfoAtPositionAsync(semanticModel, position, workspace, cancellationToken).ConfigureAwait(false);

            // prefer references to declarations.  It's more likely that the user is attempting to 
            // go to a definition at some other location, rather than the definition they're on.  
            // This can happen when a token is at a location that is both a reference and a definition.
            // For example, on an anonymous type member declaration.
            var symbol = overriddenSymbol ??
                semanticInfo.AliasSymbol ??
                semanticInfo.ReferencedSymbols.FirstOrDefault() ??
                semanticInfo.DeclaredSymbol ??
                semanticInfo.Type;

            return (FindRelatedExplicitlyDeclaredSymbol(symbol, semanticModel.Compilation), semanticInfo.Span);
        }

        private async Task<ISymbol> FindOverriddenSymbolIfOverride(Document document, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var overridePosition = await GetDeclarationPositionIfOverride(syntaxTree, position, cancellationToken).ConfigureAwait(false);
            if (overridePosition.HasValue)
            {
                var symbolService = document.GetLanguageService<IGoToDefinitionSymbolService>();
                var (overrideSymbol, _) = await symbolService.GetSymbolAndBoundSpanAsync(document, overridePosition.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (overrideSymbol != null)
                {
                    return INamedTypeSymbolExtensions.GetOverriddenMember(overrideSymbol);
                }
            }

            return null;
        }
    }
}
