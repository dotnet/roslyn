// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpInferredMemberNameReducer
    {
        private class Rewriter : AbstractExpressionRewriter
        {
            public Rewriter(OptionSet optionSet, CancellationToken cancellationToken)
                : base(optionSet, cancellationToken)
            {
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
