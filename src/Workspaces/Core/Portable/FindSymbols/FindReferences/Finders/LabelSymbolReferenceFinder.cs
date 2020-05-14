// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
