// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpExtensionMethodReducer
    {
        private class Rewriter : AbstractExpressionRewriter
        {
            public Rewriter(OptionSet optionSet, CancellationToken cancellationToken)
                : base(optionSet, cancellationToken)
            {
            }

            public override SyntaxNode VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                return SimplifyExpression(
                    node,
                    newNode: base.VisitInvocationExpression(node),
                    simplifier: SimplifyExtensionMethod);
            }
        }
    }
}
