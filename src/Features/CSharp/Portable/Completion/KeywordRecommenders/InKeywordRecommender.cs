// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class InKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public InKeywordRecommender()
            : base(SyntaxKind.InKeyword)
        {
        }

        /// <summary>
        /// Same as <see cref="SyntaxKindSet.AllMemberModifiers"/> with in specific exclusions
        /// </summary>
        private static readonly ISet<SyntaxKind> InMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                // SyntaxKind.AsyncKeyword,    // async methods cannot be in
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                // SyntaxKind.ReadOnlyKeyword, // fields cannot be in
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VirtualKeyword,
                // SyntaxKind.VolatileKeyword, // fields cannot be in
            };

        /// <summary>
        /// Same as <see cref="SyntaxKindSet.AllGlobalMemberModifiers"/> with in specific exclusions
        /// </summary>
        private static readonly ISet<SyntaxKind> InGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                // SyntaxKind.AsyncKeyword,    // async methods cannot be in
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                // SyntaxKind.ReadOnlyKeyword, // fields cannot be in
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                // SyntaxKind.VolatileKeyword, // fields cannot be in
            };

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsValidContextInForEachClause(context) ||
                IsValidContextInFromClause(context, cancellationToken) ||
                IsValidContextInJoinClause(context, cancellationToken) ||
                context.TargetToken.IsTypeParameterVarianceContext() ||
                context.SyntaxTree.IsParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.SyntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.SyntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                context.SyntaxTree.IsGlobalMemberDeclarationContext(position, InGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: InMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        private bool IsValidContextInForEachClause(CSharpSyntaxContext context)
        {
            // cases:
            //   foreach (var v |
            //   foreach (var v i|
            //   foreach (var (x, y) |

            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                if (token.Parent is ForEachStatementSyntax statement && token == statement.Identifier)
                {
                    return true;
                }
            }
            else if (token.Kind() == SyntaxKind.CloseParenToken)
            {
                var statement = token.GetAncestor<ForEachVariableStatementSyntax>();
                if (statement != null && token.Span.End == statement.Variable.Span.End)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidContextInFromClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                // case:
                //   from x |
                if (token.GetPreviousToken(includeSkipped: true).IsKindOrHasMatchingText(SyntaxKind.FromKeyword))
                {
                    var typeSyntax = token.Parent as TypeSyntax;
                    if (!typeSyntax.IsPotentialTypeName(context.SemanticModel, cancellationToken))
                    {
                        return true;
                    }
                }

                if (token.Parent is FromClauseSyntax fromClause)
                {
                    // case:
                    //   from int x |
                    if (token == fromClause.Identifier && fromClause.Type != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsValidContextInJoinClause(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var token = context.TargetToken;

            if (token.Kind() == SyntaxKind.IdentifierToken)
            {
                var joinClause = token.Parent.FirstAncestorOrSelf<JoinClauseSyntax>();
                if (joinClause != null)
                {
                    // case:
                    //   join int x |
                    if (token == joinClause.Identifier && joinClause.Type != null)
                    {
                        return true;
                    }

                    // case:
                    //   join x |
                    if (joinClause.Type != null &&
                        joinClause.Type.IsKind(SyntaxKind.IdentifierName) &&
                        token == ((IdentifierNameSyntax)joinClause.Type).Identifier &&
                        !joinClause.Type.IsPotentialTypeName(context.SemanticModel, cancellationToken))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
