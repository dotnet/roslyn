// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISyntaxFactsService : ISyntaxFacts, ILanguageService
    {
        SyntaxNode Parenthesize(SyntaxNode expression, bool includeElasticTrivia = true, bool addSimplifierAnnotation = true);

        SyntaxToken ToIdentifierToken(string name);

        // Walks the tree, starting from contextNode, looking for the first construct
        // with a missing close brace.  If found, the close brace will be added and the
        // updates root will be returned.  The context node in that new tree will also
        // be returned.
        void AddFirstMissingCloseBrace<TContextNode>(
            SyntaxNode root, TContextNode contextNode,
            out SyntaxNode newRoot, out TContextNode newContextNode) where TContextNode : SyntaxNode;
    }
}
