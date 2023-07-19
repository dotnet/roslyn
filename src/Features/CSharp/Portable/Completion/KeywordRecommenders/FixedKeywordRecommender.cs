// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class FixedKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            SyntaxKind.NewKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.ProtectedKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.UnsafeKeyword,
        };

        public FixedKeywordRecommender()
            : base(SyntaxKind.FixedKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
            => IsUnsafeStatementContext(context) || IsMemberDeclarationContext(context, cancellationToken);

        private static bool IsMemberDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.TargetToken.IsUnsafeContext() &&
               (context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
               context.IsMemberDeclarationContext(validModifiers: s_validModifiers, validTypeDeclarations: SyntaxKindSet.StructOnlyTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken));
        }

        private static bool IsUnsafeStatementContext(CSharpSyntaxContext context)
            => context.TargetToken.IsUnsafeContext() && context.IsStatementContext;
    }
}
