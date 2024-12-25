// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class StaticKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    private static readonly ISet<SyntaxKind> s_validTypeModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.InternalKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.FileKeyword,
    };

    private static readonly ISet<SyntaxKind> s_validNonInterfaceMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.AsyncKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.VolatileKeyword,
    };

    private static readonly ISet<SyntaxKind> s_validInterfaceMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.AbstractKeyword,
        SyntaxKind.AsyncKeyword,
        SyntaxKind.ExternKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.SealedKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.VolatileKeyword,
        SyntaxKind.VirtualKeyword,
    };

    private static readonly ISet<SyntaxKind> s_validGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.ExternKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.PrivateKeyword,
        SyntaxKind.ReadOnlyKeyword,
        SyntaxKind.UnsafeKeyword,
        SyntaxKind.VolatileKeyword,
    };

    private static readonly ISet<SyntaxKind> s_validLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.ExternKeyword,
        SyntaxKind.AsyncKeyword,
        SyntaxKind.UnsafeKeyword
    };

    public StaticKeywordRecommender()
        : base(SyntaxKind.StaticKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsGlobalStatementContext ||
            context.TargetToken.IsUsingKeywordInUsingDirective() ||
            (context.TargetToken.IsKind(SyntaxKind.UsingKeyword) && context.TargetToken.Parent?.IsParentKind(SyntaxKind.GlobalStatement) == true) ||
            IsValidContextForType(context, cancellationToken) ||
            IsValidContextForMember(context, cancellationToken) ||
            context.SyntaxTree.IsLambdaDeclarationContext(position, otherModifier: SyntaxKind.AsyncKeyword, cancellationToken) ||
            context.SyntaxTree.IsLocalFunctionDeclarationContext(position, s_validLocalFunctionModifiers, cancellationToken);
    }

    private static bool IsValidContextForMember(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, s_validGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: s_validNonInterfaceMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: s_validInterfaceMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.InterfaceOnlyTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken);
    }

    private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsTypeDeclarationContext(
            validModifiers: s_validTypeModifiers,
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
            canBePartial: false,
            cancellationToken: cancellationToken);
    }
}
