// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

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
    /// Same as <see cref="SyntaxKindSet.AllGlobalMemberModifiers"/> with ref-specific exclusions
    /// </summary>
    private static readonly ISet<SyntaxKind> RefGlobalMemberModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
        {
            // SyntaxKind.AsyncKeyword,    // async local functions cannot be byref
            SyntaxKind.ExternKeyword,
            SyntaxKind.InternalKeyword,
            SyntaxKind.NewKeyword,
            SyntaxKind.OverrideKeyword,
            SyntaxKind.PublicKeyword,
            SyntaxKind.PrivateKeyword,
            SyntaxKind.ReadOnlyKeyword,
            SyntaxKind.StaticKeyword,
            SyntaxKind.UnsafeKeyword,
            SyntaxKind.VolatileKeyword,
        };

    /// <summary>
    /// Same as <see cref="SyntaxKindSet.AllGlobalMemberModifiers"/> with ref-specific exclusions for C# script
    /// </summary>
    private static readonly ISet<SyntaxKind> RefGlobalMemberScriptModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
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
            IsRefParameterModifierContext(position, context) ||
            IsValidContextForType(context, cancellationToken) ||
            syntaxTree.IsAnonymousMethodParameterModifierContext(position, context.LeftToken) ||
            syntaxTree.IsPossibleLambdaParameterModifierContext(position, context.LeftToken, cancellationToken) ||
            context.TargetToken.IsConstructorOrMethodParameterArgumentContext() ||
            context.TargetToken.IsXmlCrefParameterModifierContext() ||
            IsValidNewByRefContext(syntaxTree, position, context, cancellationToken);
    }

    private static bool IsRefParameterModifierContext(int position, CSharpSyntaxContext context)
    {
        if (context.SyntaxTree.IsParameterModifierContext(
                position, context.LeftToken, includeOperators: false, out var parameterIndex, out var previousModifier))
        {
            if (previousModifier is SyntaxKind.None or SyntaxKind.ScopedKeyword)
            {
                return true;
            }

            if (previousModifier == SyntaxKind.ThisKeyword &&
                parameterIndex == 0 &&
                context.SyntaxTree.IsPossibleExtensionMethodContext(context.LeftToken))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValidNewByRefContext(SyntaxTree syntaxTree, int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return
            IsValidRefExpressionContext(context) ||
            context.IsDelegateReturnTypeContext ||
            syntaxTree.IsGlobalMemberDeclarationContext(position, syntaxTree.IsScript() ? RefGlobalMemberScriptModifiers : RefGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: RefMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                canBePartial: true,
                cancellationToken: cancellationToken);
    }

    private static bool IsValidRefExpressionContext(CSharpSyntaxContext context)
    {
        // {
        //     ref var x ...
        // 
        if (context.IsStatementContext)
        {
            return true;
        }

        // 
        //  ref Goo(int x, ...
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

            // scoped ref ...
            case SyntaxKind.ScopedKeyword:
            case SyntaxKind.IdentifierToken when token.Text == "scoped":
                return true;

            // {
            //     () => ref ...
            // 
            case SyntaxKind.EqualsGreaterThanToken:
                return true;

            // {
            //     for (ref var x ...
            //
            //     foreach (ref var x ...
            //
            case SyntaxKind.OpenParenToken:
                var previous = token.GetPreviousToken(includeSkipped: true);
                return previous.Kind() is SyntaxKind.ForKeyword or SyntaxKind.ForEachKeyword;

            // {
            //     ref var x = ref
            // 
            case SyntaxKind.EqualsToken:
                var parent = token.Parent;
                return parent?.Kind() == SyntaxKind.SimpleAssignmentExpression
                    || parent?.Parent?.Kind() == SyntaxKind.VariableDeclarator;

            // {
            //     var x = true ?
            //     var x = true ? ref y :
            case SyntaxKind.QuestionToken:
            case SyntaxKind.ColonToken:
                return token.Parent?.Kind() == SyntaxKind.ConditionalExpression;
        }

        return false;
    }

    private static bool IsValidContextForType(CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.IsTypeDeclarationContext(validModifiers: SyntaxKindSet.AllTypeModifiers,
            validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations, canBePartial: true, cancellationToken);
    }
}
