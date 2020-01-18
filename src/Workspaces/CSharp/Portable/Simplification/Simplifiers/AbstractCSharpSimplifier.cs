// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers
{
    internal abstract class AbstractCSharpSimplifier<TSyntax, TSimplifiedSyntax>
        where TSyntax : SyntaxNode
        where TSimplifiedSyntax : SyntaxNode
    {
        public abstract bool TrySimplify(
            TSyntax crefSyntax,
            SemanticModel semanticModel,
            OptionSet optionSet,
            out TSimplifiedSyntax replacementNode,
            out TextSpan issueSpan,
            CancellationToken cancellationToken);
    }
}
