// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery
{
    internal readonly struct ExtendedSyntaxNode
    {
        public SyntaxNode Node { get; }

        public ImmutableArray<SyntaxTrivia> ExtraLeadingComments { get; }

        public ImmutableArray<SyntaxTrivia> ExtraTrailingComments { get; }

        public ExtendedSyntaxNode(
            SyntaxNode node,
            IEnumerable<SyntaxToken> extraLeadingTokens,
            IEnumerable<SyntaxToken> extraTrailingTokens)
         : this(node, extraLeadingTokens.GetTrivia(), extraTrailingTokens.GetTrivia())
        {
        }

        public ExtendedSyntaxNode(
            SyntaxNode node,
            IEnumerable<SyntaxTrivia> extraLeadingComments,
            IEnumerable<SyntaxTrivia> extraTrailingComments)
        {
            Node = node;
            ExtraLeadingComments = extraLeadingComments.ToImmutableArray();
            ExtraTrailingComments = extraTrailingComments.ToImmutableArray();
        }
    }
}
