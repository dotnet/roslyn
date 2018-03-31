// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            var syntaxFacts = this.SyntaxFactsService;

            var container = containerOpt ?? location.AncestorsAndSelf().FirstOrDefault(
                a => syntaxFacts.IsExecutableBlock(a) || syntaxFacts.IsMethodBody(a));
            var existingSymbols = semanticModel.GetExistingSymbols(container, cancellationToken);

            var reservedNames = semanticModel.LookupSymbols(location.SpanStart)
                                             .Select(s => s.Name)
                                             .Concat(existingSymbols.Select(s => s.Name));

            return syntaxFacts.ToIdentifierToken(
                NameGenerator.EnsureUniqueness(baseName, reservedNames, syntaxFacts.IsCaseSensitive));
        }
    }
}
