﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Simplification.Simplifiers
{
    internal abstract class AbstractSimplifier<TSyntax, TSimplifiedSyntax>
        where TSyntax : SyntaxNode
        where TSimplifiedSyntax : SyntaxNode
    {
        public abstract bool TrySimplify(
            TSyntax syntax,
            SemanticModel semanticModel,
            OptionSet optionSet,
            out TSimplifiedSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken);
    }
}
