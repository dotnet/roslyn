// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp;

internal abstract partial class AbstractSignatureHelpProvider : ISignatureHelpProvider
{
    protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutParametersFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(
            SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeParameters);

    protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutTypeParametersFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(
            SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions & ~SymbolDisplayGenericsOptions.IncludeTypeParameters);

    protected AbstractSignatureHelpProvider()
    {
    }

    public abstract bool IsTriggerCharacter(char ch);
    public abstract bool IsRetriggerCharacter(char ch);

    protected abstract Task<SignatureHelpItems?> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken);

    protected static SignatureHelpItems? CreateSignatureHelpItems(
        IList<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState? state, int? selectedItemIndex, int parameterIndexOverride)
    {
        if (items is null || items.Count == 0 || state == null)
            return null;

        if (selectedItemIndex < 0)
            selectedItemIndex = null;

        (items, selectedItemIndex) = Filter(items, state.Value.ArgumentNames, selectedItemIndex);

        // If the caller provided a preferred parameter for us to be on then override whatever we found syntactically.
        var argumentIndex = state.Value.ArgumentIndex;
        if (parameterIndexOverride >= 0)
        {
            // However, in the case where the overridden index is to a variadic member, and the syntactic index goes
            // beyond the length of hte normal parameters, do not do this.  The syntactic index is valid for the
            // variadic member, and we still want to remember where we are syntactically so that if the user picks
            // another member that we correctly pick the right parameter for it.
            var keepSyntacticIndex =
                argumentIndex > parameterIndexOverride &&
                selectedItemIndex != null &&
                items[selectedItemIndex.Value].IsVariadic &&
                argumentIndex >= items[selectedItemIndex.Value].Parameters.Length;

            if (!keepSyntacticIndex)
                argumentIndex = parameterIndexOverride;
        }

        return new SignatureHelpItems(items, applicableSpan, argumentIndex, state.Value.ArgumentCount, state.Value.ArgumentName, selectedItemIndex);
    }

    protected static SignatureHelpItems? CreateCollectionInitializerSignatureHelpItems(
        IList<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState? state)
    {
        // We will have added all the accessible '.Add' methods that take at least one
        // arguments. However, in general the one-arg Add method is the least likely for the
        // user to invoke. For example, say there is:
        //
        //      new JObject { { $$ } }
        //
        // Technically, the user could be calling the `.Add(object)` overload in this case.
        // However, normally in that case, they would just supply the value directly like so:
        //
        //      new JObject { new JProperty(...), new JProperty(...) }
        //
        // So, it's a strong signal when they're inside another `{ $$ }` that they want to call
        // the .Add methods that take multiple args, like so:
        //
        //      new JObject { { propName, propValue }, { propName, propValue } }
        // 
        // So, we include all the .Add methods, but we prefer selecting the first that has
        // at least two parameters.
        return CreateSignatureHelpItems(
            items, applicableSpan, state, items.IndexOf(i => i.Parameters.Length >= 2), parameterIndexOverride: -1);
    }

    private static (IList<SignatureHelpItem> items, int? selectedItem) Filter(IList<SignatureHelpItem> items, ImmutableArray<string> parameterNames, int? selectedItem)
    {
        if (parameterNames.IsDefault)
            return (items.ToList(), selectedItem);

        var filteredList = items.Where(i => Include(i, parameterNames)).ToList();
        var isEmpty = filteredList.Count == 0;
        if (!selectedItem.HasValue || isEmpty)
            return (isEmpty ? items.ToList() : filteredList, selectedItem);

        // adjust the selected item
        var selection = items[selectedItem.Value];
        selectedItem = filteredList.IndexOf(selection);

        return (filteredList, selectedItem);
    }

    private static bool Include(SignatureHelpItem item, ImmutableArray<string> parameterNames)
    {
        using var _ = PooledHashSet<string>.GetInstance(out var itemParameterNames);
        foreach (var parameter in item.Parameters)
            itemParameterNames.Add(parameter.Name);

        foreach (var name in parameterNames)
        {
            if (!itemParameterNames.Contains(name))
                return false;
        }

        return true;
    }

    // TODO: remove once Pythia moves to ExternalAccess APIs
    [Obsolete("Use overload without ISymbolDisplayService")]
