// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ProtectedKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ProtectedKeywordRecommender()
            : base(SyntaxKind.ProtectedKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
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
            if (context.IsMemberDeclarationContext(validModifiers: SyntaxKindSet.AllMemberModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                return CheckPreviousAccessibilityModifiers(context);
            }

            return false;
        }

        private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceRecordTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                // protected things can't be in namespaces.
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
            // We can show up after 'internal' and 'private'.
            var precedingModifiers = context.PrecedingModifiers;
            return
                !precedingModifiers.Contains(SyntaxKind.FileKeyword) &&
                !precedingModifiers.Contains(SyntaxKind.PublicKeyword) &&
                !precedingModifiers.Contains(SyntaxKind.ProtectedKeyword);
        }
    }
}
