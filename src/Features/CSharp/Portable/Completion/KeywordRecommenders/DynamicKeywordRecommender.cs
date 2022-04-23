// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
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
        private static bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsPreProcessorDirectiveContext ||
                context.IsInTaskLikeTypeContext)
            {
                return false;
            }

            return IsDynamicTypeContext(position, context, cancellationToken);
        }

        public ImmutableArray<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return IsValidContext(position, context, cancellationToken)
                ? ImmutableArray.Create(new RecommendedKeyword("dynamic"))
                : ImmutableArray<RecommendedKeyword>.Empty;
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
                context.IsFunctionPointerTypeArgumentContext ||
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
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsAfterRefTypeContext(CSharpSyntaxContext context)
            => context.TargetToken.IsKind(SyntaxKind.RefKeyword, SyntaxKind.ReadOnlyKeyword) &&
               context.TargetToken.Parent.IsKind(SyntaxKind.RefType);
    }
}
