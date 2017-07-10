// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

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

        internal static bool CanSimplifyTupleElementName(ArgumentSyntax node, CSharpParseOptions parseOptions)
        {
            // Tuple elements are arguments in a tuple expression
            if (node.NameColon == null || !node.IsParentKind(SyntaxKind.TupleExpression))
            {
                return false;
            }

            if (parseOptions.LanguageVersion < LanguageVersion.CSharp7_1)
            {
                return false;
            }

            var inferredName = node.Expression.TryGetInferredMemberName();
            if (inferredName == null || inferredName != node.NameColon.Name.Identifier.ValueText)
            {
                return false;
            }

            return true;
        }

        internal static bool CanSimplifyAnonymousTypeMemberName(AnonymousObjectMemberDeclaratorSyntax node)
        {
            if (node.NameEquals == null)
            {
                return false;
            }

            var inferredName = node.Expression.TryGetInferredMemberName();
            if (inferredName == null || inferredName != node.NameEquals.Name.Identifier.ValueText)
            {
                return false;
            }

            return true;
        }
    }
}
