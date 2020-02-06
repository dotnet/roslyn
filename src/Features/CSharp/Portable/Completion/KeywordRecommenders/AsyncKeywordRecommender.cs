// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AsyncKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public AsyncKeywordRecommender()
            : base(SyntaxKind.AsyncKeyword, isValidInPreprocessorContext: false)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsAnyExpressionContext)
            {
                return true;
            }

            if (context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
            {
                return false;
            }

            return InMemberDeclarationContext(position, context, cancellationToken)
                || context.SyntaxTree.IsLocalFunctionDeclarationContext(position, cancellationToken);
        }

        private static bool InMemberDeclarationContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsGlobalStatementContext
                || context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken)
                || context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }
    }
}
