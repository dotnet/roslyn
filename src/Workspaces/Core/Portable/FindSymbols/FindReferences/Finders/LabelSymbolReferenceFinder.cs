// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal sealed class LabelSymbolReferenceFinder : AbstractMemberScopedReferenceFinder<ILabelSymbol>
    {
        protected override bool TokensMatch(FindReferencesDocumentState state, SyntaxToken token, string name)
        {
            // Labels in VB can actually be numeric literals.  Wacky.
            return IdentifiersMatch(state.SyntaxFacts, name, token) || state.SyntaxFacts.IsLiteral(token);
        }
    }
}
