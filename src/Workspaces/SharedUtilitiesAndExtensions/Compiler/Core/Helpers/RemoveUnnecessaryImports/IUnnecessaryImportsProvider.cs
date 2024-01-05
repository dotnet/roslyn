// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal interface IUnnecessaryImportsProvider<TSyntaxNode>
        where TSyntaxNode : SyntaxNode
    {
        ImmutableArray<TSyntaxNode> GetUnnecessaryImports(SemanticModel model, TextSpan? span, CancellationToken cancellationToken);

        ImmutableArray<TSyntaxNode> GetUnnecessaryImports(
            SemanticModel model,
            TextSpan? span,
            Func<SyntaxNode, bool>? predicate,
            CancellationToken cancellationToken);
    }
}
