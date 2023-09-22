// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class DelegateKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
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
        if (context.IsGlobalStatementContext ||
            context.IsUsingAliasTypeContext ||
            IsAfterAsyncKeywordInExpressionContext(context, cancellationToken) ||
            context.IsTypeDeclarationContext(
                validModifiers: s_validModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken))
        {
            return true;
        }

        // now look for places that a function pointer could go `delegate*...`
        if (context.IsNonAttributeExpressionContext || context.IsTypeContext)
        {
            if (context.IsConstantExpressionContext)
                return false;

            if (context.SyntaxTree.IsUsingStaticContext(position, cancellationToken))
                return false;

            if (context.IsAttributeNameContext)
                return false;

            return true;
        }

        return false;
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
                semanticModel: context.SemanticModel);
    }
}
