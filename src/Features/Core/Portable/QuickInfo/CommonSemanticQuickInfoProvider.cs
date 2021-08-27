﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
    {
        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            QuickInfoContext context, SyntaxToken token)
        {
            var (tokenInformation, supportedPlatforms) = await ComputeQuickInfoDataAsync(context, token).ConfigureAwait(false);
            if (tokenInformation.Symbols.IsDefaultOrEmpty)
                return null;

            var cancellationToken = context.CancellationToken;
            var semanticModel = await context.Document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            return await CreateContentAsync(
                context.Document.Project.Solution.Workspace, semanticModel, token, tokenInformation, supportedPlatforms, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            CommonQuickInfoContext context, SyntaxToken token)
        {
            var tokenInformation = BindToken(context.Workspace, context.SemanticModel, token, context.CancellationToken);
            if (tokenInformation.Symbols.IsDefaultOrEmpty)
                return null;

            return await CreateContentAsync(
                context.Workspace, context.SemanticModel, token, tokenInformation, supportedPlatforms: null, context.CancellationToken).ConfigureAwait(false);
        }

        private async Task<(TokenInformation tokenInformation, SupportedPlatformData? supportedPlatforms)> ComputeQuickInfoDataAsync(
            QuickInfoContext context,
            SyntaxToken token)
        {
            var cancellationToken = context.CancellationToken;
            var document = context.Document;

            var linkedDocumentIds = document.GetLinkedDocumentIds();
            if (linkedDocumentIds.Any())
                return await ComputeFromLinkedDocumentsAsync(context, token, linkedDocumentIds).ConfigureAwait(false);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var tokenInformation = BindToken(
                document.Project.Solution.Workspace, semanticModel, token, cancellationToken);
            return (tokenInformation, supportedPlatforms: null);
        }

        private async Task<(TokenInformation, SupportedPlatformData supportedPlatforms)> ComputeFromLinkedDocumentsAsync(
            QuickInfoContext context,
            SyntaxToken token,
            ImmutableArray<DocumentId> linkedDocumentIds)
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

            var cancellationToken = context.CancellationToken;
            var document = context.Document;
            var solution = document.Project.Solution;
            var workspace = solution.Workspace;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var mainTokenInformation = BindToken(workspace, semanticModel, token, cancellationToken);

            var candidateProjects = new List<ProjectId> { document.Project.Id };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<(DocumentId docId, TokenInformation tokenInformation)>
            {
                (document.Id, mainTokenInformation)
            };

            foreach (var linkedDocumentId in linkedDocumentIds)
            {
                var linkedDocument = solution.GetRequiredDocument(linkedDocumentId);
                var linkedModel = await linkedDocument.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var linkedToken = FindTokenInLinkedDocument(token, linkedModel, cancellationToken);

                if (linkedToken != default)
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(linkedDocumentId.ProjectId);
                    var linkedSymbols = BindToken(workspace, linkedModel, linkedToken, cancellationToken);
                    candidateResults.Add((linkedDocumentId, linkedSymbols));
                }
            }

            // Take the first result with no errors.
            // If every file binds with errors, take the first candidate, which is from the current file.
            var bestBinding = candidateResults.FirstOrNull(c => HasNoErrors(c.tokenInformation.Symbols))
                ?? candidateResults.First();

            if (bestBinding.tokenInformation.Symbols.IsDefaultOrEmpty)
                return default;

            // We calculate the set of supported projects
            candidateResults.Remove(bestBinding);
            foreach (var (docId, tokenInformation) in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!tokenInformation.Symbols.Intersect(bestBinding.tokenInformation.Symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                    invalidProjects.Add(docId.ProjectId);
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, workspace);
            return (bestBinding.tokenInformation, supportedPlatforms);
        }

        private static bool HasNoErrors(ImmutableArray<ISymbol> symbols)
            => symbols.Length > 0
                && !ErrorVisitor.ContainsError(symbols.FirstOrDefault());

        private static SyntaxToken FindTokenInLinkedDocument(
            SyntaxToken token,
            SemanticModel linkedModel,
            CancellationToken cancellationToken)
        {
            var root = linkedModel.SyntaxTree.GetRoot(cancellationToken);
            if (root == null)
                return default;

            // Don't search trivia because we want to ignore inactive regions
            var linkedToken = root.FindToken(token.SpanStart);

            // The new and old tokens should have the same span?
            return token.Span == linkedToken.Span ? linkedToken : default;
        }

        protected static Task<QuickInfoItem> CreateContentAsync(
            Workspace workspace,
            SemanticModel semanticModel,
            SyntaxToken token,
            TokenInformation tokenInformation,
            SupportedPlatformData? supportedPlatforms,
            CancellationToken cancellationToken)
        {
            var syntaxFactsService = workspace.Services.GetLanguageServices(semanticModel.Language).GetRequiredService<ISyntaxFactsService>();

            var symbols = tokenInformation.Symbols;

            // if generating quick info for an attribute, prefer bind to the class instead of the constructor
            if (syntaxFactsService.IsAttributeName(token.Parent!))
            {
                symbols = symbols.OrderBy((s1, s2) =>
                    s1.Kind == s2.Kind ? 0 :
                    s1.Kind == SymbolKind.NamedType ? -1 :
                    s2.Kind == SymbolKind.NamedType ? 1 : 0).ToImmutableArray();
            }

            return QuickInfoUtilities.CreateQuickInfoItemAsync(
                workspace, semanticModel, token.Span, symbols, supportedPlatforms,
                tokenInformation.ShowAwaitReturn, tokenInformation.NullableFlowState, cancellationToken);
        }

        protected abstract bool GetBindableNodeForTokenIndicatingLambda(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);
        protected abstract bool GetBindableNodeForTokenIndicatingPossibleIndexerAccess(SyntaxToken token, [NotNullWhen(returnValue: true)] out SyntaxNode? found);

        protected virtual NullableFlowState GetNullabilityAnalysis(Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxNode node, CancellationToken cancellationToken) => NullableFlowState.None;

        private TokenInformation BindToken(
            Workspace workspace, SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        {
            var hostServices = workspace.Services;
            var languageServices = hostServices.GetLanguageServices(semanticModel.Language);
            var syntaxFacts = languageServices.GetRequiredService<ISyntaxFactsService>();
            var enclosingType = semanticModel.GetEnclosingNamedType(token.SpanStart, cancellationToken);

            var symbols = GetSymbolsFromToken(token, workspace, semanticModel, cancellationToken);

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
                    nullableFlowState = GetNullabilityAnalysis(workspace, semanticModel, firstSymbol, bindableParent, cancellationToken);
                }

                return new TokenInformation(symbols, isAwait, nullableFlowState);
            }

            // Couldn't bind the token to specific symbols.  If it's an operator, see if we can at
            // least bind it to a type.
            if (syntaxFacts.IsOperator(token))
            {
                var typeInfo = semanticModel.GetTypeInfo(token.Parent!, cancellationToken);
                if (IsOk(typeInfo.Type))
                {
                    return new TokenInformation(ImmutableArray.Create<ISymbol>(typeInfo.Type));
                }
            }

            return new TokenInformation(ImmutableArray<ISymbol>.Empty);
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
