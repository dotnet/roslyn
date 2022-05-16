// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract partial class AbstractSemanticFactsService : ISemanticFacts
    {
        public abstract ISyntaxFacts SyntaxFacts { get; }
        public abstract IBlockFacts BlockFacts { get; }

        protected abstract ISemanticFacts SemanticFacts { get; }

        protected abstract SyntaxToken ToIdentifierToken(string identifier);

        // local name can be same as field or property. but that will hide
        // those and can cause semantic change later in some context.
        // so to be safe, we consider field and property in scope when
        // creating unique name for local
        private static readonly Func<ISymbol, bool> s_LocalNameFilter = s =>
            s.Kind == SymbolKind.Local ||
            s.Kind == SymbolKind.Parameter ||
            s.Kind == SymbolKind.RangeVariable ||
            s.Kind == SymbolKind.Field ||
            s.Kind == SymbolKind.Property ||
            (s.Kind == SymbolKind.NamedType && s.IsStatic);

        public SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
            string baseName, CancellationToken cancellationToken)
        {
            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, filter: null, usedNames: null, cancellationToken);
        }

        public SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
            string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, filter: null, usedNames, cancellationToken);
        }

        public SyntaxToken GenerateUniqueLocalName(
            SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
            string baseName, CancellationToken cancellationToken)
        {
            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, s_LocalNameFilter, usedNames: Enumerable.Empty<string>(), cancellationToken);
        }

        public SyntaxToken GenerateUniqueLocalName(
            SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
            string baseName, IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, s_LocalNameFilter, usedNames: usedNames, cancellationToken);
        }

        public SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel,
            SyntaxNode location, SyntaxNode containerOpt,
            string baseName, Func<ISymbol, bool> filter,
            IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            var container = containerOpt ?? location.AncestorsAndSelf().FirstOrDefault(
                a => BlockFacts.IsExecutableBlock(a) || SyntaxFacts.IsParameterList(a) || SyntaxFacts.IsMethodBody(a));

            var candidates = GetCollidableSymbols(semanticModel, location, container, cancellationToken);
            var filteredCandidates = filter != null ? candidates.Where(filter) : candidates;

            return GenerateUniqueName(baseName, filteredCandidates.Select(s => s.Name).Concat(usedNames));
        }

        /// <summary>
        /// Retrieves all symbols that could collide with a symbol at the specified location.
        /// A symbol can possibly collide with the location if it is available to that location and/or
        /// could cause a compiler error if its name is re-used at that location.
        /// </summary>
        protected virtual IEnumerable<ISymbol> GetCollidableSymbols(SemanticModel semanticModel, SyntaxNode location, SyntaxNode container, CancellationToken cancellationToken)
            => semanticModel.LookupSymbols(location.SpanStart).Concat(semanticModel.GetExistingSymbols(container, cancellationToken));

        public SyntaxToken GenerateUniqueName(string baseName, IEnumerable<string> usedNames)
        {
            return this.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(
                    baseName, usedNames, this.SyntaxFacts.IsCaseSensitive));
        }

        #region ISemanticFacts implementation

        public bool SupportsImplicitInterfaceImplementation => SemanticFacts.SupportsImplicitInterfaceImplementation;

        public bool SupportsParameterizedProperties => SemanticFacts.SupportsParameterizedProperties;

        public bool ExposesAnonymousFunctionParameterNames => SemanticFacts.ExposesAnonymousFunctionParameterNames;

        public bool IsWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsWrittenTo(semanticModel, node, cancellationToken);

        public bool IsOnlyWrittenTo(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsOnlyWrittenTo(semanticModel, node, cancellationToken);

        public bool IsInOutContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsInOutContext(semanticModel, node, cancellationToken);

        public bool IsInRefContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsInRefContext(semanticModel, node, cancellationToken);

        public bool IsInInContext(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsInInContext(semanticModel, node, cancellationToken);

        public bool CanReplaceWithRValue(SemanticModel semanticModel, SyntaxNode expression, CancellationToken cancellationToken)
            => SemanticFacts.CanReplaceWithRValue(semanticModel, expression, cancellationToken);

        public ISymbol GetDeclaredSymbol(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
            => SemanticFacts.GetDeclaredSymbol(semanticModel, token, cancellationToken);

        public bool LastEnumValueHasInitializer(INamedTypeSymbol namedTypeSymbol)
            => SemanticFacts.LastEnumValueHasInitializer(namedTypeSymbol);

        public bool TryGetSpeculativeSemanticModel(SemanticModel oldSemanticModel, SyntaxNode oldNode, SyntaxNode newNode, out SemanticModel speculativeModel)
            => SemanticFacts.TryGetSpeculativeSemanticModel(oldSemanticModel, oldNode, newNode, out speculativeModel);

        public ImmutableHashSet<string> GetAliasNameSet(SemanticModel model, CancellationToken cancellationToken)
            => SemanticFacts.GetAliasNameSet(model, cancellationToken);

        public ForEachSymbols GetForEachSymbols(SemanticModel semanticModel, SyntaxNode forEachStatement)
            => SemanticFacts.GetForEachSymbols(semanticModel, forEachStatement);

        public IMethodSymbol GetGetAwaiterMethod(SemanticModel semanticModel, SyntaxNode node)
            => SemanticFacts.GetGetAwaiterMethod(semanticModel, node);

        public ImmutableArray<IMethodSymbol> GetDeconstructionAssignmentMethods(SemanticModel semanticModel, SyntaxNode node)
            => SemanticFacts.GetDeconstructionAssignmentMethods(semanticModel, node);

        public ImmutableArray<IMethodSymbol> GetDeconstructionForEachMethods(SemanticModel semanticModel, SyntaxNode node)
            => SemanticFacts.GetDeconstructionForEachMethods(semanticModel, node);

        public bool IsPartial(ITypeSymbol typeSymbol, CancellationToken cancellationToken)
            => SemanticFacts.IsPartial(typeSymbol, cancellationToken);

        public bool IsNullChecked(IParameterSymbol parameterSymbol, CancellationToken cancellationToken)
            => SemanticFacts.IsNullChecked(parameterSymbol, cancellationToken);

        public IEnumerable<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode memberDeclaration, CancellationToken cancellationToken)
            => SemanticFacts.GetDeclaredSymbols(semanticModel, memberDeclaration, cancellationToken);

        public IParameterSymbol FindParameterForArgument(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
            => SemanticFacts.FindParameterForArgument(semanticModel, argumentNode, cancellationToken);

        public IParameterSymbol FindParameterForAttributeArgument(SemanticModel semanticModel, SyntaxNode argumentNode, CancellationToken cancellationToken)
            => SemanticFacts.FindParameterForAttributeArgument(semanticModel, argumentNode, cancellationToken);

        public ImmutableArray<ISymbol> GetBestOrAllSymbols(SemanticModel semanticModel, SyntaxNode node, SyntaxToken token, CancellationToken cancellationToken)
            => SemanticFacts.GetBestOrAllSymbols(semanticModel, node, token, cancellationToken);

        public bool IsInsideNameOfExpression(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
            => SemanticFacts.IsInsideNameOfExpression(semanticModel, node, cancellationToken);

        public ImmutableArray<IMethodSymbol> GetLocalFunctionSymbols(Compilation compilation, ISymbol symbol, CancellationToken cancellationToken)
            => SemanticFacts.GetLocalFunctionSymbols(compilation, symbol, cancellationToken);

        public bool IsInExpressionTree(SemanticModel semanticModel, SyntaxNode node, INamedTypeSymbol expressionTypeOpt, CancellationToken cancellationToken)
            => SemanticFacts.IsInExpressionTree(semanticModel, node, expressionTypeOpt, cancellationToken);

        #endregion
    }
}
