// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpInferredMemberNameReducer : AbstractCSharpReducer
    {
        public override IExpressionRewriter CreateExpressionRewriter(OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Rewriter(optionSet, cancellationToken);
        }

        private static ArgumentSyntax SimplifyTupleName(ArgumentSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            string inferredName = node.IsParentKind(SyntaxKind.TupleExpression) ?
                CSharpSimplificationService.ExtractAnonymousTypeMemberName(node.Expression).ValueText :
                null;

            if (inferredName == null || inferredName != node?.NameColon?.Name.Identifier.ValueText)
            {
                return node;
            }

            return node.WithNameColon(null);
        }


        private static SyntaxNode SimplifyAnonymousTypeMemberName(AnonymousObjectMemberDeclaratorSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken canellationToken)
        {
            string inferredName = CSharpSimplificationService.ExtractAnonymousTypeMemberName(node.Expression).ValueText;

            if (inferredName == null || inferredName != node?.NameEquals?.Name.Identifier.ValueText)
            {
                return node;
            }

            return node.WithNameEquals(null);
        }
    }
}
