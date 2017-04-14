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
            if (node.NameColon == null || !node.IsParentKind(SyntaxKind.TupleExpression))
            {
                return node;
            }

            var inferredName = node.Expression.TryGetInferredMemberName();

            if (inferredName == null || inferredName != node.NameColon.Name.Identifier.ValueText)
            {
                return node;
            }

            return node.WithNameColon(null).WithTriviaFrom(node);
        }


        private static SyntaxNode SimplifyAnonymousTypeMemberName(AnonymousObjectMemberDeclaratorSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken canellationToken)
        {
            if (node.NameEquals == null)
            {
                return node;
            }

            var inferredName = node.Expression.TryGetInferredMemberName();

            if (inferredName == null || inferredName != node.NameEquals.Name.Identifier.ValueText)
            {
                return node;
            }

            return node.WithNameEquals(null).WithTriviaFrom(node);
        }
    }
}
