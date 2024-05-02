﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class StringKeywordRecommender : AbstractSpecialTypePreselectingKeywordRecommender
{
    public StringKeywordRecommender()
        : base(SyntaxKind.StringKeyword)
    {
    }

    protected override SpecialType SpecialType => SpecialType.System_String;

    protected override bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntaxTree = context.SyntaxTree;
        return
            (context.IsGenericTypeArgumentContext && !context.TargetToken.GetRequiredParent().HasAncestor<XmlCrefAttributeSyntax>()) ||
            context.IsAnyExpressionContext ||
            context.IsCrefContext ||
            context.IsDefiniteCastTypeContext ||
            context.IsDelegateReturnTypeContext ||
            context.IsFunctionPointerTypeArgumentContext ||
            context.IsGlobalStatementContext ||
            context.IsImplicitOrExplicitOperatorTypeContext ||
            context.IsIsOrAsTypeContext ||
            context.IsLocalFunctionDeclarationContext ||
            context.IsLocalVariableDeclarationContext ||
            context.IsObjectCreationTypeContext ||
            context.IsParameterTypeContext ||
            context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
            context.IsPossibleTupleContext ||
            context.IsStatementContext ||
            context.IsTypeOfExpressionContext ||
            context.IsUsingAliasTypeContext ||
            syntaxTree.IsDefaultExpressionContext(position, context.LeftToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
            syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: SyntaxKindSet.AllMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.NonEnumTypeDeclarations,
                canBePartial: false,
                cancellationToken);
    }
}
