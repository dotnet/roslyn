// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class LabelSymbolReferenceFinder : AbstractMemberScopedReferenceFinder<ILabelSymbol>
    {
        protected override Func<SyntaxToken, bool> GetTokensMatchFunction(ISyntaxFactsService syntaxFacts, string name)
        {
            // Labels in VB can actually be numeric literals.  Wacky.
            return t => IdentifiersMatch(syntaxFacts, name, t) || syntaxFacts.IsLiteral(t);
        }
    }
}
