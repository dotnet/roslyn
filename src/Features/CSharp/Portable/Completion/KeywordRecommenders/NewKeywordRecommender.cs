// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class NewKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.ReadOnlyKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VirtualKeyword,
                SyntaxKind.VolatileKeyword,
            };

        protected static readonly ISet<SyntaxKind> ValidTypeModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword
            };

        public NewKeywordRecommender()
            : base(SyntaxKind.NewKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsNewConstraintContext(context) ||
                context.IsAnyExpressionContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                IsMemberDeclarationContext(context, cancellationToken) ||
                IsTypeDeclarationContext(context, cancellationToken);
        }

        private bool IsTypeDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.IsTypeDeclarationContext(validModifiers: ValidTypeModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations, canBePartial: false, cancellationToken: cancellationToken))
            {
                // we must be on a nested type.
                var token = context.LeftToken;
                return token.GetAncestors<TypeDeclarationSyntax>()
                    .Any(t => token.SpanStart > t.OpenBraceToken.Span.End &&
                              token.Span.End < t.CloseBraceToken.SpanStart);
            }

            return false;
        }

        private bool IsMemberDeclarationContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.SyntaxTree.IsGlobalMemberDeclarationContext(context.Position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private static bool IsNewConstraintContext(CSharpSyntaxContext context)
        {
            // cases:
            //    where T : |
            //    where T : class, |
            //    where T : Goo, |
            // note: 'new()' can't come after a 'struct' constraint.

            if (context.SyntaxTree.IsTypeParameterConstraintStartContext(context.Position, context.LeftToken))
            {
                return true;
            }

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.CommaToken &&
                token.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
            {
                var constraintClause = token.Parent as TypeParameterConstraintClauseSyntax;
                if (!constraintClause.Constraints
                        .OfType<ClassOrStructConstraintSyntax>()
                        .Any(c => c.ClassOrStructKeyword.Kind() == SyntaxKind.StructKeyword))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
