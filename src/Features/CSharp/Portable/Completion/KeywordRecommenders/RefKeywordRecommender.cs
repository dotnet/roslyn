// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class RefKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public RefKeywordRecommender()
            : base(SyntaxKind.RefKeyword)
        {
        }

        /// <summary>
        /// Same as <see cref="SyntaxKindSet.AllMemberModifiers"/> with ref specific exclusions
        /// </summary>
        private static readonly ISet<SyntaxKind> RefMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                SyntaxKind.AbstractKeyword,
                // SyntaxKind.AsyncKeyword,    // async methods cannot be byref
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                // SyntaxKind.ReadOnlyKeyword, // fields cannot be byref
                SyntaxKind.SealedKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                SyntaxKind.VirtualKeyword,
                // SyntaxKind.VolatileKeyword, // fields cannot be byref
            };

        /// <summary>
        /// Same as <see cref="SyntaxKindSet.AllGlobalMemberModifiers"/> with ref specific exclusions
        /// </summary>
        private static readonly ISet<SyntaxKind> RefGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
            {
                // SyntaxKind.AsyncKeyword,    // async methods cannot be byref
                SyntaxKind.ExternKeyword,
                SyntaxKind.InternalKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.OverrideKeyword,
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                // SyntaxKind.ReadOnlyKeyword, // fields cannot be byref
                SyntaxKind.StaticKeyword,
                SyntaxKind.UnsafeKeyword,
                // SyntaxKind.VolatileKeyword, // fields cannot be byref
            };

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                syntaxTree.IsParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
                context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
                context.TargetToken.IsXmlCrefParameterModifierContext() ||
                (syntaxTree.Options.Features.ContainsKey("refLocalsAndReturns") && 
                    IsValidNewByRefContext(syntaxTree, position, context, cancellationToken));
        }

        private bool IsValidNewByRefContext(SyntaxTree syntaxTree, int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            return
                IsValidRefDeclarationOrAssignmentContext(syntaxTree, position, context, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, RefGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: RefMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: true,
                    cancellationToken: cancellationToken);
        }

        private static bool IsValidRefDeclarationOrAssignmentContext(SyntaxTree syntaxTree, int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            // {
            //     ref var x ...
            // 
            if (context.IsStatementContext)
            {
                return true;
            }

            // 
            //  ref Foo(int x, ...
            // 
            if (context.IsGlobalStatementContext)
            {
                return true;
            }

            var token = context.TargetToken;

            switch (token.Kind())
            {
                // {
                //     return ref  ...
                // 
                case SyntaxKind.ReturnKeyword:
                    return true;

                    // {
                //     () => ref ...
                // 
                case SyntaxKind.EqualsGreaterThanToken:
                    return true;

                // {
                //     for(ref var x ...
                // 
                case SyntaxKind.OpenParenToken:
                    var previous = token.GetPreviousToken(includeSkipped: true);
                    return previous.IsKind(SyntaxKind.ForKeyword);

                // {
                //     ref var x = ref
                // 
                case SyntaxKind.EqualsToken:
                    return token.Parent?.Parent?.Kind() == SyntaxKind.VariableDeclarator;
            }

            return false;
        }
    }
}
