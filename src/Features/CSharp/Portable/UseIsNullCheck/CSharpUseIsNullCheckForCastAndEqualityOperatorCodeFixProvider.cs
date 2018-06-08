// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseIsNullCheck;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseIsNullCheckDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            context.RegisterCodeFix(
                new MyCodeAction(CSharpFeaturesResources.Use_is_null_check,
                c => this.FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.Properties[UseIsNullConstants.Kind] != UseIsNullConstants.CastAndEqualityKey)
                {
                    continue;
                }

                var binary = (BinaryExpressionSyntax)diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);

                editor.ReplaceNode(
                    binary, 
                    (current, g) => Rewrite((BinaryExpressionSyntax)current));
            }

            return SpecializedTasks.EmptyTask;
        }

        private static ExpressionSyntax Rewrite(BinaryExpressionSyntax binary)
        {
            var isPattern = RewriteWorker(binary);
            if (binary.IsKind(SyntaxKind.EqualsExpression))
            {
                return isPattern;
            }

            // convert:  (object)expr != null   to    !(expr is null)
            return SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.ParenthesizedExpression(isPattern.WithoutTrivia())).WithTriviaFrom(isPattern);
        }

        private static IsPatternExpressionSyntax RewriteWorker(BinaryExpressionSyntax binary)
            => binary.Right.IsKind(SyntaxKind.NullLiteralExpression)
                ? Rewrite(binary, binary.Left, binary.Right)
                : Rewrite(binary, binary.Right, binary.Left);

        private static IsPatternExpressionSyntax Rewrite(
            BinaryExpressionSyntax binary, ExpressionSyntax expr, ExpressionSyntax nullLiteral)
        {
            var castExpr = (CastExpressionSyntax)expr;
            return SyntaxFactory.IsPatternExpression(
                castExpr.Expression.WithTriviaFrom(binary.Left),
                SyntaxFactory.Token(SyntaxKind.IsKeyword).WithTriviaFrom(binary.OperatorToken),
                SyntaxFactory.ConstantPattern(nullLiteral).WithTriviaFrom(binary.Right));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, title)
            {
            }
        }
    }
}
