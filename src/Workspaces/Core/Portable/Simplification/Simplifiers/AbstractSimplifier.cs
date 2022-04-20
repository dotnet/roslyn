// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Simplification.Simplifiers
{
    internal abstract class AbstractSimplifier<TSyntax, TSimplifiedSyntax, TSimplifierOptions>
        where TSyntax : SyntaxNode
        where TSimplifiedSyntax : SyntaxNode
        where TSimplifierOptions : SimplifierOptions
    {
        protected readonly SemanticModel SemanticModel;

        protected AbstractSimplifier(SemanticModel semanticModel)
        {
            SemanticModel = semanticModel;
        }

        public abstract bool TrySimplify(
            TSyntax syntax,
            TSimplifierOptions options,
            [NotNullWhen(true)] out TSimplifiedSyntax? replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken);
    }
}
