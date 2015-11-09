// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Completion.Providers;
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

        public IEnumerable<RecommendedKeyword> RecommendKeywords(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (IsValidContext(position, context, cancellationToken))
            {
                return SpecializedCollections.SingletonEnumerable(new RecommendedKeyword("dynamic"));
            }

            return null;
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
                syntaxTree.IsDefaultExpressionContext(position, context.LeftToken, cancellationToken) ||
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
    }
}
