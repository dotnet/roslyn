// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
