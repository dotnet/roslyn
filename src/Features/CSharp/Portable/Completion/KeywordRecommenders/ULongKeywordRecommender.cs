// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders;

internal sealed class ULongKeywordRecommender() : AbstractSpecialTypePreselectingKeywordRecommender(SyntaxKind.ULongKeyword)
{
    protected override SpecialType SpecialType => SpecialType.System_UInt64;

    protected override bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntaxTree = context.SyntaxTree;
        return
            (context.IsGenericTypeArgumentContext && !context.TargetToken.GetRequiredParent().HasAncestor<XmlCrefAttributeSyntax>()) ||
            context.IsEnumBaseListContext ||
            context.IsFixedVariableDeclarationContext ||
            context.IsLocalVariableDeclarationContext ||
            context.IsObjectCreationTypeContext ||
            context.IsParameterTypeContext ||
            context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
            context.IsPossibleTupleContext ||
            context.IsPrimaryFunctionExpressionContext ||
            context.IsStatementContext ||
            context.IsUsingAliasTypeContext ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
            syntaxTree.IsAfterKeyword(position, SyntaxKind.StackAllocKeyword, cancellationToken) ||
            syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
            context.IsMemberDeclarationContext(
                validModifiers: SyntaxKindSet.AllMemberModifiers,
                validTypeDeclarations: SyntaxKindSet.NonEnumTypeDeclarations,
                canBePartial: false,
                cancellationToken: cancellationToken);
    }
}
