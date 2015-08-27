// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class PrivateKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public PrivateKeywordRecommender()
            : base(SyntaxKind.PrivateKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.IsGlobalStatementContext ||
                IsValidContextForAccessor(context) ||
                IsValidContextForType(context, cancellationToken) ||
                IsValidContextForMember(context, cancellationToken);
        }

        private static bool IsValidContextForAccessor(CSharpSyntaxContext context)
        {
            if (context.TargetToken.IsAccessorDeclarationContext<PropertyDeclarationSyntax>(context.Position) ||
                context.TargetToken.IsAccessorDeclarationContext<IndexerDeclarationSyntax>(context.Position))
            {
                return CheckPreviousAccessibilityModifiers(context);
            }

            return false;
        }

        private static bool IsValidContextForMember(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                var modifiers = context.PrecedingModifiers;

                // can't have private + abstract/virtual/override/sealed
                if (modifiers.Contains(SyntaxKind.AbstractKeyword) ||
                    modifiers.Contains(SyntaxKind.VirtualKeyword) ||
                    modifiers.Contains(SyntaxKind.OverrideKeyword) ||
                    modifiers.Contains(SyntaxKind.SealedKeyword))
                {
                    return false;
                }

                return CheckPreviousAccessibilityModifiers(context);
            }

            return false;
        }

        private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers, validTypeDeclarations: SyntaxKindSet.ClassStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                // private things can't be in namespaces.
                var typeDecl = context.ContainingTypeDeclaration;
                if (typeDecl == null)
                {
                    return false;
                }

                return CheckPreviousAccessibilityModifiers(context);
            }

            return false;
        }

        private static bool CheckPreviousAccessibilityModifiers(CSharpSyntaxContext context)
        {
            var precedingModifiers = context.PrecedingModifiers;
            return
                !precedingModifiers.Contains(SyntaxKind.PublicKeyword) &&
                !precedingModifiers.Contains(SyntaxKind.InternalKeyword) &&
                !precedingModifiers.Contains(SyntaxKind.ProtectedKeyword) &&
                !precedingModifiers.Contains(SyntaxKind.PrivateKeyword);
        }
    }
}
