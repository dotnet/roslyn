// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal abstract partial class AnalyzerDriver
    {
        internal readonly struct DeclarationAnalysisData
        {
            /// <summary>
            /// GetSyntax() for the given SyntaxReference.
            /// </summary>
            public readonly SyntaxNode DeclaringReferenceSyntax;

            /// <summary>
            /// Topmost declaration node for analysis.
            /// </summary>
            public readonly SyntaxNode TopmostNodeForAnalysis;

            /// <summary>
            /// All member declarations within the declaration.
            /// </summary>
            public readonly ImmutableArray<DeclarationInfo> DeclarationsInNode;

            /// <summary>
            /// All descendant nodes for syntax node actions.
            /// </summary>
            public readonly ArrayBuilder<SyntaxNode> DescendantNodesToAnalyze = ArrayBuilder<SyntaxNode>.GetInstance();

            /// <summary>
            /// Flag indicating if this is a partial analysis.
            /// </summary>
            public readonly bool IsPartialAnalysis;

            public DeclarationAnalysisData(
                SyntaxNode declaringReferenceSyntax,
                SyntaxNode topmostNodeForAnalysis,
                ImmutableArray<DeclarationInfo> declarationsInNodeBuilder,
                bool isPartialAnalysis)
            {
                DeclaringReferenceSyntax = declaringReferenceSyntax;
                TopmostNodeForAnalysis = topmostNodeForAnalysis;
                DeclarationsInNode = declarationsInNodeBuilder;
                IsPartialAnalysis = isPartialAnalysis;
            }

            public void Free()
            {
                DescendantNodesToAnalyze.Free();
            }
        }
    }
}
