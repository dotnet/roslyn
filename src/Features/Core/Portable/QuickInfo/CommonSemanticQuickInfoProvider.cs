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
            var linkedDocumentIds = document.GetLinkedDocumentIds();

            var modelAndSymbols = await this.BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);
            if (modelAndSymbols.Item2.Length == 0 && !linkedDocumentIds.Any())
            {
                return null;
            }

            if (!linkedDocumentIds.Any())
            {
                return await CreateContentAsync(document.Project.Solution.Workspace,
                    token,
                    modelAndSymbols.Item1,
                    modelAndSymbols.Item2,
                    supportedPlatforms: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Linked files/shared projects: imagine the following when FOO is false
            // #if FOO
            // int x = 3;
            // #endif 
            // var y = x$$;
            //
            // 'x' will bind as an error type, so we'll show incorrect information.
            // Instead, we need to find the head in which we get the best binding, 
            // which in this case is the one with no errors.

            var candidateProjects = new List<ProjectId>() { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<Tuple<DocumentId, SemanticModel, ImmutableArray<ISymbol>>>();
            candidateResults.Add(Tuple.Create(document.Id, modelAndSymbols.Item1, modelAndSymbols.Item2));

            foreach (var link in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetDocument(link);
                var linkedToken = await FindTokenInLinkedDocument(token, document, linkedDocument, cancellationToken).ConfigureAwait(false);

                if (linkedToken != default)
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(link.ProjectId);
                    var linkedModelAndSymbols = await this.BindTokenAsync(linkedDocument, linkedToken, cancellationToken).ConfigureAwait(false);
                    candidateResults.Add(Tuple.Create(link, linkedModelAndSymbols.Item1, linkedModelAndSymbols.Item2));
                }
            }

            // Take the first result with no errors.
            var bestBinding = candidateResults.FirstOrDefault(
                c => c.Item3.Length > 0 && !ErrorVisitor.ContainsError(c.Item3.FirstOrDefault()));

            // Every file binds with errors. Take the first candidate, which is from the current file.
            if (bestBinding == null)
            {
                bestBinding = candidateResults.First();
            }

            if (bestBinding.Item3 == null || !bestBinding.Item3.Any())
            {
                return null;
            }

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.Item3.Intersect(bestBinding.Item3, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                {
                    invalidProjects.Add(candidate.Item1.ProjectId);
                }
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, document.Project.Solution.Workspace);
            return await CreateContentAsync(document.Project.Solution.Workspace, token, bestBinding.Item2, bestBinding.Item3, supportedPlatforms, cancellationToken).ConfigureAwait(false);
        }

        private async Task<SyntaxToken> FindTokenInLinkedDocument(SyntaxToken token, Document originalDocument, Document linkedDocument, CancellationToken cancellationToken)
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
                FatalError.Report(linkedFileException);
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

            var sections = await descriptionService.ToDescriptionGroupsAsync(workspace, semanticModel, token.SpanStart, symbols.AsImmutable(), cancellationToken).ConfigureAwait(false);
            var blocks = new List<QuickInfoSection>(sections.Count);

            if (sections.ContainsKey(SymbolDescriptionGroups.MainDescription) && !sections[SymbolDescriptionGroups.MainDescription].IsDefaultOrEmpty)
            {
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Description, sections[SymbolDescriptionGroups.MainDescription]));
            }

            var documentationContent = GetDocumentationContent(symbols, sections, semanticModel, token, formatter, syntaxFactsService, cancellationToken);
            if (workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISyntaxFactsService>().IsAwaitKeyword(token) &&
                (symbols.First() as INamedTypeSymbol)?.SpecialType == SpecialType.System_Void)
            {
                documentationContent = default;
                showSymbolGlyph = false;
            }

            if (!documentationContent.IsDefaultOrEmpty)
            {
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.DocumentationComments, documentationContent));
            }

            if (sections.ContainsKey(SymbolDescriptionGroups.TypeParameterMap) && !sections[SymbolDescriptionGroups.TypeParameterMap].IsDefaultOrEmpty)
            {
                var builder = new List<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(sections[SymbolDescriptionGroups.TypeParameterMap]);
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.TypeParameters, builder.ToImmutableArray()));
            }

            if (sections.ContainsKey(SymbolDescriptionGroups.AnonymousTypes) && !sections[SymbolDescriptionGroups.AnonymousTypes].IsDefaultOrEmpty)
            {
                var builder = new List<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(sections[SymbolDescriptionGroups.AnonymousTypes]);
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.AnonymousTypes, builder.ToImmutableArray()));
            }

            var usageTextBuilder = new List<TaggedText>();
            if (sections.ContainsKey(SymbolDescriptionGroups.AwaitableUsageText) && !sections[SymbolDescriptionGroups.AwaitableUsageText].IsDefaultOrEmpty)
            {
                usageTextBuilder.AddRange(sections[SymbolDescriptionGroups.AwaitableUsageText]);
            }

            if (supportedPlatforms != null)
            {
                usageTextBuilder.AddRange(supportedPlatforms.ToDisplayParts().ToTaggedText());
            }

            if (usageTextBuilder.Count > 0)
            {
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Usage, usageTextBuilder.ToImmutableArray()));
            }

            if (sections.ContainsKey(SymbolDescriptionGroups.Exceptions) && !sections[SymbolDescriptionGroups.Exceptions].IsDefaultOrEmpty)
            {
                blocks.Add(QuickInfoSection.Create(QuickInfoSectionKinds.Exception, sections[SymbolDescriptionGroups.Exceptions]));
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

            return QuickInfoItem.Create(token.Span, tags: tags, sections: blocks.ToImmutableArray());
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

        private async Task<ValueTuple<SemanticModel, ImmutableArray<ISymbol>>> BindTokenAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
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
                return ValueTuple.Create(
                    semanticModel,
                    typeParameter != null && typeParameter.TypeParameterKind == TypeParameterKind.Cref
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
                    return ValueTuple.Create(semanticModel,
                        ImmutableArray.Create<ISymbol>(typeInfo.Type));
                }
            }

            return ValueTuple.Create(semanticModel, ImmutableArray<ISymbol>.Empty);
        }

        private static bool IsOk(ISymbol symbol)
        {
            return symbol != null && !symbol.IsErrorType() && !symbol.IsAnonymousFunction();
        }

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol within)
        {
            return within == null || symbol.IsAccessibleWithin(within);
        }
    }
}
