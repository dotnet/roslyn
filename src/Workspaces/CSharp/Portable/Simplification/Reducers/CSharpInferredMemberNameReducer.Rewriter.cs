// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpInferredMemberNameReducer
    {
        private class Rewriter : AbstractReductionRewriter
        {
            public Rewriter(ObjectPool<IReductionRewriter> pool)
                : base(pool)
            {
                s_simplifyTupleName = SimplifyTupleName;
            }

            private readonly Func<ArgumentSyntax, SemanticModel, OptionSet, CancellationToken, ArgumentSyntax> s_simplifyTupleName;

            private ArgumentSyntax SimplifyTupleName(ArgumentSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
            {
                if (CanSimplifyTupleElementName(node, this.ParseOptions))
                {
                    return node.WithNameColon(null).WithTriviaFrom(node);
                }

                return node;
            }

            private static readonly Func<AnonymousObjectMemberDeclaratorSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyAnonymousTypeMemberName = SimplifyAnonymousTypeMemberName;

            private static SyntaxNode SimplifyAnonymousTypeMemberName(AnonymousObjectMemberDeclaratorSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken canellationToken)
            {

                if (CanSimplifyAnonymousTypeMemberName(node))
                {
                    return node.WithNameEquals(null).WithTriviaFrom(node);
                }

                return node;
            }

            public override SyntaxNode VisitArgument(ArgumentSyntax node)
            {
                var newNode = base.VisitArgument(node);

                if (node.Parent.IsKind(SyntaxKind.TupleExpression))
                {
                    return SimplifyNode(
                        node,
                        parentNode: node.Parent,
                        newNode: newNode,
                        simplifier: s_simplifyTupleName);
                }

                return newNode;
            }

            public override SyntaxNode VisitAnonymousObjectMemberDeclarator(AnonymousObjectMemberDeclaratorSyntax node)
            {
                return SimplifyNode(
                    node,
                    parentNode: node.Parent,
                    newNode: base.VisitAnonymousObjectMemberDeclarator(node),
                    simplifier: s_simplifyAnonymousTypeMemberName);
            }
        }
    }
}
