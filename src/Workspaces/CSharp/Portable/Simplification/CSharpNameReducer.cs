// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpNameReducer : AbstractCSharpReducer
    {
        public override IExpressionRewriter CreateExpressionRewriter(OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Rewriter(optionSet, cancellationToken);
        }

        private static SyntaxNode SimplifyName(
            SyntaxNode node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            SyntaxNode replacementNode;
            TextSpan issueSpan;

            if (node.Kind() == SyntaxKind.QualifiedCref)
            {
                var crefSyntax = (QualifiedCrefSyntax)node;
                CrefSyntax crefReplacement;

                if (!crefSyntax.TryReduceOrSimplifyExplicitName(semanticModel, out crefReplacement, out issueSpan, optionSet, cancellationToken))
                {
                    return node;
                }

                replacementNode = crefReplacement;
            }
            else
            {
                var expressionSyntax = (ExpressionSyntax)node;
                ExpressionSyntax expressionReplacement;
                if (!expressionSyntax.TryReduceOrSimplifyExplicitName(semanticModel, out expressionReplacement, out issueSpan, optionSet, cancellationToken))
                {
                    return node;
                }

                replacementNode = expressionReplacement;
            }

            node = node.CopyAnnotationsTo(replacementNode).WithAdditionalAnnotations(Formatter.Annotation);
            return node.WithoutAnnotations(Simplifier.Annotation);
        }
    }
}
