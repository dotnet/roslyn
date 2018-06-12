// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery
{
    internal struct ForEachInfo<TForEachStatement, TStatement>
    {
        public TForEachStatement ForEachStatement { get; }

        public ImmutableArray<ExtendedSyntaxNode> ConvertingExtendedNodes { get; }

        public ImmutableArray<SyntaxToken> Identifiers { get; }

        public ImmutableArray<TStatement> Statements { get; }

        public ImmutableArray<SyntaxToken> LeadingTokens { get; }

        public ImmutableArray<SyntaxToken> TrailingTokens { get; }

        public ForEachInfo(
            TForEachStatement forEachStatement,
            IEnumerable<ExtendedSyntaxNode> convertingExtendedNodes,
            IEnumerable<SyntaxToken> identifiers,
            IEnumerable<TStatement> statements,
            IEnumerable<SyntaxToken> leadingTokens,
            IEnumerable<SyntaxToken> trailingTokens)
        {
            ForEachStatement = forEachStatement;
            ConvertingExtendedNodes = convertingExtendedNodes.ToImmutableArray();
            Identifiers = identifiers.ToImmutableArray();
            Statements = statements.ToImmutableArray();
            LeadingTokens = leadingTokens.ToImmutableArray();
            TrailingTokens = trailingTokens.ToImmutableArray();
        }
    }
}
