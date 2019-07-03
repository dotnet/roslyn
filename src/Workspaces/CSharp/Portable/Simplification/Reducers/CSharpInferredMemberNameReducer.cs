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

            if (RemovalCausesAmbiguity(((TupleExpressionSyntax)node.Parent).Arguments, node))
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

            if (RemovalCausesAmbiguity(((AnonymousObjectCreationExpressionSyntax)node.Parent).Initializers, node))
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

        // An explicit name cannot be removed if some other position would produce it as inferred name
        private static bool RemovalCausesAmbiguity(SeparatedSyntaxList<ArgumentSyntax> arguments, ArgumentSyntax toRemove)
        {
            var name = toRemove.NameColon.Name.Identifier.ValueText;
            foreach (var argument in arguments)
            {
                if (argument == toRemove)
                {
                    continue;
                }

                if (argument.NameColon is null && argument.Expression.TryGetInferredMemberName()?.Equals(name) == true)
                {
                    return true;
                }
            }

            return false;
        }

        // An explicit name cannot be removed if some other position would produce it as inferred name
        private static bool RemovalCausesAmbiguity(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax> initializers, AnonymousObjectMemberDeclaratorSyntax toRemove)
        {
            var name = toRemove.NameEquals.Name.Identifier.ValueText;
            foreach (var initializer in initializers)
            {
                if (initializer == toRemove)
                {
                    continue;
                }

                if (initializer.NameEquals is null && initializer.Expression.TryGetInferredMemberName()?.Equals(name) == true)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
