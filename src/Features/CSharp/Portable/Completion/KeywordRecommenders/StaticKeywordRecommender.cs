// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
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
        };

        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
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

        public StaticKeywordRecommender()
            : base(SyntaxKind.StaticKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                context.TargetToken.IsUsingKeywordInUsingDirective() ||
                context.IsStatementContext ||
                IsValidContextForType(context, cancellationToken) ||
                IsValidContextForMember(context, cancellationToken);
        }

        private static bool IsValidContextForMember(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, s_validGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsTypeDeclarationContext(
                validModifiers: s_validTypeModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken);
        }
    }
}
