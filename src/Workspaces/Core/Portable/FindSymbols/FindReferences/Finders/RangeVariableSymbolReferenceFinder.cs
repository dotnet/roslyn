// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class RangeVariableSymbolReferenceFinder : AbstractMemberScopedReferenceFinder<IRangeVariableSymbol>
    {
        protected override Func<SyntaxToken, bool> GetTokensMatchFunction(ISyntaxFactsService syntaxFacts, string name)
        {
            return t => IdentifiersMatch(syntaxFacts, name, t);
        }
    }
}
