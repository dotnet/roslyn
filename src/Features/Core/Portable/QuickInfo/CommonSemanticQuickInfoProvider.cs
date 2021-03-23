﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
    {
        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var (model, tokenInformation, supportedPlatforms) = await ComputeQuickInfoDataAsync(document, token, cancellationToken).ConfigureAwait(false);

            if (tokenInformation.Symbols.IsDefaultOrEmpty)
            {
                return null;
            }

            return await CreateContentAsync(document.Project.Solution.Workspace,
                token, model, tokenInformation, supportedPlatforms,
                cancellationToken).ConfigureAwait(false);
        }

        private async Task<(SemanticModel model, TokenInformation tokenInformation, SupportedPlatformData? supportedPlatforms)> ComputeQuickInfoDataAsync(
            Document document,
            SyntaxToken token,
            CancellationToken cancellationToken)
        {
            var linkedDocumentIds = document.GetLinkedDocumentIds();
            if (linkedDocumentIds.Any())
            {
                return await ComputeFromLinkedDocumentsAsync(document, linkedDocumentIds, token, cancellationToken).ConfigureAwait(false);
            }

            var (model, tokenInformation) = await BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);

            return (model, tokenInformation, supportedPlatforms: null);
        }

        private async Task<(SemanticModel model, TokenInformation, SupportedPlatformData supportedPlatforms)> ComputeFromLinkedDocumentsAsync(
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

            var (model, tokenInformation) = await BindTokenAsync(document, token, cancellationToken).ConfigureAwait(false);

            var candidateProjects = new List<ProjectId>() { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<(DocumentId docId, SemanticModel model, TokenInformation tokenInformation)>
            {
                (document.Id, model, tokenInformation)
            };

            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = document.Project.Solution.GetRequiredDocument(linkedDocumentId);
                var linkedToken = await FindTokenInLinkedDocumentAsync(token, linkedDocument, cancellationToken).ConfigureAwait(false);

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
            var bestBinding = candidateResults.FirstOrNull(c => HasNoErrors(c.tokenInformation.Symbols))
                ?? candidateResults.First();

            if (bestBinding.tokenInformation.Symbols.IsDefaultOrEmpty)
            {
                return default;
            }

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.tokenInformation.Symbols.Intersect(bestBinding.tokenInformation.Symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                {
                    invalidProjects.Add(candidate.docId.ProjectId);
                }
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, document.Project.Solution.Workspace);

            return (bestBinding.model, bestBinding.tokenInformation, supportedPlatforms);
        }

        private static bool HasNoErrors(ImmutableArray<ISymbol> symbols)
            => symbols.Length > 0
                && !ErrorVisitor.ContainsError(symbols.FirstOrDefault());

        private static async Task<SyntaxToken> FindTokenInLinkedDocumentAsync(
            SyntaxToken token,
            Document linkedDocument,
            CancellationToken cancellationToken)
        {
            var root = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (root == null)
            {
                return default;
            }

            // Don't search trivia because we want to ignore inactive regions
            var linkedToken = root.FindToken(token.SpanStart);

            // The new and old tokens should have the same span?
            if (token.Span == linkedToken.Span)
            {
                return linkedToken;
            }

            return default;
        }

        protected static async Task<QuickInfoItem> CreateContentAsync(
            Workspace workspace,
            SyntaxToken token,
            SemanticModel semanticModel,
            TokenInformation tokenInformation,
            SupportedPlatformData? supportedPlatforms,
            CancellationToken cancellationToken)
        {
            var descriptionService = workspace.Services.GetLanguageServices(token.Language).GetRequiredService<ISymbolDisplayService>();
            var formatter = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<IDocumentationCommentFormattingService>();
            var syntaxFactsService = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<ISyntaxFactsService>();

            var showWarningGlyph = supportedPlatforms != null && supportedPlatforms.HasValidAndInvalidProjects();

            var symbols = tokenInformation.Symbols;

            // if generating quick info for an attribute, prefer bind to the class instead of the constructor
            if (syntaxFactsService.IsAttributeName(token.Parent!))
            {
                symbols = symbols.OrderBy((s1, s2) =>
                    s1.Kind == s2.Kind ? 0 :
                    s1.Kind == SymbolKind.NamedType ? -1 :
                    s2.Kind == SymbolKind.NamedType ? 1 : 0).ToImmutableArray();
            }

            var groups = await descriptionService.ToDescriptionGroupsAsync(workspace, semanticModel, token.SpanStart, symbols, cancellationToken).ConfigureAwait(false);

            var sections = ImmutableArray.CreateBuilder<QuickInfoSection>(initialCapacity: groups.Count);

            var symbol = tokenInformation.Symbols.First();
            if (tokenInformation.ShowAwaitReturn)
            {
                // We show a special message if the Task being awaited has no return
                if (symbol is INamedTypeSymbol { SpecialType: SpecialType.System_Void })
                {
                    var builder = ImmutableArray.CreateBuilder<TaggedText>();
                    builder.AddText(FeaturesResources.Awaited_task_returns_no_value);
                    AddSection(QuickInfoSectionKinds.Description, builder.ToImmutable());
                    return QuickInfoItem.Create(token.Span, sections: sections.ToImmutable());
                }
                else
                {
                    if (TryGetGroupText(SymbolDescriptionGroups.MainDescription, out var mainDescriptionTaggedParts))
                    {
                        // We'll take the existing message and wrap it with a message saying this was returned from the task.
                        var defaultSymbol = "{0}";
                        var symbolIndex = FeaturesResources.Awaited_task_returns_0.IndexOf(defaultSymbol);

                        var builder = ImmutableArray.CreateBuilder<TaggedText>();
                        builder.AddText(FeaturesResources.Awaited_task_returns_0.Substring(0, symbolIndex));
                        builder.AddRange(mainDescriptionTaggedParts);
                        builder.AddText(FeaturesResources.Awaited_task_returns_0[(symbolIndex + defaultSymbol.Length)..]);

                        AddSection(QuickInfoSectionKinds.Description, builder.ToImmutable());
                    }
                }
            }
            else
            {
                if (TryGetGroupText(SymbolDescriptionGroups.MainDescription, out var mainDescriptionTaggedParts))
                {
                    AddSection(QuickInfoSectionKinds.Description, mainDescriptionTaggedParts);
                }
            }

            if (groups.TryGetValue(SymbolDescriptionGroups.Documentation, out var docParts) && !docParts.IsDefaultOrEmpty)
                AddSection(QuickInfoSectionKinds.DocumentationComments, docParts);

            var remarksDocumentation = GetRemarksDocumentationContent(workspace, groups, semanticModel);
            if (!remarksDocumentation.IsDefaultOrEmpty)
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                if (!docParts.IsDefaultOrEmpty)
                    builder.AddLineBreak();

                builder.AddRange(remarksDocumentation);
                AddSection(QuickInfoSectionKinds.RemarksDocumentationComments, builder.ToImmutable());
            }

            if (groups.TryGetValue(SymbolDescriptionGroups.ReturnsDocumentation, out var returnsDocumentation) &&
                !returnsDocumentation.IsDefaultOrEmpty)
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(returnsDocumentation);
                AddSection(QuickInfoSectionKinds.ReturnsDocumentationComments, builder.ToImmutable());
            }

            if (groups.TryGetValue(SymbolDescriptionGroups.ValueDocumentation, out var valueDocumentation) &&
                !valueDocumentation.IsDefaultOrEmpty)
            {
                var builder = ImmutableArray.CreateBuilder<TaggedText>();
                builder.AddLineBreak();
                builder.AddRange(valueDocumentation);
                AddSection(QuickInfoSectionKinds.ValueDocumentationComments, builder.ToImmutable());
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

            var nullableMessage = tokenInformation.NullableFlowState switch
            {
                NullableFlowState.MaybeNull => string.Format(FeaturesResources._0_may_be_null_here, symbol.Name),
                NullableFlowState.NotNull => string.Format(FeaturesResources._0_is_not_null_here, symbol.Name),
                _ => null
            };

            if (nullableMessage != null)
            {
                AddSection(QuickInfoSectionKinds.NullabilityAnalysis, ImmutableArray.Create(new TaggedText(TextTags.Text, nullableMessage)));
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

            var tags = ImmutableArray.CreateRange(GlyphTags.GetTags(tokenInformation.Symbols.First().GetGlyph()));

            if (showWarningGlyph)
            {
                tags = tags.Add(WellKnownTags.Warning);
            }

            return QuickInfoItem.Create(token.Span, tags, sections.ToImmutable());

            bool TryGetGroupText(SymbolDescriptionGroups group, out ImmutableArray<TaggedText> taggedParts)
                => groups.TryGetValue(group, out taggedParts) && !taggedParts.IsDefaultOrEmpty;

            void AddSection(string kind, ImmutableArray<TaggedText> taggedParts)
                => sections.Add(QuickInfoSection.Create(kind, taggedParts));
        }

        private static ImmutableArray<TaggedText> GetRemarksDocumentationContent(
            Workspace workspace,
            IDictionary<SymbolDescriptionGroups, ImmutableArray<TaggedText>> sections,
            SemanticModel semanticModel)
        {
            if (!workspace.Options.GetOption(QuickInfoOptions.ShowRemarksInQuickInfo, semanticModel.Language))
                return default;

            return sections.TryGetValue(SymbolDescriptionGroups.RemarksDocumentation, out var parts)
                ? parts
                : default;
        }

        protected abstract bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
        protected abstract bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);

        protected virtual NullableFlowState GetNullabilityAnalysis(Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxNode node, CancellationToken cancellationToken) => NullableFlowState.None;

        private async Task<(SemanticModel semanticModel, TokenInformation tokenInformation)> BindTokenAsync(
            Document document, SyntaxToken token, CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = GetSymbolsFromToken(token, document.Project.Solution.Workspace, semanticModel, cancellationToken);

            var bindableParent = syntaxFacts.TryGetBindableParent(token);
            var overloads = bindableParent != null
                ? semanticModel.GetMemberGroup(bindableParent, cancellationToken)
                : ImmutableArray<ISymbol>.Empty;

            symbols = symbols.Where(IsOk)
                             .Where(s => IsAccessible(s, enclosingType))
                             .Concat(overloads)
                             .Distinct(SymbolEquivalenceComparer.Instance)
                             .ToImmutableArray();

            if (symbols.Any())
            {
                var firstSymbol = symbols.First();
                var isAwait = syntaxFacts.IsAwaitKeyword(token);
                var nullableFlowState = NullableFlowState.None;
                if (bindableParent != null)
                {
                    nullableFlowState = GetNullabilityAnalysis(document.Project.Solution.Workspace, semanticModel, firstSymbol, bindableParent, cancellationToken);
                }

                return (semanticModel, new TokenInformation(symbols, isAwait, nullableFlowState));
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent!, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return (semanticModel, new TokenInformation(ImmutableArray.Create<ISymbol>(typeInfo.Type)));
                }
            }

            return (semanticModel, new TokenInformation(ImmutableArray<ISymbol>.Empty));
        }

        private ImmutableArray<ISymbol> GetSymbolsFromToken(SyntaxToken token, Workspace workspace, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (GetBindableNodeForTokenIndicatingLambda(token, out var lambdaSyntax))
            {
                var symbol = semanticModel.GetSymbolInfo(lambdaSyntax, cancellationToken).Symbol;
                return symbol != null ? ImmutableArray.Create(symbol) : ImmutableArray<ISymbol>.Empty;
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

        private static bool IsOk([NotNullWhen(returnValue: true)] ISymbol? symbol)
        {
            if (symbol == null)
                return false;

            if (symbol.IsErrorType())
                return false;

            if (symbol is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Cref })
                return false;

            return true;
        }

        private static bool IsAccessible(ISymbol symbol, INamedTypeSymbol? within)
            => within == null
                || symbol.IsAccessibleWithin(within);
    }
}
