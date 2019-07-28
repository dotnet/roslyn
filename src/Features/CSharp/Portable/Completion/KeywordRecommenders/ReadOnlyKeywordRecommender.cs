// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
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
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsRefReadOnlyContext(CSharpSyntaxContext context)
            => context.TargetToken.IsKind(SyntaxKind.RefKeyword) &&
               context.TargetToken.Parent.IsKind(SyntaxKind.RefType);

        private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: true, cancellationToken);
        }
    }
}
