// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
    {
        protected override async Task<QuickInfoItem> BuildQuickInfoAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var (model, symbols, supportedPlatforms) = await ComputeQuickInfoDataAsync(document, token, cancellationToken).ConfigureAwait(false);

            if (symbols.IsDefaultOrEmpty)
            {
                return null;
            }

            return await CreateContentAsync(document.Project.Solution.Workspace,
                token, model, symbols, supportedPlatforms,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<(SemanticModel model, ImmutableArray<ISymbol> symbols, SupportedPlatformData supportedPlatforms)> ComputeQuickInfoDataAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            if (linkedDocumentIds.Any())
            {
                return await ComputeFromLinkedDocumentsAsync(document, linkedDocumentIds, token, cancellationToken).ConfigureAwait(false);
            }

            var (model, symbols) = await BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);

            return (model, symbols, supportedPlatforms: null);
        }

        private async Task<(SemanticModel model, ImmutableArray<ISymbol> symbols, SupportedPlatformData supportedPlatforms)> ComputeFromLinkedDocumentsAsync(
            Document document,
            ImmutableArray<DocumentId> linkedDocumentIds,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            // Linked files/shared projects: imagine the following when GOO is false
            // #if GOO
            // int x = 3;
            // #endif
            // var y = x$$;
            //
            // 'x' will bind as an error type, so we'll show incorrect information.
            // Instead, we need to find the head in which we get the best binding,
            // which in this case is the one with no errors.

            var (model, symbols) = await BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);

            var candidateProjects = new List<ProjectId>() { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<(DocumentId docId, SemanticModel model, ImmutableArray<ISymbol> symbols)>
            {
                (document.Id, model, symbols)
            };

            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetDocument(linkedDocumentId);
                var linkedToken = await FindTokenInLinkedDocumentAsync(token, document, linkedDocument, cancellationToken).ConfigureAwait(false);

                if (linkedToken != default)
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(linkedDocumentId.ProjectId);
                    var (linkedModel, linkedSymbols) = await BindTokenAsync(linkedDocument, linkedToken, cancellationToken).ConfigureAwait(false);
                    candidateResults.Add((linkedDocumentId, linkedModel, linkedSymbols));
                }
            }

            // Take the first result with no errors.
            // If every file binds with errors, take the first candidate, which is from the current file.
            var bestBinding = candidateResults.FirstOrNull(c => HasNoErrors(c.symbols))
                ?? candidateResults.First();

            if (bestBinding.symbols.IsDefaultOrEmpty)
            {
                return default;
            }

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.symbols.Intersect(bestBinding.symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                {
                    invalidProjects.Add(candidate.docId.ProjectId);
                }
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, document.Project.Solution.Workspace);

            return (bestBinding.model, bestBinding.symbols, supportedPlatforms);
        }

        private static bool HasNoErrors(ImmutableArray<ISymbol> symbols)
            => symbols.Length > 0
                && !ErrorVisitor.ContainsError(symbols.FirstOrDefault());

        private async Task<SyntaxToken> FindTokenInLinkedDocumentAsync(
            SyntaxToken token,
            Document originalDocument,
            Document linkedDocument,
            CancellationToken cancellationToken)
        {
            if (!linkedDocument.SupportsSyntaxTree)
            {
                return default;
            }

            var root = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                // Don't search trivia because we want to ignore inactive regions
                var linkedToken = root.FindToken(token.SpanStart);

                // The new and old tokens should have the same span?
                if (token.Span == linkedToken.Span)
                {
                    return linkedToken;
                }
            }
            catch (Exception thrownException)
            {
                // We are seeing linked files with different spans cause FindToken to crash.
                // Capturing more information for https://devdiv.visualstudio.com/DevDiv/_workitems?id=209299
                var originalText = await originalDocument.GetTextAsync().ConfigureAwait(false);
                var linkedText = await linkedDocument.GetTextAsync().ConfigureAwait(false);
                var linkedFileException = new LinkedFileDiscrepancyException(thrownException, originalText.ToString(), linkedText.ToString());

                // This problem itself does not cause any corrupted state, it just changes the set
                // of symbols included in QuickInfo, so we report and continue running.
                FatalError.ReportWithoutCrash(linkedFileException);
            }

            return default;
        }

        protected async Task<QuickInfoItem> CreateContentAsync(
            Workspace workspace,
            SyntaxToken token,
            SemanticModel semanticModel,
            IEnumerable<ISymbol> symbols,
            SupportedPlatformData supportedPlatforms,
            CancellationToken cancellationToken)
        {
            var descriptionService = workspace.Services.GetLanguageServices(token.Language).GetService<ISymbolDisplayService>();
            var formatter = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<IDocumentationCommentFormattingService>();
            var syntaxFactsService = workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>();
            var showWarningGlyph = supportedPlatforms != null && supportedPlatforms.HasValidAndInvalidProjects();
            var showSymbolGlyph = true;

            var groups = await descriptionService.ToDescriptionGroupsAsync(workspace, semanticModel, token.SpanStart, symbols.AsImmutable(), cancellationToken).ConfigureAwait(false);

            bool TryGetGroupText(SymbolDescriptionGroups group, out ImmutableArray<TaggedText> taggedParts)
                => groups.TryGetValue(group, out taggedParts) && !taggedParts.IsDefaultOrEmpty;

            var sections = ImmutableArray.CreateBuilder<QuickInfoSection>(initialCapacity: groups.Count);

            void AddSection(string kind, ImmutableArray<TaggedText> taggedParts)
                => sections.Add(QuickInfoSection.Create(kind, taggedParts));

            if (TryGetGroupText(SymbolDescriptionGroups.MainDescription, out var mainDescriptionTaggedParts))
            {
                AddSection(QuickInfoSectionKinds.Description, mainDescriptionTaggedParts);
            }

            var documentationContent = GetDocumentationContent(symbols, groups, semanticModel, token, formatter, syntaxFactsService, cancellationToken);
            if (syntaxFactsService.IsAwaitKeyword(token) &&
                (symbols.First() as INamedTypeSymbol)?.SpecialType == SpecialType.System_Void)
            {
                documentationContent = default;
                showSymbolGlyph = false;
            }

            if (!documentationContent.IsDefaultOrEmpty)
            {
                AddSection(QuickInfoSectionKinds.DocumentationComments, documentationContent);
            }

            if (TryGetGroupText(SymbolDescriptionGroups.TypeParameterMap, out var typeParameterMapText))
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(typeParameterMapText);
                AddSection(QuickInfoSectionKinds.TypeParameters, builder.ToImmutable());
            }

            if (TryGetGroupText(SymbolDescriptionGroups.AnonymousTypes, out var anonymousTypesText))
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(anonymousTypesText);
                AddSection(QuickInfoSectionKinds.AnonymousTypes, builder.ToImmutable());
            }

            var usageTextBuilder = ImmutableArray.CreateBuilder<TaggedText>();
            if (TryGetGroupText(SymbolDescriptionGroups.AwaitableUsageText, out var awaitableUsageText))
            {
                usageTextBuilder.AddRange(awaitableUsageText);
            }

            var nullableAnalysis = TryGetNullabilityAnalysis(workspace, semanticModel, token, cancellationToken);
            if (!nullableAnalysis.IsDefaultOrEmpty)
            {
                AddSection(QuickInfoSectionKinds.NullabilityAnalysis, nullableAnalysis);
            }

            if (supportedPlatforms != null)
            {
                usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            if (usageTextBuilder.Count > 0)
            {
                AddSection(QuickInfoSectionKinds.Usage, usageTextBuilder.ToImmutable());
            }

            if (TryGetGroupText(SymbolDescriptionGroups.Exceptions, out var exceptionsText))
            {
                AddSection(QuickInfoSectionKinds.Exception, exceptionsText);
            }

            if (TryGetGroupText(SymbolDescriptionGroups.Captures, out var capturesText))
            {
                AddSection(QuickInfoSectionKinds.Captures, capturesText);
            }

            var tags = ImmutableArray<string>.Empty;
            if (showSymbolGlyph)
            {
                tags = tags.AddRange(GlyphTags.GetTags(symbols.First().GetGlyph()));
            }

            if (showWarningGlyph)
            {
                tags = tags.Add(WellKnownTags.Warning);
            }

            return QuickInfoItem.Create(token.Span, tags, sections.ToImmutable());
        }

        private ImmutableArray<TaggedText> GetDocumentationContent(
            IEnumerable<ISymbol> symbols,
            IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> sections,
            SemanticModel semanticModel,
            SyntaxToken token,
            IDocumentationCommentFormattingService formatter,
            ISyntaxFactsService syntaxFactsService,
            CancellationToken cancellationToken)
        {
            if (sections.TryGetValue(SymbolDescriptionGroups.Documentation, out var parts))
            {
                var documentationBuilder = new List<TaggedText>();
                documentationBuilder.AddRange(sections[SymbolDescriptionGroups.Documentation]);
                return documentationBuilder.ToImmutableArray();
            }
            else if (symbols.Any())
            {
                var symbol = symbols.First().OriginalDefinition;

                // if generating quick info for an attribute, bind to the class instead of the constructor
                if (syntaxFactsService.IsAttributeName(token.Parent) &&
                    symbol.ContainingType?.IsAttribute() == true)
                {
                    symbol = symbol.ContainingType;
                }

                var documentation = symbol.GetDocumentationParts(semanticModel, token.SpanStart, formatter, cancellationToken);

                if (documentation != null)
                {
                    return documentation.ToImmutableArray();
                }
            }

            return default;
        }

        protected abstract bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, out SyntaxNode found);
        protected abstract bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, out SyntaxNode found);

        protected virtual ImmutableArray<TaggedText> TryGetNullabilityAnalysis(Workspace workspace, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken) => default;

        private async Task<(SemanticModel semanticModel, ImmutableArray<ISymbol> symbols)> BindTokenAsync(
            Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = GetSymbolsFromToken(token, document.Project.Solution.Workspace, semanticModel, cancellationToken);

            var bindableParent = syntaxFacts.GetBindableParent(token);
            var overloads = semanticModel.GetMemberGroup(bindableParent, cancellationToken);

            symbols = symbols.Where(IsOk)
                             .Where(s => IsAccessible(s, enclosingType))
                             .Concat(overloads)
                             .Distinct(SymbolEquivalenceComparer.Instance)
                             .ToImmutableArray();

            if (symbols.Any())
            {
                var discardSymbols = (symbols.First() as ITypeParameterSymbol)?.TypeParameterKind == TypeParameterKind.Cref;
                return (semanticModel, discardSymbols ? ImmutableArray<ISymbol>.Empty : symbols);
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return (semanticModel, ImmutableArray.Create<ISymbol>(typeInfo.Type));
                }
            }

            return (semanticModel, ImmutableArray<ISymbol>.Empty);
        }

        private ImmutableArray<ISymbol> GetSymbolsFromToken(SyntaxToken token, Workspace workspace, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (GetBindableNodeForTokenIndicatingLambda(token, out var lambdaSyntax))
            {
                return ImmutableArray.Create(semanticModel.GetSymbolInfo(lambdaSyntax, cancellationToken).Symbol);
            }

            if (GetBindableNodeForTokenIndicatingPossibleIndexerAccess(token, out var elementAccessExpression))
            {
                var symbol = semanticModel.GetSymbolInfo(elementAccessExpression, cancellationToken).Symbol;
                if (symbol?.IsIndexer() == true)
                {
                    return ImmutableArray.Create(symbol);
                }
            }

            return semanticModel.GetSemanticInfo(token, workspace, cancellationToken)
                .GetSymbols(includeType: true);
        }

        private static bool IsOk(ISymbol symbol)
            => symbol != null && !symbol.IsErrorType();

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol within)
            => within == null
                || symbol.IsAccessibleWithin(within);
    }
}
