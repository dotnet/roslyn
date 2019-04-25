// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class EventKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validClassModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.NewKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.VirtualKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.AbstractKeyword,
                SyntaxKind.ExternKeyword,
                SyntaxKind.UnsafeKeyword
            };

        private static readonly ISet<SyntaxKind> s_validStructModifiers = new HashSet<SyntaxKind>(s_validClassModifiers, SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.ReadOnlyKeyword,
            };

        public EventKeywordRecommender()
            : base(SyntaxKind.EventKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsGlobalStatementContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(validModifiers: s_validClassModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken) ||
                context.IsMemberDeclarationContext(validModifiers: s_validStructModifiers, validTypeDeclarations: SyntaxKindSet.StructOnlyTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken) ||
                context.IsMemberAttributeContext(SyntaxKindSet.ClassInterfaceStructTypeDeclarations, cancellationToken);
        }
    }
}
