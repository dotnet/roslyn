// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SymbolMapping;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GoToDefinition;

internal abstract class AbstractGoToDefinitionSymbolService : IGoToDefinitionSymbolService
{
    protected abstract ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation);

    protected abstract int? GetTargetPositionIfControlFlow(SemanticModel semanticModel, SyntaxToken token);

    public async Task<(ISymbol?, Project, TextSpan)> GetSymbolProjectAndBoundSpanAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var services = document.Project.Solution.Services;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var semanticInfo = await SymbolFinder.GetSemanticInfoAtPositionAsync(semanticModel, position, services, cancellationToken).ConfigureAwait(false);

        // Prefer references to declarations. It's more likely that the user is attempting to 
        // go to a definition at some other location, rather than the definition they're on. 
        // This can happen when a token is at a location that is both a reference and a definition.
        // For example, on an anonymous type member declaration.
        var symbol = semanticInfo.AliasSymbol
            ?? semanticInfo.ReferencedSymbols.FirstOrDefault()
            ?? semanticInfo.DeclaredSymbol
            ?? semanticInfo.Type;

        if (symbol is null)
        {
            return (null, project, semanticInfo.Span);
        }

        // If this document is not in the primary workspace, we may want to search for results
        // in a solution different from the one we started in. Use the starting workspace's
        // ISymbolMappingService to get a context for searching in the proper solution.
        // For example when looking at a file from Source Link, it could be a partial type that
        // only has a subset of the type actually part of the project (because the rest hasn't been
        // downloaded) so we want to ensure we're navigating based on the original metadata symbol.
        var mappingService = services.GetRequiredService<ISymbolMappingService>();
        var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);

        // If the mapping fails, we proceed as normal with the symbol we originally found.
        if (mapping is not null)
        {
            symbol = mapping.Symbol;
            project = mapping.Project;
        }

        // The compilation will have already been realised, either by the semantic model or the symbol mapping
        var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
        return (FindRelatedExplicitlyDeclaredSymbol(symbol, compilation), project, semanticInfo.Span);
    }

    public async Task<(int? targetPosition, TextSpan tokenSpan)> GetTargetIfControlFlowAsync(Document document, int position, CancellationToken cancellationToken)
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var token = await syntaxTree.GetTouchingTokenAsync(position, syntaxFacts.IsBindableToken, cancellationToken, findInsideTrivia: true).ConfigureAwait(false);

        if (token == default)
            return default;

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        return (GetTargetPositionIfControlFlow(semanticModel, token), token.Span);
    }
}
