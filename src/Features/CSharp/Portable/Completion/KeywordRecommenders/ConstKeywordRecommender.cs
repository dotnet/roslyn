// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class ConstKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.ProtectedKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.PrivateKeyword,
    };

    private static readonly ISet<SyntaxKind> s_validGlobalModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.NewKeyword,
        SyntaxKind.PublicKeyword,
        SyntaxKind.InternalKeyword,
        SyntaxKind.PrivateKeyword,
    };

    public ConstKeywordRecommender()
        : base(SyntaxKind.ConstKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            IsMemberDeclarationContext(context, cancellationToken) ||
            IsLocalVariableDeclaration(context);
    }

    private static bool IsMemberDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, s_validGlobalModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: s_validModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken);
    }

    private static bool IsLocalVariableDeclaration(CSharpSyntaxContext context)
    {
        // cases:
        //   void Goo() {
        //     |
        //
        //   |
        return
            context.IsStatementContext ||
            context.IsGlobalStatementContext;
    }
}
