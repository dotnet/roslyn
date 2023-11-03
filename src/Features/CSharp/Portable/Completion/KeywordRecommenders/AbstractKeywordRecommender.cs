// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class AbstractKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validNonInterfaceMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ExternKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.OverrideKeyword,
        };

        private static readonly ISet<SyntaxKind> s_validInterfaceMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ExternKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.OverrideKeyword,
        };

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

        public AbstractKeywordRecommender()
            : base(SyntaxKind.AbstractKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validNonInterfaceMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validInterfaceMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.InterfaceOnlyTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken) ||
                context.IsTypeDeclarationContext(
                    validModifiers: s_validTypeModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }
    }
}
