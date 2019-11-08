// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class DelegateKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.InternalKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.UnsafeKeyword
            };

        public DelegateKeywordRecommender()
            : base(SyntaxKind.DelegateKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                (context is { IsNonAttributeExpressionContext: true, IsConstantExpressionContext: false }) ||
                IsAfterAsyncKeywordInExpressionContext(context, cancellationToken) ||
                context.IsTypeDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsAfterAsyncKeywordInExpressionContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.TargetToken.IsKindOrHasMatchingText(SyntaxKind.AsyncKeyword) &&
                context.SyntaxTree.IsExpressionContext(
                    context.TargetToken.SpanStart,
                    context.TargetToken,
                    attributes: false,
                    cancellationToken: cancellationToken,
                    semanticModelOpt: context.SemanticModel);
        }
    }
}
