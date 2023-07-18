// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class UnsafeKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validTypeModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.AbstractKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.SealedKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.FileKeyword,
        };

        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.AbstractKeyword,
            SyntaxKind.ExternKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.OverrideKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.SealedKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.VirtualKeyword,
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
            SyntaxKind.StaticKeyword,
            SyntaxKind.VolatileKeyword,
        };

        private static readonly ISet<SyntaxKind> s_validLocalFunctionModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.ExternKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.AsyncKeyword
        };

        public UnsafeKeywordRecommender()
            : base(SyntaxKind.UnsafeKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsTypeDeclarationContext(validModifiers: s_validTypeModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken) ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, s_validGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken) ||
                syntaxTree.IsLocalFunctionDeclarationContext(position, s_validLocalFunctionModifiers, cancellationToken) ||
                (context.IsInImportsDirective && context.TargetToken.Kind() is SyntaxKind.UsingKeyword or SyntaxKind.StaticKeyword);
        }
    }
}
