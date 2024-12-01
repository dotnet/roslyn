// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal class ReadOnlyKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
{
    private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.StaticKeyword,
        };

    public ReadOnlyKeywordRecommender()
        : base(SyntaxKind.ReadOnlyKeyword)
    {
    }

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            context.IsGlobalStatementContext ||
            IsRefReadOnlyContext(context) ||
            IsValidContextForType(context, cancellationToken) ||
            context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: s_validMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken) ||
            IsStructAccessorContext(context);
    }

    private static bool IsRefReadOnlyContext(CSharpSyntaxContext context)
        => context.TargetToken.IsKind(SyntaxKind.RefKeyword) &&
           (context.TargetToken.Parent.IsKind(SyntaxKind.RefType) ||
            context.IsParameterTypeContext ||
            context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
            context.IsFunctionPointerTypeArgumentContext);

    private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers.Except([SyntaxKind.ReadOnlyKeyword]).ToSet(),
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: false, cancellationToken);
    }

    private static bool IsStructAccessorContext(CSharpSyntaxContext context)
    {
        var type = context.ContainingTypeDeclaration;
        return type is not null &&
            type.Kind() is SyntaxKind.StructDeclaration or SyntaxKind.RecordStructDeclaration &&
            context.TargetToken.IsAnyAccessorDeclarationContext(context.Position, SyntaxKind.ReadOnlyKeyword);
    }
}
