// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        internal sealed class DeclarationAnalysisDataBuilder
        {
            public DeclarationAnalysisDataBuilder()
            {
                this.DeclarationsInNode = ImmutableArray.CreateBuilder<DeclarationInfo>();
                this.DescendantNodesToAnalyze = ImmutableArray.CreateBuilder<SyntaxNode>();
            }

            /// <summary>
            /// GetSyntax() for the given SyntaxReference.
            /// </summary>
            public SyntaxNode DeclaringReferenceSyntax { get; set; }

            /// <summary>
            /// Topmost declaration node for analysis.
            /// </summary>
            public SyntaxNode TopmostNodeForAnalysis { get; set; }

            /// <summary>
            /// All member declarations within the declaration.
            /// </summary>
            public ImmutableArray<DeclarationInfo>.Builder DeclarationsInNode { get; }

            /// <summary>
            /// All descendant nodes for syntax node actions.
            /// </summary>
            public ImmutableArray<SyntaxNode>.Builder DescendantNodesToAnalyze { get; }

            /// <summary>
            /// Flag indicating if this is a partial analysis.
            /// </summary>
            public bool IsPartialAnalysis { get; set; }

            public void Free()
            {
                DeclaringReferenceSyntax = null;
                TopmostNodeForAnalysis = null;
                DeclarationsInNode.Clear();
                DescendantNodesToAnalyze.Clear();
                IsPartialAnalysis = false;
            }
        }
    }
}
