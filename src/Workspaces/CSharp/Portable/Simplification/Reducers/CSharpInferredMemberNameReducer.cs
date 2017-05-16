// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    /// <summary>
    /// Complexify makes inferred names explicit for tuple elements and anonymous type members. This
    /// class considers which ones of those can be simplified (after the refactoring was done).
    /// If the inferred name of the member matches, the explicit name (from Complexify) can be removed.
    /// </summary>
    internal partial class CSharpInferredMemberNameReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpInferredMemberNameReducer() : base(s_pool)
        {
        }

        private static readonly Func<ArgumentSyntax, SemanticModel, OptionSet, CancellationToken, ArgumentSyntax> s_simplifyTupleName = SimplifyTupleName;

        private static ArgumentSyntax SimplifyTupleName(ArgumentSyntax node, SemanticModel semanticModel, OptionSet optionSet, CancellationToken cancellationToken)
        {
            // Tuple elements are arguments in a tuple expression
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

        private static readonly Func<AnonymousObjectMemberDeclaratorSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyAnonymousTypeMemberName = SimplifyAnonymousTypeMemberName;

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