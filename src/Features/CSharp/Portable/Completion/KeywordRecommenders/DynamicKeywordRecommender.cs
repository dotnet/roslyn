// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class DynamicKeywordRecommender : IKeywordRecommender<CSharpSyntaxContext>
    {
        private bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            if (context.IsPreProcessorDirectiveContext)
            {
                return false;
            }

            return IsDynamicTypeContext(position, context, cancellationToken);
        }

        public Task<IEnumerable<RecommendedKeyword>> RecommendKeywordsAsync(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (IsValidContext(position, context, cancellationToken))
            {
                return Task.FromResult(SpecializedCollections.SingletonEnumerable(new RecommendedKeyword("dynamic")));
            }

            return Task.FromResult<IEnumerable<RecommendedKeyword>>(null);
        }

        protected static bool IsDynamicTypeContext(
            int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;

            // first do quick exit check
            if (syntaxTree.IsDefinitelyNotTypeContext(position, cancellationToken))
            {
                return false;
            }

            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsDefiniteCastTypeContext ||
                syntaxTree.IsPossibleCastTypeContext(position, context.LeftToken, cancellationToken) ||
                context.IsObjectCreationTypeContext ||
                context.IsGenericTypeArgumentContext ||
                context.IsIsOrAsTypeContext ||
                syntaxTree.IsDefaultExpressionContext(position, context.LeftToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                IsAfterRefTypeContext(context) ||
                context.IsLocalVariableDeclarationContext ||
                context.IsParameterTypeContext ||
                context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
                context.IsDelegateReturnTypeContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsAfterRefTypeContext(CSharpSyntaxContext context)
            => context.TargetToken.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword) &&
               context.TargetToken.Parent.IsKind(SyntaxKind.RefType);
    }
}
