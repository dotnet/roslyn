// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Navigation;

internal abstract class AbstractDefinitionLocationService : IDefinitionLocationService
{
    private readonly IThreadingContext _threadingContext;
    private readonly IStreamingFindUsagesPresenter _streamingPresenter;

    protected AbstractDefinitionLocationService(
        IThreadingContext threadingContext,
        IStreamingFindUsagesPresenter streamingPresenter)
    {
        _threadingContext = threadingContext;
        _streamingPresenter = streamingPresenter;
    }

    private static Task<INavigableLocation?> GetNavigableLocationAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var workspace = solution.Workspace;
        var service = workspace.Services.GetRequiredService<IDocumentNavigationService>();

        return service.GetLocationForPositionAsync(
            workspace, document.Id, position, virtualSpace: 0, cancellationToken);
    }

    public async Task<DefinitionLocation?> GetDefinitionLocationAsync(
        Document document, int position, CancellationToken cancellationToken)
    {
        var symbolService = document.GetRequiredLanguageService<IGoToDefinitionSymbolService>();
        var (controlFlowTarget, controlFlowSpan) = await symbolService.GetTargetIfControlFlowAsync(
            document, position, cancellationToken).ConfigureAwait(false);
        if (controlFlowTarget != null)
        {
            var location = await GetNavigableLocationAsync(
                document, controlFlowTarget.Value, cancellationToken).ConfigureAwait(false);
            return location is null ? null : new DefinitionLocation(location, new DocumentSpan(document, controlFlowSpan));
        }
        else
        {
            // Try to compute the referenced symbol and attempt to go to definition for the symbol.
            var (symbol, project, span) = await symbolService.GetSymbolProjectAndBoundSpanAsync(
                document, position, cancellationToken).ConfigureAwait(false);
            if (symbol is null)
                return null;

            // if the symbol only has a single source location, and we're already on it,
            // try to see if there's a better symbol we could navigate to.
            var remappedLocation = await GetAlternativeLocationIfAlreadyOnDefinitionAsync(
                project, position, symbol, originalDocument: document, cancellationToken).ConfigureAwait(false);
            if (remappedLocation != null)
                return new DefinitionLocation(remappedLocation, new DocumentSpan(document, span));

            var isThirdPartyNavigationAllowed = await IsThirdPartyNavigationAllowedAsync(
                symbol, position, document, cancellationToken).ConfigureAwait(false);

            var location = await GoToDefinitionHelpers.GetDefinitionLocationAsync(
                symbol,
                project.Solution,
                _threadingContext,
                _streamingPresenter,
                thirdPartyNavigationAllowed: isThirdPartyNavigationAllowed,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            if (location is null)
                return null;

            return new DefinitionLocation(location, new DocumentSpan(document, span));
        }
    }

    /// <summary>
    /// Attempts to find a better definition for the symbol, if the user is already on the definition of it.
    /// </summary>
    /// <param name="project">The project context to use for finding symbols</param>
    /// <param name="originalDocument">The document the user is navigating from. This may not be part of the project supplied.</param>
    private async Task<INavigableLocation?> GetAlternativeLocationIfAlreadyOnDefinitionAsync(
        Project project, int position, ISymbol symbol, Document originalDocument, CancellationToken cancellationToken)
    {
        var solution = project.Solution;

        var sourceLocations = symbol.Locations.WhereAsArray(loc => loc.IsInSource);
        if (sourceLocations.Length != 1)
            return null;

        var definitionLocation = sourceLocations[0];
        if (!definitionLocation.SourceSpan.IntersectsWith(position))
            return null;

        var definitionTree = definitionLocation.SourceTree;
        var definitionDocument = solution.GetDocument(definitionTree);
        if (definitionDocument != originalDocument)
            return null;

        // Ok, we were already on the definition. Look for better symbols we could show results
        // for instead. For now, just see if we're on an interface member impl. If so, we can
        // instead navigate to the actual interface member.
        //
        // In the future we can expand this with other mappings if appropriate.
        var interfaceImpls = symbol.ExplicitOrImplicitInterfaceImplementations();
        if (interfaceImpls.Length == 0)
            return null;

        var title = string.Format(EditorFeaturesResources._0_implemented_members,
            FindUsagesHelpers.GetDisplayName(symbol));

        using var _ = ArrayBuilder<DefinitionItem>.GetInstance(out var builder);
        foreach (var impl in interfaceImpls)
        {
            builder.AddRange(await GoToDefinitionFeatureHelpers.GetDefinitionsAsync(
                impl, solution, thirdPartyNavigationAllowed: false, cancellationToken).ConfigureAwait(false));
        }

        var definitions = builder.ToImmutable();

        return await _streamingPresenter.GetStreamingLocationAsync(
            _threadingContext, solution.Workspace, title, definitions, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> IsThirdPartyNavigationAllowedAsync(
        ISymbol symbolToNavigateTo, int caretPosition, Document document, CancellationToken cancellationToken)
    {
        var syntaxRoot = document.GetRequiredSyntaxRootSynchronously(cancellationToken);
        var syntaxFactsService = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var containingTypeDeclaration = syntaxFactsService.GetContainingTypeDeclaration(syntaxRoot, caretPosition);

        if (containingTypeDeclaration != null)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            Debug.Assert(semanticModel != null);

            // Allow third parties to navigate to all symbols except types/constructors
            // if we are navigating from the corresponding type.

            if (semanticModel.GetDeclaredSymbol(containingTypeDeclaration, cancellationToken) is ITypeSymbol containingTypeSymbol &&
                (symbolToNavigateTo is ITypeSymbol || symbolToNavigateTo.IsConstructor()))
            {
                var candidateTypeSymbol = symbolToNavigateTo is ITypeSymbol
                    ? symbolToNavigateTo
                    : symbolToNavigateTo.ContainingType;

                if (Equals(containingTypeSymbol, candidateTypeSymbol))
                {
                    // We are navigating from the same type, so don't allow third parties to perform the navigation.
                    // This ensures that if we navigate to a class from within that class, we'll stay in the same file
                    // rather than navigate to, say, XAML.
                    return false;
                }
            }
        }

        return true;
    }
}
