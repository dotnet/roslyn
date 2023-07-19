// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal abstract class AbstractSpecialTypePreselectingKeywordRecommender(
        SyntaxKind keywordKind,
        bool isValidInPreprocessorContext = false,
        bool shouldFormatOnCommit = false) : AbstractSyntacticSingleKeywordRecommender(keywordKind, isValidInPreprocessorContext, shouldFormatOnCommit)
    {
        protected abstract SpecialType SpecialType { get; }
        protected abstract bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken);

        // When the keyword is the inferred type in this context, we should treat it like its corresponding type symbol
        // in terms of MatchPripority, so the selection can be determined by how well it matches the filter text instead,
        // e.g. selecting "string" over "String" when user typed "str".
        protected override int PreselectMatchPriority => SymbolMatchPriority.PreferType;

        protected override bool ShouldPreselect(CSharpSyntaxContext context, CancellationToken cancellationToken)
            => context.InferredTypes.Any(static (t, self) => t.SpecialType == self.SpecialType, this);

        protected sealed override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // Filter out all special-types from locations where we think we only want something task-like.
            if (context.IsTaskLikeTypeContext)
                return false;

            return IsValidContextWorker(position, context, cancellationToken) ||
                IsAfterRefOrReadonlyInTopLevelOrMemberDeclaration(context, position, cancellationToken);
        }

        private static bool IsAfterRefOrReadonlyInTopLevelOrMemberDeclaration(CSharpSyntaxContext context, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            if (!syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) &&
                !syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken))
            {
                return false;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            // if we have `readonly` move backwards to see if we have `ref readonly`.
            if (token.Kind() is SyntaxKind.ReadOnlyKeyword)
                token = syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken);

            // if we're not after `ref` or `ref readonly` then don't offer a type-keyword here.
            if (token.Kind() != SyntaxKind.RefKeyword)
                return false;

            // If we're inside a type, this is always to have a ref/readonly type name.
            var containingType = token.GetAncestor<TypeDeclarationSyntax>();
            if (containingType != null)
                return true;

            // If not in a type, but in a namespace, this is not ok to have a ref/readonly type name.
            var containingNamespace = token.GetAncestor<BaseNamespaceDeclarationSyntax>();
            if (containingNamespace != null)
                return false;

            // otherwise, we're at top level.  Can have a ref/readonly top-level local/function.
            return true;
        }
    }
}
