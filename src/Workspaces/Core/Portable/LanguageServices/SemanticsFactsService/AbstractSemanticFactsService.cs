// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal abstract class AbstractSemanticFactsService
    {
        protected abstract ISyntaxFactsService SyntaxFactsService { get; }

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
            // local name can be same as field or property. but that will hide
            // those and can cause semantic change later in some context.
            // so to be safe, we consider field and property in scope when
            // creating unique name for local
            Func<ISymbol, bool> filter = s =>
                s.Kind == SymbolKind.Local ||
                s.Kind == SymbolKind.Parameter ||
                s.Kind == SymbolKind.RangeVariable ||
                s.Kind == SymbolKind.Field ||
                s.Kind == SymbolKind.Property;

            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, filter, usedNames: Enumerable.Empty<string>(), cancellationToken);
        }

        public SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel,
            SyntaxNode location, SyntaxNode containerOpt,
            string baseName, Func<ISymbol, bool> filter,
            IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            var syntaxFacts = this.SyntaxFactsService;

            var container = containerOpt ?? location.AncestorsAndSelf().FirstOrDefault(
                a => syntaxFacts.IsExecutableBlock(a) || syntaxFacts.IsMethodBody(a));

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

        private SyntaxToken GenerateUniqueName(string baseName, IEnumerable<string> usedNames)
        {
            return this.SyntaxFactsService.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(
                    baseName, usedNames, this.SyntaxFactsService.IsCaseSensitive));
        }
    }
}
