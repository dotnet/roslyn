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

            if (symbols.IsDefault || symbols.IsEmpty)
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
            var bestBinding = candidateResults.FirstOrNullable(c => HasNoErrors(c.symbols))
                ?? candidateResults.First();

            if (bestBinding.symbols.IsDefault || bestBinding.symbols.IsEmpty)
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
            var sections = new List<QuickInfoSection>(groups.Count);

            if (groups.ContainsKey(SymbolDescriptionGroups.MainDescription) && !groups[SymbolDescriptionGroups.MainDescription].IsDefaultOrEmpty)
            {
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Description, groups[SymbolDescriptionGroups.MainDescription]));
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
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.DocumentationComments, documentationContent));
            }

            if (groups.ContainsKey(SymbolDescriptionGroups.TypeParameterMap) && !groups[SymbolDescriptionGroups.TypeParameterMap].IsDefaultOrEmpty)
            {
                var builder = new List<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(groups[SymbolDescriptionGroups.TypeParameterMap]);
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.TypeParameters, builder.ToImmutableArray()));
            }

            if (groups.ContainsKey(SymbolDescriptionGroups.AnonymousTypes) && !groups[SymbolDescriptionGroups.AnonymousTypes].IsDefaultOrEmpty)
            {
                var builder = new List<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(groups[SymbolDescriptionGroups.AnonymousTypes]);
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.AnonymousTypes, builder.ToImmutableArray()));
            }

            var usageTextBuilder = new List<TaggedText>();
            if (groups.ContainsKey(SymbolDescriptionGroups.AwaitableUsageText) && !groups[SymbolDescriptionGroups.AwaitableUsageText].IsDefaultOrEmpty)
            {
                usageTextBuilder.AddRange(groups[SymbolDescriptionGroups.AwaitableUsageText]);
            }

            if (supportedPlatforms != null)
            {
                usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            if (usageTextBuilder.Count > 0)
            {
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Usage, usageTextBuilder.ToImmutableArray()));
            }

            if (groups.ContainsKey(SymbolDescriptionGroups.Exceptions) && !groups[SymbolDescriptionGroups.Exceptions].IsDefaultOrEmpty)
            {
                sections.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Exception, groups[SymbolDescriptionGroups.Exceptions]));
            }

            var tags = ImmutableArray<string>.Empty;
            if (showSymbolGlyph)
            {
                tags = tags.AddRange(GlyphTags.GetTags(symbols.First().GetGlyph()));
            }

            if (showWarningGlyph)
            {
                tags = tags.Add(Completion.CompletionTags.Warning);
            }

            return QuickInfoItem.Create(token.Span, tags: tags, sections: sections.ToImmutableArray());
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

        private async Task<(SemanticModel semanticModel, ImmutableArray<ISymbol> symbols)> BindTokenAsync(
            Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelForNodeAsync(token.Parent, cancellationToken).ConfigureAwait(false);
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = semanticModel.GetSemanticInfo(token, document.Project.Solution.Workspace, cancellationToken)
                                       .GetSymbols(includeType: true);

            var bindableParent = document.GetLanguageService<ISyntaxFactsService>().GetBindableParent(token);
            var overloads = semanticModel.GetMemberGroup(bindableParent, cancellationToken);

            symbols = symbols.Where(IsOk)
                             .Where(s => IsAccessible(s, enclosingType))
                             .Concat(overloads)
                             .Distinct(SymbolEquivalenceComparer.Instance)
                             .ToImmutableArray();

            if (symbols.Any())
            {
                var typeParameter = symbols.First() as ITypeParameterSymbol;
                return (semanticModel,
                    symbols: typeParameter != null && typeParameter.TypeParameterKind == TypeParameterKind.Cref
                        ? ImmutableArray<ISymbol>.Empty
                        : symbols);
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return (semanticModel, symbols: ImmutableArray.Create<ISymbol>(typeInfo.Type));
                }
            }

            return (semanticModel, symbols: ImmutableArray<ISymbol>.Empty);
        }

        private static bool IsOk(ISymbol symbol)
            => symbol != null
                && !symbol.IsErrorType()
                && !symbol.IsAnonymousFunction();

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol within)
            => within == null
                || symbol.IsAccessibleWithin(within);
    }
}
