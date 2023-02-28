// Licensed to the .NET Foundation under one or more agreements.
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

namespace Microsoft.CodeAnalysis.CSharp.Completion.KeywordRecommenders
{
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
                context.IsAnyExpressionContext ||
                context.IsDefiniteCastTypeContext ||
                context.IsStatementContext ||
                context.IsGlobalStatementContext ||
                context.IsObjectCreationTypeContext ||
                (context.IsGenericTypeArgumentContext && !context.TargetToken.GetRequiredParent().HasAncestor<XmlCrefAttributeSyntax>()) ||
                context.IsFunctionPointerTypeArgumentContext ||
                context.IsIsOrAsTypeContext ||
                context.IsLocalVariableDeclarationContext ||
                context.IsParameterTypeContext ||
                context.IsPossibleLambdaOrAnonymousMethodParameterTypeContext ||
                context.IsLocalFunctionDeclarationContext ||
                context.IsImplicitOrExplicitOperatorTypeContext ||
                context.IsTypeOfExpressionContext ||
                context.IsCrefContext ||
                syntaxTree.IsDefaultExpressionContext(position, context.LeftToken) ||
                syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
                context.IsDelegateReturnTypeContext ||
                syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
                context.IsPossibleTupleContext ||
                context.IsMemberDeclarationContext(
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken) ||
                IsAfterRefOrReadonlyInTopLevelOrMemberDeclaration(context, position, cancellationToken);
        }

        private static bool IsAfterRefOrReadonlyInTopLevelOrMemberDeclaration(CSharpSyntaxContext context, int position, CancellationToken cancellationToken)
        {
            var syntaxTree = context.SyntaxTree;
            if (!syntaxTree.IsAfterKeyword(position, SyntaxKind.RefKeyword, cancellationToken) &&
                !syntaxTree.IsAfterKeyword(position, SyntaxKind.ReadOnlyKeyword, cancellationToken))
            {
                return false;
            }

            var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            // if we have `readonly` move backwards to see if we have `ref readonly`.
            if (token.Kind() is SyntaxKind.ReadOnlyKeyword)
                token = syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken);

            // if we're not after `ref` or `ref readonly` then don't offer `string` here.
            if (token.Kind() != SyntaxKind.RefKeyword)
                return false;

            // check if the location prior to the 'ref/readonly' is itself a member start location.  If so,
            // then it's fine to show 'string'.  For example, `class C { public ref $$ }`
            if (syntaxTree.IsMemberDeclarationContext(
                    token.SpanStart,
                    contextOpt: null,
                    validModifiers: SyntaxKindSet.AllMemberModifiers,
                    validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructRecordTypeDeclarations,
                    canBePartial: false,
                    cancellationToken))
            {
                return true;
            }

            // Compiler error recovery sometimes treats 'ref' standing along as an incomplete member syntax.
            if (token.Parent is RefTypeSyntax { Parent: IncompleteMemberSyntax { Parent: CompilationUnitSyntax } })
                return true;

            // Otherwise see if we're in a global statement.
            return token.GetAncestors<SyntaxNode>().Any(a => a is GlobalStatementSyntax);
        }
    }
}
