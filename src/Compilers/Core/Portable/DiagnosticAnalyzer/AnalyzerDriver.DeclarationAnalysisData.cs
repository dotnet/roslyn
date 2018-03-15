// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        internal sealed class DeclarationAnalysisData
        {
            public static DeclarationAnalysisData CreateFrom(DeclarationAnalysisDataBuilder builder) => new DeclarationAnalysisData(builder);
            private DeclarationAnalysisData(DeclarationAnalysisDataBuilder builder)
            {
                DeclaringReferenceSyntax = builder.DeclaringReferenceSyntax;
                TopmostNodeForAnalysis = builder.TopmostNodeForAnalysis;
                DeclarationsInNode = builder.DeclarationsInNode.ToImmutable();
                DescendantNodesToAnalyze = builder.DescendantNodesToAnalyze.ToImmutable();
                IsPartialAnalysis = builder.IsPartialAnalysis;
            }

            /// <summary>
            /// GetSyntax() for the given SyntaxReference.
            /// </summary>
            public SyntaxNode DeclaringReferenceSyntax { get; }

            /// <summary>
            /// Topmost declaration node for analysis.
            /// </summary>
            public SyntaxNode TopmostNodeForAnalysis { get; }

            /// <summary>
            /// All member declarations within the declaration.
            /// </summary>
            public ImmutableArray<DeclarationInfo> DeclarationsInNode { get; }

            /// <summary>
            /// All descendant nodes for syntax node actions.
            /// </summary>
            public ImmutableArray<SyntaxNode> DescendantNodesToAnalyze { get; }

            /// <summary>
            /// Flag indicating if this is a partial analysis.
            /// </summary>
            public bool IsPartialAnalysis { get; }
        }
    }
}
