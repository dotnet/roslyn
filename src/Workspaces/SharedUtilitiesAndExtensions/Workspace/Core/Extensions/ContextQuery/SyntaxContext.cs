// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery
{
    internal abstract class SyntaxContext
    {
        private ISet<INamedTypeSymbol> _outerTypes;

        protected SyntaxContext(
            Workspace workspace,
            SemanticModel semanticModel,
            int position,
            SyntaxToken leftToken,
            SyntaxToken targetToken,
            bool isTypeContext,
            bool isNamespaceContext,
            bool isNamespaceDeclarationNameContext,
            bool isPreProcessorDirectiveContext,
            bool isRightOfNameSeparator,
            bool isStatementContext,
            bool isAnyExpressionContext,
            bool isAttributeNameContext,
            bool isEnumTypeMemberAccessContext,
            bool isNameOfContext,
            bool isInQuery,
            bool isInImportsDirective,
            bool isWithinAsyncMethod,
            bool isPossibleTupleContext,
            bool isAtStartOfPattern,
            bool isAtEndOfPattern,
            bool isRightSideOfNumericType,
            bool isOnArgumentListBracketOrComma,
            CancellationToken cancellationToken)
        {
            this.Workspace = workspace;
            this.SemanticModel = semanticModel;
            this.SyntaxTree = semanticModel.SyntaxTree;
            this.Position = position;
            this.LeftToken = leftToken;
            this.TargetToken = targetToken;
            this.IsTypeContext = isTypeContext;
            this.IsNamespaceContext = isNamespaceContext;
            this.IsNamespaceDeclarationNameContext = isNamespaceDeclarationNameContext;
            this.IsPreProcessorDirectiveContext = isPreProcessorDirectiveContext;
            this.IsRightOfNameSeparator = isRightOfNameSeparator;
            this.IsStatementContext = isStatementContext;
            this.IsAnyExpressionContext = isAnyExpressionContext;
            this.IsAttributeNameContext = isAttributeNameContext;
            this.IsEnumTypeMemberAccessContext = isEnumTypeMemberAccessContext;
            this.IsNameOfContext = isNameOfContext;
            this.IsInQuery = isInQuery;
            this.IsInImportsDirective = isInImportsDirective;
            this.IsWithinAsyncMethod = isWithinAsyncMethod;
            this.IsPossibleTupleContext = isPossibleTupleContext;
            this.IsAtStartOfPattern = isAtStartOfPattern;
            this.IsAtEndOfPattern = isAtEndOfPattern;
            this.InferredTypes = ComputeInferredTypes(workspace, semanticModel, position, cancellationToken);
            this.IsRightSideOfNumericType = isRightSideOfNumericType;
            this.IsOnArgumentListBracketOrComma = isOnArgumentListBracketOrComma;
        }

        public Workspace Workspace { get; }
        public SemanticModel SemanticModel { get; }
        public SyntaxTree SyntaxTree { get; }
        public int Position { get; }

        /// <summary>
        /// The token to the left of <see cref="Position"/>. This token may be touching the position.
        /// </summary>
        public SyntaxToken LeftToken { get; }

        /// <summary>
        /// The first token to the left of <see cref="Position"/> that we're not touching. Equal to <see cref="LeftToken"/>
        /// if we aren't touching <see cref="LeftToken" />.
        /// </summary>
        public SyntaxToken TargetToken { get; }

        public bool IsTypeContext { get; }
        public bool IsNamespaceContext { get; }

        public bool IsNamespaceDeclarationNameContext { get; }

        public bool IsPreProcessorDirectiveContext { get; }

        public bool IsRightOfNameSeparator { get; }
        public bool IsStatementContext { get; }
        public bool IsAnyExpressionContext { get; }
        public bool IsAttributeNameContext { get; }
        public bool IsEnumTypeMemberAccessContext { get; }
        public bool IsNameOfContext { get; }

        public bool IsInQuery { get; }
        public bool IsInImportsDirective { get; }
        public bool IsWithinAsyncMethod { get; }
        public bool IsPossibleTupleContext { get; }
        public bool IsAtStartOfPattern { get; }
        public bool IsAtEndOfPattern { get; }

        public bool IsRightSideOfNumericType { get; }
        public bool IsOnArgumentListBracketOrComma { get; }

        public ImmutableArray<ITypeSymbol> InferredTypes { get; }

        private ISet<INamedTypeSymbol> ComputeOuterTypes(CancellationToken cancellationToken)
        {
            var enclosingSymbol = this.SemanticModel.GetEnclosingSymbol(this.LeftToken.SpanStart, cancellationToken);
            if (enclosingSymbol != null)
            {
                var containingType = enclosingSymbol.GetContainingTypeOrThis();
                if (containingType != null)
                {
                    return containingType.GetContainingTypes().ToSet();
                }
            }

            return SpecializedCollections.EmptySet<INamedTypeSymbol>();
        }

        protected ImmutableArray<ITypeSymbol> ComputeInferredTypes(Workspace workspace,
            SemanticModel semanticModel,
            int position,
            CancellationToken cancellationToken)
        {
            var typeInferenceService = workspace?.Services.GetLanguageService<ITypeInferenceService>(semanticModel.Language)
                ?? GetTypeInferenceServiceWithoutWorkspace();
            return typeInferenceService.InferTypes(semanticModel, position, cancellationToken);
        }

        internal abstract ITypeInferenceService GetTypeInferenceServiceWithoutWorkspace();

        public ISet<INamedTypeSymbol> GetOuterTypes(CancellationToken cancellationToken)
        {
            if (_outerTypes == null)
            {
                Interlocked.CompareExchange(ref _outerTypes, ComputeOuterTypes(cancellationToken), null);
            }

            return _outerTypes;
        }

        public TService GetLanguageService<TService>() where TService : class, ILanguageService
            => this.Workspace.Services.GetLanguageService<TService>(this.SemanticModel.Language);

        public TService GetWorkspaceService<TService>() where TService : class, IWorkspaceService
            => this.Workspace.Services.GetService<TService>();
    }
}
