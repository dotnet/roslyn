// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class VoidKeywordRecommender() : AbstractSyntacticSingleKeywordRecommender(SyntaxKind.VoidKeyword)
{
    private static readonly ISet<SyntaxKind> s_validClassInterfaceRecordModifiers = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer)
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

    private static readonly ISet<SyntaxKind> s_validStructModifiers = new HashSet<SyntaxKind>(s_validClassInterfaceRecordModifiers, SyntaxFacts.EqualityComparer)
    {
        SyntaxKind.ReadOnlyKeyword,
    };

    protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntaxTree = context.SyntaxTree;
        return
            IsMemberReturnTypeContext(position, context, cancellationToken) ||
            context.IsGlobalStatementContext ||
            context.IsTypeOfExpressionContext ||
            syntaxTree.IsSizeOfExpressionContext(position, context.LeftToken) ||
            context.IsDelegateReturnTypeContext ||
            context.IsFunctionPointerTypeArgumentContext ||
            IsUnsafeLocalVariableDeclarationContext(context) ||
            IsUnsafeParameterTypeContext(context) ||
            IsUnsafeCastTypeContext(context) ||
            IsUnsafeDefaultExpressionContext(context) ||
            IsUnsafeUsingDirectiveContext(context) ||
            context.IsFixedVariableDeclarationContext ||
            context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.SyntaxTree.IsLocalFunctionDeclarationContext(position, cancellationToken);
    }

    private static bool IsUnsafeDefaultExpressionContext(CSharpSyntaxContext context)
    {
        return
            context.TargetToken.IsUnsafeContext() &&
            context.SyntaxTree.IsDefaultExpressionContext(context.Position, context.LeftToken);
    }

    private static bool IsUnsafeCastTypeContext(CSharpSyntaxContext context)
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

    private static bool IsUnsafeParameterTypeContext(CSharpSyntaxContext context)
    {
        return
            context.TargetToken.IsUnsafeContext() &&
            context.IsParameterTypeContext;
    }

    private static bool IsUnsafeLocalVariableDeclarationContext(CSharpSyntaxContext context)
    {
        if (context.TargetToken.IsUnsafeContext())
        {
            return
                context.IsLocalVariableDeclarationContext ||
                context.IsStatementContext;
        }

        return false;
    }

    private static bool IsUnsafeUsingDirectiveContext(CSharpSyntaxContext context)
    {
        return
            context.IsUsingAliasTypeContext &&
            context.TargetToken.IsUnsafeContext();
    }

    private static bool IsMemberReturnTypeContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        => context.SyntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(validModifiers: s_validClassInterfaceRecordModifiers, validTypeDeclarations: SyntaxKindSet.ClassInterfaceRecordTypeDeclarations, canBePartial: true, cancellationToken) ||
            context.IsMemberDeclarationContext(validModifiers: s_validStructModifiers, validTypeDeclarations: SyntaxKindSet.StructOnlyTypeDeclarations, canBePartial: false, cancellationToken);
}
