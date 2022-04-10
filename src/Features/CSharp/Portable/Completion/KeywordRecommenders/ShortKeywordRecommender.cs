// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ShortKeywordRecommender : AbstractSpecialTypePreselectingKeywordRecommender
    {
        public ShortKeywordRecommender()
            : base(SyntaxKind.ShortKeyword)
        {
        }

        protected override bool IsValidContextWorker(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsAnyExpressionContext ||
                context.IsDefiniteCastTypeContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsObjectCreationTypeContext ||
                (context.IsGenericTypeArgumentContext && !context.TargetToken.Parent.HasAncestor<XmlCrefAttributeSyntax>()) ||
                context.IsFunctionPointerTypeArgumentContext ||
                context.IsEnumBaseListContext ||
                context.IsIsOrAsTypeContext ||
                context.IsLocalVariableDeclarationContext ||
                context.IsFixedVariableDeclarationContext ||
                context.IsParameterTypeContext ||
                context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
                context.IsLocalFunctionDeclarationContext ||
                context.IsImplicitOrExplicitOperatorTypeContext ||
                context.IsPrimaryFunctionExpressionContext ||
                context.IsCrefContext ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.StackAllocKeyword, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsPossibleTupleContext ||
                context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }

        protected override SpecialType SpecialType => SpecialType.System_Int16;
    }
}
