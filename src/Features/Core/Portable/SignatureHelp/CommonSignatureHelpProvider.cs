// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract partial class CommonSignatureHelpProvider : SignatureHelpProvider
    {
        protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutParametersFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeParameters);

        protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutTypeParametersFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions & ~SymbolDisplayGenericsOptions.IncludeTypeParameters);


        protected abstract SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken);

        protected abstract Task ProvideSignaturesWorkerAsync(SignatureContext context);

        public override async Task ProvideSignaturesAsync(SignatureContext context)
        {
            await ProvideSignaturesWorkerAsync(context).ConfigureAwait(false);
            if (context.Items.Count == 0)
            {
                return;
            }

            var relatedDocuments = context.Document.GetLinkedDocumentIds();
            if (!relatedDocuments.Any())
            {
                return;
            }

            var document = context.Document;
            var position = context.Position;
            var trigger = context.Trigger;
            var cancellationToken = context.CancellationToken;

            var relatedDocumentsAndItems = await GetItemsForRelatedDocuments(context, relatedDocuments).ConfigureAwait(false);
            var candidateLinkedProjectsAndSymbolSets = await ExtractSymbolsFromRelatedItems(position, relatedDocumentsAndItems, cancellationToken).ConfigureAwait(false);

            var totalProjects = candidateLinkedProjectsAndSymbolSets.Select(c => c.Item1).Concat(document.Project.Id);

            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
            var compilation = semanticModel.Compilation;
            var finalItems = new List<SignatureHelpItem>();
            foreach (var item in context.Items)
            {
                var symbolKey = item.GetSymbolKey();
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
                finalItems.Add(UpdateItem(item, platformData, expectedSymbol));
            }

            context.UpdateItems(finalItems);
        }

         protected SignatureHelpItem CreateItem(
            ISymbol orderSymbol,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService,
            bool isVariadic,
            IList<SymbolDisplayPart> prefixParts,
            IList<SymbolDisplayPart> separatorParts,
            IList<SymbolDisplayPart> suffixParts,
            IList<CommonParameterData> parameters,
            IList<SymbolDisplayPart> descriptionParts = null)
        {
            prefixParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(prefixParts, semanticModel, position, symbolDisplayService);
            separatorParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(separatorParts, semanticModel, position, symbolDisplayService);
            suffixParts = anonymousTypeDisplayService.InlineDelegateAnonymousTypes(suffixParts, semanticModel, position, symbolDisplayService);
            parameters = parameters.Select(p => InlineDelegateAnonymousTypes(p, semanticModel, position, symbolDisplayService, anonymousTypeDisplayService)).ToList();
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
                orderSymbol, directAnonymousTypeReferences, semanticModel, position, symbolDisplayService);

            if (info.AnonymousTypesParts.Count > 0)
            {
                var anonymousTypeParts = new List<SymbolDisplayPart>
                {
                    new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, "\r\n\r\n")
                };

                anonymousTypeParts.AddRange(info.AnonymousTypesParts);

                return SignatureHelpItem.Create(
                    isVariadic,
                    info.ReplaceAnonymousTypes(prefixParts).ToTaggedText(),
                    info.ReplaceAnonymousTypes(separatorParts).ToTaggedText(),
                    info.ReplaceAnonymousTypes(suffixParts).ToTaggedText(),
                    parameters.Select(p => ReplaceAnonymousTypes(p, info)).Select(p => (SignatureHelpParameter)p).ToImmutableArray(),
                    anonymousTypeParts.ToTaggedText())
                    .WithSymbol(orderSymbol)
                    .WithPosition(position);
            }

            return SignatureHelpItem.Create(
                isVariadic,
                prefixParts.ToTaggedText(),
                separatorParts.ToTaggedText(),
                suffixParts.ToTaggedText(),
                parameters.Select(p => (SignatureHelpParameter)p).ToImmutableArray(),
                descriptionParts.ToTaggedText())
                .WithSymbol(orderSymbol)
                .WithPosition(position);
        }

        private CommonParameterData ReplaceAnonymousTypes(
            CommonParameterData parameter,
            AnonymousTypeDisplayInfo info)
        {
            return new CommonParameterData(
                parameter.Name,
                parameter.IsOptional,
                parameter.Symbol,
                parameter.Position,
                displayParts: info.ReplaceAnonymousTypes(parameter.DisplayParts).ToImmutableArrayOrEmpty(),
                selectedDisplayParts: info.ReplaceAnonymousTypes(parameter.SelectedDisplayParts).ToImmutableArrayOrEmpty());
        }

        private CommonParameterData InlineDelegateAnonymousTypes(
            CommonParameterData parameter,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService)
        {
            return new CommonParameterData(
                parameter.Name,
                parameter.IsOptional,
                parameter.Symbol,
                position,
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.DisplayParts, semanticModel, position, symbolDisplayService).ToImmutableArray(),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.PrefixDisplayParts, semanticModel, position, symbolDisplayService).ToImmutableArray(),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SuffixDisplayParts, semanticModel, position, symbolDisplayService).ToImmutableArray(),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SelectedDisplayParts, semanticModel, position, symbolDisplayService).ToImmutableArray(),
                properties: parameter.Properties);
        }