#pragma warning disable CA1822 // Mark members as static - see obsolete comment above.
    protected SignatureHelpItem CreateItem(
#pragma warning restore CA1822 // Mark members as static
        ISymbol orderSymbol,
        SemanticModel semanticModel,
        int position,
        ISymbolDisplayService symbolDisplayService,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        bool isVariadic,
        Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
        IList<SymbolDisplayPart> prefixParts,
        IList<SymbolDisplayPart> separatorParts,
        IList<SymbolDisplayPart> suffixParts,
        IList<SignatureHelpSymbolParameter> parameters,
        IList<SymbolDisplayPart>? descriptionParts = null)
    {
        return CreateItem(orderSymbol, semanticModel, position, structuralTypeDisplayService,
            isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts);
    }

    protected static SignatureHelpItem CreateItem(
        ISymbol orderSymbol,
        SemanticModel semanticModel,
        int position,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        bool isVariadic,
        Func<CancellationToken, IEnumerable<TaggedText>>? documentationFactory,
        IList<SymbolDisplayPart> prefixParts,
        IList<SymbolDisplayPart> separatorParts,
        IList<SymbolDisplayPart> suffixParts,
        IList<SignatureHelpSymbolParameter> parameters,
        IList<SymbolDisplayPart>? descriptionParts = null)
    {
        return CreateItemImpl(orderSymbol, semanticModel, position, structuralTypeDisplayService,
            isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts);
    }

    protected static SignatureHelpItem CreateItemImpl(
        ISymbol orderSymbol,
        SemanticModel semanticModel,
        int position,
        IStructuralTypeDisplayService structuralTypeDisplayService,
        bool isVariadic,
        Func<CancellationToken, IEnumerable<TaggedText>>? documentationFactory,
        IList<SymbolDisplayPart> prefixParts,
        IList<SymbolDisplayPart> separatorParts,
        IList<SymbolDisplayPart> suffixParts,
        IList<SignatureHelpSymbolParameter> parameters,
        IList<SymbolDisplayPart>? descriptionParts)
    {
        descriptionParts = descriptionParts == null
            ? SpecializedCollections.EmptyList<SymbolDisplayPart>()
            : descriptionParts;

        var allParts = prefixParts.Concat(separatorParts)
                                  .Concat(suffixParts)
                                  .Concat(parameters.SelectMany(p => p.GetAllParts()))
                                  .Concat(descriptionParts);

        var structuralTypes =
            from part in allParts
            where part.Symbol.IsAnonymousType() || part.Symbol.IsTupleType()
            select (INamedTypeSymbol)part.Symbol!;

        var info = structuralTypeDisplayService.GetTypeDisplayInfo(
            orderSymbol, structuralTypes.ToImmutableArray(), semanticModel, position);

        if (info.TypesParts.Count > 0)
        {
            var structuralTypeParts = new List<SymbolDisplayPart>
            {
                new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, "\r\n\r\n")
            };

            structuralTypeParts.AddRange(info.TypesParts);

            return new SymbolKeySignatureHelpItem(
                orderSymbol,
                isVariadic,
                documentationFactory,
                info.ReplaceStructuralTypes(prefixParts, semanticModel, position).ToTaggedText(),
                info.ReplaceStructuralTypes(separatorParts, semanticModel, position).ToTaggedText(),
                info.ReplaceStructuralTypes(suffixParts, semanticModel, position).ToTaggedText(),
                parameters.Select(p => ReplaceStructuralTypes(p, info, semanticModel, position)).Select(p => (SignatureHelpParameter)p),
                structuralTypeParts.ToTaggedText());
        }

        return new SymbolKeySignatureHelpItem(
            orderSymbol,
            isVariadic,
            documentationFactory,
            prefixParts.ToTaggedText(),
            separatorParts.ToTaggedText(),
            suffixParts.ToTaggedText(),
            parameters.Select(p => (SignatureHelpParameter)p),
            descriptionParts.ToTaggedText());
    }

    private static SignatureHelpSymbolParameter ReplaceStructuralTypes(
        SignatureHelpSymbolParameter parameter,
        StructuralTypeDisplayInfo info,
        SemanticModel semanticModel,
        int position)
    {
        return new SignatureHelpSymbolParameter(
            parameter.Name,
            parameter.IsOptional,
            parameter.DocumentationFactory,
            info.ReplaceStructuralTypes(parameter.DisplayParts, semanticModel, position),
            info.ReplaceStructuralTypes(parameter.SelectedDisplayParts, semanticModel, position));
    }

    public async Task<SignatureHelpItems?> GetItemsAsync(
        Document document, int position, SignatureHelpTriggerInfo triggerInfo, SignatureHelpOptions options, CancellationToken cancellationToken)
    {
        var itemsForCurrentDocument = await GetItemsWorkerAsync(document, position, triggerInfo, options, cancellationToken).ConfigureAwait(false);
        if (itemsForCurrentDocument == null)
        {
            return itemsForCurrentDocument;
        }

        var relatedDocuments = await FindActiveRelatedDocumentsAsync(position, document, cancellationToken).ConfigureAwait(false);
        if (relatedDocuments.IsEmpty)
        {
            return itemsForCurrentDocument;
        }

        var totalProjects = relatedDocuments.Select(d => d.Project.Id).Concat(document.Project.Id);

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
        var compilation = semanticModel.Compilation;

        var finalItems = new List<SignatureHelpItem>();
        foreach (var item in itemsForCurrentDocument.Items)
        {
            if (item is not SymbolKeySignatureHelpItem symbolKeyItem ||
                symbolKeyItem.SymbolKey is not SymbolKey symbolKey ||
                symbolKey.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken).Symbol is not ISymbol symbol)
            {
                finalItems.Add(item);
                continue;
            }

            // If the symbol is an instantiated generic method, ensure we use its original
            // definition for symbol key resolution in related compilations.
            if (symbol is IMethodSymbol methodSymbol && methodSymbol.IsGenericMethod && methodSymbol != methodSymbol.OriginalDefinition)
            {
                symbolKey = SymbolKey.Create(methodSymbol.OriginalDefinition, cancellationToken);
            }

            var invalidProjectsForCurrentSymbol = new List<ProjectId>();
            foreach (var relatedDocument in relatedDocuments)
            {
                // Try to resolve symbolKey in each related compilation,
                // unresolvable key means the symbol is unavailable in the corresponding project.
                var relatedSemanticModel = await relatedDocument.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
                if (symbolKey.Resolve(relatedSemanticModel.Compilation, ignoreAssemblyKey: true, cancellationToken).Symbol == null)
                {
                    invalidProjectsForCurrentSymbol.Add(relatedDocument.Project.Id);
                }
            }

            var platformData = new SupportedPlatformData(document.Project.Solution, invalidProjectsForCurrentSymbol, totalProjects);
            finalItems.Add(UpdateItem(item, platformData));
        }

        return new SignatureHelpItems(
            finalItems, itemsForCurrentDocument.ApplicableSpan,
            itemsForCurrentDocument.ArgumentIndex,
            itemsForCurrentDocument.ArgumentCount,
            itemsForCurrentDocument.ArgumentName,
            itemsForCurrentDocument.SelectedItemIndex);
    }

    private static async Task<ImmutableArray<Document>> FindActiveRelatedDocumentsAsync(int position, Document document, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<Document>.GetInstance(out var builder);
        foreach (var relatedDocument in document.GetLinkedDocuments())
        {
            var syntaxTree = await relatedDocument.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!relatedDocument.GetRequiredLanguageService<ISyntaxFactsService>().IsInInactiveRegion(syntaxTree, position, cancellationToken))
            {
                builder.Add(relatedDocument);
            }
        }

        return builder.ToImmutable();
    }

    private static SignatureHelpItem UpdateItem(SignatureHelpItem item, SupportedPlatformData platformData)
    {
        var platformParts = platformData.ToDisplayParts().ToTaggedText();
        if (platformParts.Length == 0)
        {
            return item;
        }

        var startingNewLine = new List<TaggedText>();
        startingNewLine.AddLineBreak();

        var concatted = startingNewLine.Concat(platformParts);
        var updatedDescription = item.DescriptionParts.IsDefault
            ? concatted
            : item.DescriptionParts.Concat(concatted);

        item.DescriptionParts = updatedDescription.ToImmutableArrayOrEmpty();
        return item;
    }

    protected static int? TryGetSelectedIndex<TSymbol>(ImmutableArray<TSymbol> candidates, ISymbol? currentSymbol) where TSymbol : class, ISymbol
    {
        if (currentSymbol is TSymbol matched)
        {
            var found = candidates.IndexOf(matched);
            if (found >= 0)
                return found;
        }

        return null;
    }
}
