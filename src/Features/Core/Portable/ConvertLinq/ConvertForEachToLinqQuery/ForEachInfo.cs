// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.ConvertLinq.ConvertForEachToLinqQuery
{
    internal readonly struct ForEachInfo<TForEachStatement, TStatement>
    {
        public TForEachStatement ForEachStatement { get; }

        public SemanticModel SemanticModel { get; }

        public ImmutableArray<ExtendedSyntaxNode> ConvertingExtendedNodes { get; }

        public ImmutableArray<SyntaxToken> Identifiers { get; }

        public ImmutableArray<TStatement> Statements { get; }

        public ImmutableArray<SyntaxToken> LeadingTokens { get; }

        public ImmutableArray<SyntaxToken> TrailingTokens { get; }

        public ForEachInfo(
            TForEachStatement forEachStatement,
            SemanticModel semanticModel,
            ImmutableArray<ExtendedSyntaxNode> convertingExtendedNodes,
            ImmutableArray<SyntaxToken> identifiers,
            ImmutableArray<TStatement> statements,
            ImmutableArray<SyntaxToken> leadingTokens,
            ImmutableArray<SyntaxToken> trailingTokens)
        {
            ForEachStatement = forEachStatement;
            SemanticModel = semanticModel;
            ConvertingExtendedNodes = convertingExtendedNodes;
            Identifiers = identifiers;
            Statements = statements;
            LeadingTokens = leadingTokens;
            TrailingTokens = trailingTokens;
        }
    }
}
