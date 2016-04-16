// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class VoidKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        private static readonly ISet<SyntaxKind> s_validModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
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
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.AsyncKeyword
        };

        public VoidKeywordRecommender()
            : base(SyntaxKind.VoidKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                IsMemberReturnTypeContext(position, context, cancellationToken) ||
                context.IsGlobalStatementContext ||
                context.IsTypeOfExpressionContext ||
                syntaxTree.IsSizeOfExpressionContext(position, context.LeftToken, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                IsUnsafeLocalVariableDeclarationContext(context) ||
                IsUnsafeParameterTypeContext(context) ||
                IsUnsafeCastTypeContext(context) ||
                IsUnsafeDefaultExpressionContext(context, cancellationToken) ||
                context.IsFixedVariableDeclarationContext;
        }

        private bool IsUnsafeDefaultExpressionContext(CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                context.TargetToken.IsUnsafeContext() &&
                context.SyntaxTree.IsDefaultExpressionContext(context.Position, context.LeftToken, cancellationToken);
        }

        private bool IsUnsafeCastTypeContext(CSharpSyntaxContext context)
        {
            if (context.TargetToken.IsUnsafeContext())
            {
                if (context.IsDefiniteCastTypeContext)
                {
                    return true;
                }

                var token = context.TargetToken;

                if (token.Kind() == SyntaxKind.OpenParenToken &&
                    token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsUnsafeParameterTypeContext(CSharpSyntaxContext context)
        {
            return
                context.TargetToken.IsUnsafeContext() &&
                context.IsParameterTypeContext;
        }

        private bool IsUnsafeLocalVariableDeclarationContext(CSharpSyntaxContext context)
        {
            if (context.TargetToken.IsUnsafeContext())
            {
                return
                    context.IsLocalVariableDeclarationContext ||
                    context.IsStatementContext;
            }

            return false;
        }

        private bool IsMemberReturnTypeContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: s_validModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }
    }
}
