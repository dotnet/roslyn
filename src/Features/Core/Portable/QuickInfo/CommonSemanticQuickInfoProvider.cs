// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    internal abstract partial class CommonSemanticQuickInfoProvider : CommonQuickInfoProvider
    {
        protected override async Task<QuickInfoItem?> BuildQuickInfoAsync(
            CommonQuickInfoContext context,
            SyntaxToken token)
        {
            var (tokenInformation, supportedPlatforms) = ComputeQuickInfoData(context, token);

            if (tokenInformation.Symbols.IsDefaultOrEmpty)
            {
                return null;
            }

            return await CreateContentAsync(
                context, token, tokenInformation, supportedPlatforms).ConfigureAwait(false);
        }

        private (TokenInformation tokenInformation, SupportedPlatformData? supportedPlatforms) ComputeQuickInfoData(
            CommonQuickInfoContext context,
            SyntaxToken token)
        {
            if (!context.LinkedSemanticModels.IsEmpty)
                return ComputeFromLinkedDocuments(context, token);

            var tokenInformation = BindToken(context.Workspace, context.SemanticModel, token, context.CancellationToken);
            return (tokenInformation, supportedPlatforms: null);
        }

        private (TokenInformation, SupportedPlatformData supportedPlatforms) ComputeFromLinkedDocuments(
            CommonQuickInfoContext context,
            SyntaxToken token)
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

            var mainTokenInformation = BindToken(context.Workspace, context.SemanticModel, token, cancellationToken);

            var candidateProjects = new List<ProjectId> { context.DocumentId.ProjectId };
            var invalidProjects = new List<ProjectId>();

            var candidateResults = new List<(DocumentId docId, TokenInformation tokenInformation)>
            {
                (context.DocumentId, mainTokenInformation)
            };

            foreach (var (linkedDocumentId, linkedModel) in context.LinkedSemanticModels)
            {
                var linkedToken = FindTokenInLinkedDocument(token, linkedModel, cancellationToken);

                if (linkedToken != default)
                {
                    // Not in an inactive region, so this file is a candidate.
                    candidateProjects.Add(linkedDocumentId.ProjectId);
                    var linkedSymbols = BindToken(context.Workspace, linkedModel, linkedToken, cancellationToken);
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
            foreach (var candidate in candidateResults)
            {
                // Does the candidate have anything remotely equivalent?
                if (!candidate.tokenInformation.Symbols.Intersect(bestBinding.tokenInformation.Symbols, LinkedFilesSymbolEquivalenceComparer.Instance).Any())
                    invalidProjects.Add(candidate.docId.ProjectId);
            }

            var supportedPlatforms = new SupportedPlatformData(invalidProjects, candidateProjects, context.Workspace);
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
            CommonQuickInfoContext context,
            SyntaxToken token,
            TokenInformation tokenInformation,
            SupportedPlatformData? supportedPlatforms)
        {
            var workspace = context.Workspace;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
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
