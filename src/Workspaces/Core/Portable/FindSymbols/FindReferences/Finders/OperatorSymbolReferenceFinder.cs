// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class OperatorSymbolReferenceFinder : AbstractReferenceFinder<IMethodSymbol>
    {
        protected override bool CanFind(IMethodSymbol symbol)
        {
            return symbol.MethodKind == MethodKind.UserDefinedOperator;
        }

        protected override Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            IMethodSymbol symbol,
            Project project,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken)
        {
            var op = symbol.GetPredefinedOperator();
            return FindDocumentsAsync(project, documents, op, cancellationToken);
        }

        protected override Task<ImmutableArray<ReferenceLocation>> FindReferencesInDocumentAsync(
            IMethodSymbol symbol,
            Document document,
            CancellationToken cancellationToken)
        {
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var op = symbol.GetPredefinedOperator();

            return FindReferencesInDocumentAsync(symbol, document, t =>
                IsPotentialReference(syntaxFacts, op, t),
                cancellationToken);
        }

        private bool IsPotentialReference(
            ISyntaxFactsService syntaxFacts,
            PredefinedOperator op,
            SyntaxToken token)
        {
            PredefinedOperator actualOperator;
            return syntaxFacts.TryGetPredefinedOperator(token, out actualOperator) && actualOperator == op;
        }
    }
}
