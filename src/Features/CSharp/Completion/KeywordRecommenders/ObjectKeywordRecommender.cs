// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
    internal class ObjectKeywordRecommender : AbstractSyntacticSingleKeywordRecommender
    {
        public ObjectKeywordRecommender()
            : base(SyntaxKind.ObjectKeyword)
        {
        }

        protected override bool IsValidContext(int position, CSharpSyntaxContext context, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            return
                context.IsNonAttributeExpressionContext ||
                context.IsDefiniteCastTypeContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsObjectCreationTypeContext ||
                (context.IsGenericTypeArgumentContext && !context.TargetToken.Parent.HasAncestor<XmlCrefAttributeSyntax>()) ||
                context.IsIsOrAsTypeContext ||
                context.IsLocalVariableDeclarationContext ||
                context.IsParameterTypeContext ||
                context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
                context.IsImplicitOrExplicitOperatorTypeContext ||
                context.IsTypeOfExpressionContext ||
                context.IsCrefContext ||
                syntaxTree.IsDefaultExpressionContext(position, context.LeftToken, cancellationToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
                    canBePartial: false,
                    cancellationToken: cancellationToken);
        }
    }
}
