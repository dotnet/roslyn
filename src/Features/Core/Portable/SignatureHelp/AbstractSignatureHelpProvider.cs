// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
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
        public abstract SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken);

        protected abstract Task<SignatureHelpItems> GetItemsWorkerAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken);

        /// <remarks>
        /// This overload is required for compatibility with existing extensions.
        /// </remarks>
        protected static SignatureHelpItems CreateSignatureHelpItems(
            IList<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState state)
        {
            return CreateSignatureHelpItems(items, applicableSpan, state, selectedItem: null);
        }

        protected static SignatureHelpItems CreateSignatureHelpItems(
            IList<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState state, int? selectedItem)
        {
            if (items == null || !items.Any() || state == null)
            {
                return null;
            }

            if (selectedItem < 0)
            {
                selectedItem = null;
            }

            (items, selectedItem) = Filter(items, state.ArgumentNames, selectedItem);
            return new SignatureHelpItems(items, applicableSpan, state.ArgumentIndex, state.ArgumentCount, state.ArgumentName, selectedItem);
        }

        protected static SignatureHelpItems CreateCollectionInitializerSignatureHelpItems(
            IList<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState state)
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
                items, applicableSpan, state, items.IndexOf(i => i.Parameters.Length >= 2));
        }

        private static (IList<SignatureHelpItem> items, int? selectedItem) Filter(IList<SignatureHelpItem> items, IEnumerable<string> parameterNames, int? selectedItem)
        {
            if (parameterNames == null)
            {
                return (items.ToList(), selectedItem);
            }

            var filteredList = items.Where(i => Include(i, parameterNames)).ToList();
            var isEmpty = filteredList.Count == 0;
            if (!selectedItem.HasValue || isEmpty)
            {
                return (isEmpty ? items.ToList() : filteredList, selectedItem);
            }

            // adjust the selected item
            var selection = items[selectedItem.Value];
            selectedItem = filteredList.IndexOf(selection);
            return (filteredList, selectedItem);
        }

        private static bool Include(SignatureHelpItem item, IEnumerable<string> parameterNames)
        {
            var itemParameterNames = item.Parameters.Select(p => p.Name).ToSet();
            return parameterNames.All(itemParameterNames.Contains);
        }

        public async Task<SignatureHelpState> GetCurrentArgumentStateAsync(Document document, int position, TextSpan currentSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return GetCurrentArgumentState(root, position, document.GetLanguageService<ISyntaxFactsService>(), currentSpan, cancellationToken);
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
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            bool isVariadic,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IList<SymbolDisplayPart> prefixParts,
            IList<SymbolDisplayPart> separatorParts,
            IList<SymbolDisplayPart> suffixParts,
            IList<SignatureHelpSymbolParameter> parameters,
            IList<SymbolDisplayPart> descriptionParts = null)
        {
            return CreateItem(orderSymbol, semanticModel, position, anonymousTypeDisplayService,
                isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts);
        }

        protected static SignatureHelpItem CreateItem(
            ISymbol orderSymbol,
            SemanticModel semanticModel,
            int position,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            bool isVariadic,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IList<SymbolDisplayPart> prefixParts,
            IList<SymbolDisplayPart> separatorParts,
            IList<SymbolDisplayPart> suffixParts,
            IList<SignatureHelpSymbolParameter> parameters,
            IList<SymbolDisplayPart> descriptionParts = null)
        {
            return CreateItemImpl(orderSymbol, semanticModel, position, anonymousTypeDisplayService,
                isVariadic, documentationFactory, prefixParts, separatorParts, suffixParts, parameters, descriptionParts);
        }

        protected static SignatureHelpItem CreateItemImpl(
            ISymbol orderSymbol,
            SemanticModel semanticModel,
            int position,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            bool isVariadic,
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IList<SymbolDisplayPart> prefixParts,
            IList<SymbolDisplayPart> separatorParts,
            IList<SymbolDisplayPart> suffixParts,
            IList<SignatureHelpSymbolParameter> parameters,
            IList<SymbolDisplayPart> descriptionParts)
        {
            prefixParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(prefixParts, semanticModel, position);
            separatorParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(separatorParts, semanticModel, position);
            suffixParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(suffixParts, semanticModel, position);
            parameters = parameters.Select(p => InlineDelegateAnonymousTypes(p, semanticModel, position, anonymousTypeDisplayService)).ToList();
            descriptionParts = descriptionParts == null
                ? SpecializedCollections.EmptyList<SymbolDisplayPart>()
                : descriptionParts;

            var allParts = prefixParts.Concat(separatorParts)
                                      .Concat(suffixParts)
                                      .Concat(parameters.SelectMany(p => p.GetAllParts()))
                                      .Concat(descriptionParts);

            var directAnonymousTypeReferences =
                from part in allParts
                where part.Symbol.IsNormalAnonymousType()
                select (INamedTypeSymbol)part.Symbol;

            var info = anonymousTypeDisplayService.GetNormalAnonymousTypeDisplayInfo(
                orderSymbol, directAnonymousTypeReferences, semanticModel, position);

            if (info.AnonymousTypesParts.Count > 0)
            {
                var anonymousTypeParts = new List<SymbolDisplayPart>
                {
                    new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, "\r\n\r\n")
                };

                anonymousTypeParts.AddRange(info.AnonymousTypesParts);

                return new SymbolKeySignatureHelpItem(
                    orderSymbol,
                    isVariadic,
                    documentationFactory,
                    info.ReplaceAnonymousTypes(prefixParts).ToTaggedText(),
                    info.ReplaceAnonymousTypes(separatorParts).ToTaggedText(),
                    info.ReplaceAnonymousTypes(suffixParts).ToTaggedText(),
                    parameters.Select(p => ReplaceAnonymousTypes(p, info)).Select(p => (SignatureHelpParameter)p),
                    anonymousTypeParts.ToTaggedText());
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

        private static SignatureHelpSymbolParameter ReplaceAnonymousTypes(
            SignatureHelpSymbolParameter parameter,
            AnonymousTypeDisplayInfo info)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.DocumentationFactory,
                info.ReplaceAnonymousTypes(parameter.DisplayParts),
                info.ReplaceAnonymousTypes(parameter.SelectedDisplayParts));
        }

        private static SignatureHelpSymbolParameter InlineDelegateAnonymousTypes(
            SignatureHelpSymbolParameter parameter,
            SemanticModel semanticModel,
            int position,
            IAnonymousTypeDisplayService anonymousTypeDisplayService)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.DocumentationFactory,
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.DisplayParts, semanticModel, position),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.PrefixDisplayParts, semanticModel, position),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SuffixDisplayParts, semanticModel, position),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SelectedDisplayParts, semanticModel, position));
        }

        public async Task<SignatureHelpItems> GetItemsAsync(
            Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var itemsForCurrentDocument = await GetItemsWorkerAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            if (itemsForCurrentDocument == null)
            {
                return itemsForCurrentDocument;
            }

            var relatedDocuments = document.GetLinkedDocumentIds();
            if (!relatedDocuments.Any())
            {
                return itemsForCurrentDocument;
            }

            var relatedDocumentsAndItems = await GetItemsForRelatedDocumentsAsync(document, relatedDocuments, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            var candidateLinkedProjectsAndSymbolSets = await ExtractSymbolsFromRelatedItemsAsync(position, relatedDocumentsAndItems, cancellationToken).ConfigureAwait(false);

            var totalProjects = candidateLinkedProjectsAndSymbolSets.Select(c => c.Item1).Concat(document.Project.Id);

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var finalItems = new List<SignatureHelpItem>();
            foreach (var item in itemsForCurrentDocument.Items)
            {
                var symbolKey = (item as SymbolKeySignatureHelpItem)?.SymbolKey;
                if (symbolKey == null)
                {
                    finalItems.Add(item);
                    continue;
                }

                var expectedSymbol = symbolKey.Value.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken).Symbol;
                if (expectedSymbol == null)
                {
                    finalItems.Add(item);
                    continue;
                }

                var invalidProjectsForCurrentSymbol = candidateLinkedProjectsAndSymbolSets.Where(c => !c.Item2.Contains(expectedSymbol, LinkedFilesSymbolEquivalenceComparer.Instance))
                                                                        .Select(c => c.Item1)
                                                                        .ToList();

                var platformData = new SupportedPlatformData(invalidProjectsForCurrentSymbol, totalProjects, document.Project.Solution.Workspace);
                finalItems.Add(UpdateItem(item, platformData));
            }

            return new SignatureHelpItems(
                finalItems, itemsForCurrentDocument.ApplicableSpan,
                itemsForCurrentDocument.ArgumentIndex,
                itemsForCurrentDocument.ArgumentCount,
                itemsForCurrentDocument.ArgumentName,
                itemsForCurrentDocument.SelectedItemIndex);
        }

        private static async Task<List<Tuple<ProjectId, ISet<ISymbol>>>> ExtractSymbolsFromRelatedItemsAsync(int position, List<Tuple<Document, IEnumerable<SignatureHelpItem>>> relatedDocuments, CancellationToken cancellationToken)
        {
            var resultSets = new List<Tuple<ProjectId, ISet<ISymbol>>>();
            foreach (var related in relatedDocuments)
            {
                // If we don't have symbol keys, give up.
                if (related.Item2.Any(s => (s as SymbolKeySignatureHelpItem)?.SymbolKey == null))
                {
                    continue;
                }

                var syntaxTree = await related.Item1.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (!related.Item1.GetLanguageService<ISyntaxFactsService>().IsInInactiveRegion(syntaxTree, position, cancellationToken))
                {
                    var relatedSemanticModel = await related.Item1.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
                    var symbolSet = related.Item2.Select(s => ((SymbolKeySignatureHelpItem)s).SymbolKey?.Resolve(relatedSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol)
                                                 .WhereNotNull()
                                                 .ToSet(SymbolEquivalenceComparer.IgnoreAssembliesInstance);
                    resultSets.Add(Tuple.Create(related.Item1.Project.Id, symbolSet));
                }
            }

            return resultSets;
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

        protected async Task<List<Tuple<Document, IEnumerable<SignatureHelpItem>>>> GetItemsForRelatedDocumentsAsync(Document document, IEnumerable<DocumentId> relatedDocuments, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var supportedPlatforms = new List<Tuple<Document, IEnumerable<SignatureHelpItem>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = document.Project.Solution.GetDocument(relatedDocumentId);
                var result = await GetItemsWorkerAsync(relatedDocument, position, triggerInfo, cancellationToken).ConfigureAwait(false);

                supportedPlatforms.Add(Tuple.Create(relatedDocument, result != null ? result.Items : SpecializedCollections.EmptyEnumerable<SignatureHelpItem>()));
            }

            return supportedPlatforms;
        }

        protected static int? TryGetSelectedIndex<TSymbol>(ImmutableArray<TSymbol> candidates, ISymbol currentSymbol) where TSymbol : class, ISymbol
        {
            if (currentSymbol is TSymbol matched)
            {
                var found = candidates.IndexOf(matched);
                if (found >= 0)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
