// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpDeclarationPatternReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
                _simplifyNode = SimplifyNode;
            }

            private readonly Func<DeclarationPatternSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> _simplifyNode;

            private SyntaxNode SimplifyNode(
                DeclarationPatternSyntax node,
                SemanticModel semanticModel,
                OptionSet optionSet,
                CancellationToken cancellationToken)
            {
                if (ParseOptions.LanguageVersion >= LanguageVersion.CSharp9 &&
                    node.Designation.IsMissing)
                {
                    return SyntaxFactory.TypePattern(node.Type).WithTriviaFrom(node);
                }

                return node;
            }

            public override SyntaxNode VisitDeclarationPattern(DeclarationPatternSyntax node)
            {
                return SimplifyNode(
                    node,
                    newNode: base.VisitDeclarationPattern(node),
                    parentNode: node.Parent,
                    simplifier: _simplifyNode);
            }
        }
    }
}
