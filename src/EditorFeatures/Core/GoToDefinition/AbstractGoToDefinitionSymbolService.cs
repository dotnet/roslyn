// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        public async Task<(ISymbol, TextSpan)> GetSymbolAndBoundSpanAsync(Document document, int position, bool includeType, CancellationToken cancellationToken)
        {
            var workspace = document.Project.Solution.Workspace;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticInfo = await SymbolFinder.GetSemanticInfoAtPositionAsync(semanticModel, position, workspace, cancellationToken).ConfigureAwait(false);
            var symbol = GetSymbol(semanticInfo, includeType);

            if (symbol is null)
            {
                return (null, semanticInfo.Span);
            }

            return (FindRelatedExplicitlyDeclaredSymbol(symbol, semanticModel.Compilation), semanticInfo.Span);
        }

        private static ISymbol GetSymbol(TokenSemanticInfo semanticInfo, bool includeType)
        {
            // Prefer references to declarations. It's more likely that the user is attempting to 
            // go to a definition at some other location, rather than the definition they're on. 
            // This can happen when a token is at a location that is both a reference and a definition.
            // For example, on an anonymous type member declaration.

            return semanticInfo.AliasSymbol
                ?? semanticInfo.ReferencedSymbols.FirstOrDefault()
                ?? semanticInfo.DeclaredSymbol
                ?? (includeType ? semanticInfo.Type : null);
        }
    }
}
