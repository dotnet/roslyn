﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServices
{
    internal interface ISyntaxFactsService : ISyntaxFacts, ILanguageService
    {
        bool IsInInactiveRegion(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool IsInNonUserCode(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        bool IsPossibleTupleContext(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        Task<ImmutableArray<SyntaxNode>> GetSelectedFieldsAndPropertiesAsync(SyntaxTree syntaxTree, TextSpan textSpan, bool allowPartialSelection, CancellationToken cancellationToken);

        // Walks the tree, starting from contextNode, looking for the first construct
        // with a missing close brace.  If found, the close brace will be added and the
        // updates root will be returned.  The context node in that new tree will also
        // be returned.
        // TODO: This method should be moved out of ISyntaxFactsService.
        void AddFirstMissingCloseBrace<TContextNode>(
            SyntaxNode root, TContextNode contextNode,
            out SyntaxNode newRoot, out TContextNode newContextNode) where TContextNode : SyntaxNode;
    }
}
