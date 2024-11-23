// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class PublicKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    public PublicKeywordRecommender()
        : base(SyntaxKind.PublicKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsGlobalStatementContext ||
            IsValidContextForType(context, cancellationToken) ||
            IsValidContextForMember(context, cancellationToken);
    }

    private static bool IsValidContextForMember(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: SyntaxKindSet.AllMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken))
        {
            return CheckPreviousAccessibilityModifiers(context);
        }

        return false;
    }

    private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
        {
            return CheckPreviousAccessibilityModifiers(context);
        }

        return false;
    }

    private static bool CheckPreviousAccessibilityModifiers(CSharpSyntaxContext context)
    {
        var precedingModifiers = context.PrecedingModifiers;
        return
            !precedingModifiers.Contains(SyntaxKind.FileKeyword) &&
            !precedingModifiers.Contains(SyntaxKind.PublicKeyword) &&
            !precedingModifiers.Contains(SyntaxKind.InternalKeyword) &&
            !precedingModifiers.Contains(SyntaxKind.ProtectedKeyword) &&
            !precedingModifiers.Contains(SyntaxKind.PrivateKeyword);
    }
}
