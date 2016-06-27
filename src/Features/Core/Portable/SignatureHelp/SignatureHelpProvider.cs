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

namespace Microsoft.CodeAnalysis.SignatureHelp
{
    internal abstract partial class SignatureHelpProvider : ISignatureHelpProvider
    {
        protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutParametersFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithMemberOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.MemberOptions & ~SymbolDisplayMemberOptions.IncludeParameters);

        protected static readonly SymbolDisplayFormat MinimallyQualifiedWithoutTypeParametersFormat =
            SymbolDisplayFormat.MinimallyQualifiedFormat.WithGenericsOptions(
                SymbolDisplayFormat.MinimallyQualifiedFormat.GenericsOptions & ~SymbolDisplayGenericsOptions.IncludeTypeParameters);

        protected SignatureHelpProvider()
        {
        }

        public abstract bool IsTriggerCharacter(char ch);
        public abstract bool IsRetriggerCharacter(char ch);
        protected abstract SignatureHelpState GetCurrentArgumentState(SyntaxNode root, int position, ISyntaxFactsService syntaxFacts, TextSpan currentSpan, CancellationToken cancellationToken);

        protected abstract Task ProvideSignaturesWorkerAsync(SignatureContext context);

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
            Func<CancellationToken, IEnumerable<TaggedText>> documentationFactory,
            IList<SymbolDisplayPart> prefixParts,
            IList<SymbolDisplayPart> separatorParts,
            IList<SymbolDisplayPart> suffixParts,
            IList<SignatureHelpSymbolParameter> parameters,
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

        private SignatureHelpSymbolParameter ReplaceAnonymousTypes(
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

        private SignatureHelpSymbolParameter InlineDelegateAnonymousTypes(
            SignatureHelpSymbolParameter parameter,
            SemanticModel semanticModel,
            int position,
            ISymbolDisplayService symbolDisplayService,
            IAnonymousTypeDisplayService anonymousTypeDisplayService)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.DocumentationFactory,
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.DisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.PrefixDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SuffixDisplayParts, semanticModel, position, symbolDisplayService),
                anonymousTypeDisplayService.InlineDelegateAnonymousTypes(parameter.SelectedDisplayParts, semanticModel, position, symbolDisplayService));
        }

        public async Task ProvideSignaturesAsync(SignatureContext context)
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
                var symbolKey = ((SymbolKeySignatureHelpItem)item).SymbolKey;
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
                    var symbolSet = related.Item2.Select(s => ((SymbolKeySignatureHelpItem)s).SymbolKey?.Resolve(relatedSemanticModel.Compilation, cancellationToken: cancellationToken).Symbol)
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

            item.DescriptionParts = updatedDescription.ToImmutableArrayOrEmpty();
            return item;
        }

        protected async Task<List<Tuple<Document, IEnumerable<SignatureHelpItem>>>> GetItemsForRelatedDocuments(SignatureContext context, IEnumerable<DocumentId> relatedDocuments)
        {
            var supportedPlatforms = new List<Tuple<Document, IEnumerable<SignatureHelpItem>>>();
            foreach (var relatedDocumentId in relatedDocuments)
            {
                var relatedDocument = context.Document.Project.Solution.GetDocument(relatedDocumentId);
                var semanticModel = await relatedDocument.GetSemanticModelForSpanAsync(new TextSpan(context.Position, 0), context.CancellationToken).ConfigureAwait(false);
                var newContext = new SignatureContext(relatedDocument, context.Position, context.Trigger, context.Options, context.CancellationToken);
                await ProvideSignaturesWorkerAsync(newContext).ConfigureAwait(false);

                supportedPlatforms.Add(Tuple.Create(relatedDocument, (IEnumerable<SignatureHelpItem>)newContext.Items));
            }

            return supportedPlatforms;
        }
    }
}
