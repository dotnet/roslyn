// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

        private SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel,
            SyntaxNode location, SyntaxNode containerOpt,
            string baseName, Func<ISymbol, bool> filter,
            IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            var syntaxFacts = this.SyntaxFactsService;

            var container = containerOpt ?? location.AncestorsAndSelf().FirstOrDefault(
                a => syntaxFacts.IsExecutableBlock(a) || syntaxFacts.IsMethodBody(a));

            var candidates = semanticModel.LookupSymbols(location.SpanStart).Concat(
                semanticModel.GetExistingSymbols(container, cancellationToken));

            return GenerateUniqueName(
                semanticModel, location, containerOpt, baseName, filter != null ? candidates.Where(filter) : candidates, usedNames, cancellationToken);
        }

        private SyntaxToken GenerateUniqueName(
            SemanticModel semanticModel, SyntaxNode location, SyntaxNode containerOpt,
            string baseName, IEnumerable<ISymbol> candidates, IEnumerable<string> usedNames, CancellationToken cancellationToken)
        {
            return this.SyntaxFactsService.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(
                    baseName, candidates.Select(s => s.Name).Concat(usedNames), this.SyntaxFactsService.IsCaseSensitive));
        }
    }
}
