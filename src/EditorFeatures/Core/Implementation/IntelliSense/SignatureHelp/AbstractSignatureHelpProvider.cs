// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
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

        protected static SignatureHelpItems CreateSignatureHelpItems(
            IEnumerable<SignatureHelpItem> items, TextSpan applicableSpan, SignatureHelpState state)
        {
            if (items == null || !items.Any() || state == null)
            {
                return null;
            }

            items = Filter(items, state.ArgumentNames);
            return new SignatureHelpItems(items.ToList(), applicableSpan, state.ArgumentIndex, state.ArgumentCount, state.ArgumentName);
        }

        private static IList<SignatureHelpItem> Filter(IEnumerable<SignatureHelpItem> items, IEnumerable<string> parameterNames)
        {
            if (parameterNames == null)
            {
                return items.ToList();
            }

            var filteredList = items.Where(i => Include(i, parameterNames)).ToList();
            return filteredList.Count == 0 ? items.ToList() : filteredList;
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

        protected SignatureHelpItem CreateItem(
            ISymbol orderSymbol,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            bool isVariadic,
            Func<CancellationToken, IEnumerable<SymbolDisplayPart>> documentationFactory,
            IEnumerable<SymbolDisplayPart> prefixParts,
            IEnumerable<SymbolDisplayPart> separatorParts,
            IEnumerable<SymbolDisplayPart> suffixParts,
            IEnumerable<SignatureHelpParameter> parameters,
            IEnumerable<SymbolDisplayPart> descriptionParts = null)
        {
            var item = new SymbolKeySignatureHelpItem(
                orderSymbol, isVariadic, documentationFactory, prefixParts, separatorParts,
                suffixParts, parameters, descriptionParts);

            return FixAnonymousTypeParts(orderSymbol, item, semanticModel, position, symbolDisplayService, anonymousTypeDisplayService);
        }

        private SignatureHelpItem FixAnonymousTypeParts(
            ISymbol orderSymbol, SignatureHelpItem item, SemanticModel semanticModel, int position, ISymbolDisplayService symbolDisplayService, IAnonymousTypeDisplayService anonymousTypeDisplayService)
        {
            var currentItem = new SymbolKeySignatureHelpItem(
                orderSymbol, item.IsVariadic, item.DocumentationFactory,
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(item.PrefixDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(item.SeparatorDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(item.SuffixDisplayParts, semanticModel, position, symbolDisplayService),
                item.Parameters.Select(p => InlineDelegateAnonymousTypes(p, semanticModel, position, symbolDisplayService, anonymousTypeDisplayService)),
                item.DescriptionParts);

            var directAnonymousTypeReferences =
                    from part in currentItem.GetAllParts()
                    where part.Symbol.IsNormalAnonymousType()
                    select (INamedTypeSymbol)part.Symbol;

            var info = anonymousTypeDisplayService.GetNormalAnonymousTypeDisplayInfo(
                orderSymbol, directAnonymousTypeReferences, semanticModel, position, symbolDisplayService);

            if (info.AnonymousTypesParts.Count > 0)
            {
                var anonymousTypeParts = new List<SymbolDisplayPart>
                {
                    new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, "\r\n\r\n")
                };

                anonymousTypeParts.AddRange(info.AnonymousTypesParts);

                currentItem = new SymbolKeySignatureHelpItem(
                    orderSymbol,
                    currentItem.IsVariadic,
                    currentItem.DocumentationFactory,
                    info.ReplaceAnonymousTypes(currentItem.PrefixDisplayParts),
                    info.ReplaceAnonymousTypes(currentItem.SeparatorDisplayParts),
                    info.ReplaceAnonymousTypes(currentItem.SuffixDisplayParts),
                    currentItem.Parameters.Select(p => ReplaceAnonymousTypes(p, info)),
                    anonymousTypeParts);
            }

            return currentItem;
        }

        private SignatureHelpParameter ReplaceAnonymousTypes(
            SignatureHelpParameter parameter,
            AnonymousTypeDisplayInfo info)
        {
            return new SignatureHelpParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.DocumentationFactory,
                info.ReplaceAnonymousTypes(parameter.DisplayParts),
                info.ReplaceAnonymousTypes(parameter.SelectedDisplayParts));
        }

        private SignatureHelpParameter InlineDelegateAnonymousTypes(
            SignatureHelpParameter parameter,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService)
        {
            return new SignatureHelpParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.DocumentationFactory,
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.DisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.PrefixDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SuffixDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SelectedDisplayParts, semanticModel, position, symbolDisplayService));
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

            var relatedDocumentsAndItems = await GetItemsForRelatedDocuments(document, relatedDocuments, position, triggerInfo, cancellationToken).ConfigureAwait(false);
            var candidateLinkedProjectsAndSymbolSets = await ExtractSymbolsFromRelatedItems(position, relatedDocumentsAndItems, cancellationToken).ConfigureAwait(false);

            var totalProjects = candidateLinkedProjectsAndSymbolSets.Select(c => c.Item1).Concat(document.Project.Id);

            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var finalItems = new List<SignatureHelpItem>();
            foreach (var item in itemsForCurrentDocument.Items)
            {
                var symbolKey = ((SymbolKeySignatureHelpItem)item).SymbolKey;
                if (symbolKey == null)
                {
                    finalItems.Add(item);
                    continue;
                }

                var expectedSymbol = symbolKey.Resolve(compilation, ignoreAssemblyKey: true, cancellationToken: cancellationToken).Symbol;

                if (expectedSymbol == null)
                {
                    finalItems.Add(item);
                    continue;
                }

                var invalidProjectsForCurrentSymbol = candidateLinkedProjectsAndSymbolSets.Where(c => !c.Item2.Contains(expectedSymbol, LinkedFilesSymbolEquivalenceComparer.Instance))
                                                                        .Select(c => c.Item1)
                                                                        .ToList();

                var platformData = new SupportedPlatformData(invalidProjectsForCurrentSymbol, totalProjects, document.Project.Solution.Workspace);
                finalItems.Add(UpdateItem(item, platformData, expectedSymbol));
            }

            return new SignatureHelpItems(
                finalItems, itemsForCurrentDocument.ApplicableSpan,
                itemsForCurrentDocument.ArgumentIndex,
                itemsForCurrentDocument.ArgumentCount,
                itemsForCurrentDocument.ArgumentName,
                itemsForCurrentDocument.SelectedItemIndex);
        }

        private async Task<List<Tuple<ProjectId, ISet<ISymbol>>>> ExtractSymbolsFromRelatedItems(int position, List<Tuple<Document, IEnumerable<SignatureHelpItem>>> relatedDocuments, CancellationToken cancellationToken)
        {
            var resultSets = new List<Tuple<ProjectId, ISet<ISymbol>>>();
            foreach (var related in relatedDocuments)
            {
                // If we don't have symbol keys, give up.
                if (related.Item2.Any(s => ((SymbolKeySignatureHelpItem)s).SymbolKey == null))
                {
                    continue;
                }

                var syntaxTree = await related.Item1.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (!related.Item1.GetLanguageService<ISyntaxFactsService>().IsInInactiveRegion(syntaxTree, position, cancellationToken))
                {
                    var relatedSemanticModel = await related.Item1.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
                    var symbolSet = related.Item2.Select(s => ((SymbolKeySignatureHelpItem)s).SymbolKey.Resolve(relatedSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol)
                                                 .WhereNotNull()
                                                 .ToSet(SymbolEquivalenceComparer.IgnoreAssembliesInstance);
                    resultSets.Add(Tuple.Create(related.Item1.Project.Id, symbolSet));
                }
            }

            return resultSets;
        }

        private SignatureHelpItem UpdateItem(SignatureHelpItem item, SupportedPlatformData platformData, ISymbol symbol)
        {
            var platformParts = platformData.ToDisplayParts();
            if (platformParts.Count == 0)
            {
                return item;
            }

            var startingNewLine = new List<SymbolDisplayPart>();
            startingNewLine.AddLineBreak();

            var updatedDescription = item.DescriptionParts == null
                ? item.DescriptionParts.Concat(startingNewLine.Concat(platformParts))
                : startingNewLine.Concat(platformParts);

            item.DescriptionParts = updatedDescription.ToImmutableArrayOrEmpty();
            return item;
        }

        protected async Task<List<Tuple<Document, IEnumerable<SignatureHelpItem>>>> GetItemsForRelatedDocuments(Document document, IEnumerable<DocumentId> relatedDocuments, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var supportedPlatforms = new List<Tuple<Document, IEnumerable<SignatureHelpItem>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = document.Project.Solution.GetDocument(relatedDocumentId);
                var semanticModel = await relatedDocument.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
                var result = await GetItemsWorkerAsync(relatedDocument, position, triggerInfo, cancellationToken).ConfigureAwait(false);

                supportedPlatforms.Add(Tuple.Create(relatedDocument, result != null ? result.Items : SpecializedCollections.EmptyEnumerable<SignatureHelpItem>()));
            }

            return supportedPlatforms;
        }
    }
}