#if REMOVE
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
                finalItems.Add(UpdateItem(item, platformData, expectedSymbol));
            }

            return new SignatureHelpItems(
                finalItems, itemsForCurrentDocument.ApplicableSpan,
                itemsForCurrentDocument.ArgumentIndex,
                itemsForCurrentDocument.ArgumentCount,
                itemsForCurrentDocument.ArgumentName,
                itemsForCurrentDocument.SelectedItemIndex);
        }
#endif

        private async Task<List<Tuple<ProjectId, ISet<ISymbol>>>> ExtractSymbolsFromRelatedItems(int position, List<Tuple<Document, IEnumerable<SignatureHelpItem>>> relatedDocuments, CancellationToken cancellationToken)
        {
            var resultSets = new List<Tuple<ProjectId, ISet<ISymbol>>>();
            foreach (var related in relatedDocuments)
            {
                // If we don't have symbol keys, give up.
                if (related.Item2.Any(s => s.GetSymbolKey() == null))
                {
                    continue;
                }

                var syntaxTree = await related.Item1.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                if (!related.Item1.GetLanguageService<ISyntaxFactsService>().IsInInactiveRegion(syntaxTree, position, cancellationToken))
                {
                    var relatedSemanticModel = await related.Item1.GetSemanticModelForSpanAsync(new TextSpan(position, 0), cancellationToken).ConfigureAwait(false);
                    var symbolSet = related.Item2.Select(s => s.GetSymbolKey()?.Resolve(relatedSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol)
                                                 .WhereNotNull()
                                                 .ToSet(SymbolEquivalenceComparer.IgnoreAssembliesInstance);
                    resultSets.Add(Tuple.Create(related.Item1.Project.Id, symbolSet));
                }
            }

            return resultSets;
        }

        private SignatureHelpItem UpdateItem(SignatureHelpItem item, SupportedPlatformData platformData, ISymbol symbol)
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

            return item.WithDescriptionParts(updatedDescription.ToImmutableArray());
        }

        private async Task<List<Tuple<Document, IEnumerable<SignatureHelpItem>>>> GetItemsForRelatedDocuments(SignatureContext context, IEnumerable<DocumentId> relatedDocuments)
        {
            var supportedPlatforms = new List<Tuple<Document, IEnumerable<SignatureHelpItem>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = context.Document.Project.Solution.GetDocument(relatedDocumentId);
                var semanticModel = await relatedDocument.GetSemanticModelForSpanAsync(new TextSpan(context.Position, 0), context.CancellationToken).ConfigureAwait(false);
                var newContext = new SignatureContext(this, relatedDocument, context.Position, context.Trigger, context.Options, context.CancellationToken);
                await ProvideSignaturesWorkerAsync(newContext).ConfigureAwait(false);

                supportedPlatforms.Add(Tuple.Create(relatedDocument, (IEnumerable<SignatureHelpItem>)newContext.Items));
            }

            return supportedPlatforms;
        }

        public override async Task<ImmutableArray<TaggedText>> GetItemDocumentationAsync(Document document, SignatureHelpItem item, CancellationToken cancellationToken)
        {
            var symbol = await item.GetSymbolAsync(document, cancellationToken).ConfigureAwait(false);
            if (symbol != null)
            {
                var documentationCommentFormattingService = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
                if (documentationCommentFormattingService != null)
                {
                    var position = item.GetPosition();
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    return symbol.GetDocumentationParts(model, position, documentationCommentFormattingService, cancellationToken).ToImmutableArray();
                }
            }

            return ImmutableArray<TaggedText>.Empty;
        }

        public override async Task<ImmutableArray<TaggedText>> GetParameterDocumentationAsync(Document document, SignatureHelpParameter parameter, CancellationToken cancellationToken)
        {
            var symbol = await parameter.GetSymbolAsync(document, cancellationToken).ConfigureAwait(false);
            if (symbol != null)
            {
                var documentationCommentFormattingService = document.Project.LanguageServices.GetService<IDocumentationCommentFormattingService>();
                if (documentationCommentFormattingService != null)
                {
                    var position = parameter.GetPosition();
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    return symbol.GetDocumentationParts(model, position, documentationCommentFormattingService, cancellationToken).ToImmutableArray();
                }
            }

            return ImmutableArray<TaggedText>.Empty;
        }
    }
}
