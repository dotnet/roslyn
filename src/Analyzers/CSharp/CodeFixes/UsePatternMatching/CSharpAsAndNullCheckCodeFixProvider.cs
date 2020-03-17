﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpAsAndNullCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpAsAndNullCheckCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineAsTypeCheckId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            using var _1 = PooledHashSet<Location>.GetInstance(out var declaratorLocations);
            using var _2 = PooledHashSet<SyntaxNode>.GetInstance(out var statementParentScopes);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (declaratorLocations.Add(diagnostic.AdditionalLocations[0]))
                {
                    AddEdits(editor, diagnostic, RemoveStatement, cancellationToken);
                }
            }

            foreach (var parentScope in statementParentScopes)
            {
                editor.ReplaceNode(parentScope, (newParentScope, syntaxGenerator) =>
                {
                    var firstStatement = newParentScope is BlockSyntax
                        ? ((BlockSyntax)newParentScope).Statements.First()
                        : ((SwitchSectionSyntax)newParentScope).Statements.First();
                    return syntaxGenerator.ReplaceNode(newParentScope, firstStatement, firstStatement.WithoutLeadingBlankLinesInTrivia());
                });
            }

            return Task.CompletedTask;

            void RemoveStatement(StatementSyntax statement)
            {
                editor.RemoveNode(statement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                if (statement.Parent is BlockSyntax || statement.Parent is SwitchSectionSyntax)
                {
                    statementParentScopes.Add(statement.Parent);
                }
            }
        }

        private static void AddEdits(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            Action<StatementSyntax> removeStatement,
            CancellationToken cancellationToken)
        {
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var comparisonLocation = diagnostic.AdditionalLocations[1];
            var asExpressionLocation = diagnostic.AdditionalLocations[2];

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var comparison = (ExpressionSyntax)comparisonLocation.FindNode(cancellationToken);
            var asExpression = (BinaryExpressionSyntax)asExpressionLocation.FindNode(cancellationToken);

            var rightSideOfComparison = comparison is BinaryExpressionSyntax binaryExpression
                ? (SyntaxNode)binaryExpression.Right
                : ((IsPatternExpressionSyntax)comparison).Pattern;
            var newIdentifier = declarator.Identifier
                .WithoutTrivia().WithTrailingTrivia(rightSideOfComparison.GetTrailingTrivia());

            var declarationPattern = SyntaxFactory.DeclarationPattern(
                ((TypeSyntax)asExpression.Right).WithoutTrivia().WithTrailingTrivia(SyntaxFactory.ElasticMarker),
                SyntaxFactory.SingleVariableDesignation(newIdentifier));
            ExpressionSyntax isExpression = SyntaxFactory.IsPatternExpression(asExpression.Left, declarationPattern);

            // We should negate the is-expression if we have something like "x == null" or "x is null"
            if (comparison.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.IsPatternExpression))
            {
                isExpression = SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    isExpression.Parenthesize());
            }

            if (declarator.Parent is VariableDeclarationSyntax declaration &&
                declaration.Parent is LocalDeclarationStatementSyntax localDeclaration &&
                declaration.Variables.Count == 1)
            {
                // Trivia on the local declaration will move to the next statement.
                // use the callback form as the next statement may be the place where we're
                // inlining the declaration, and thus need to see the effects of that change.
                editor.ReplaceNode(
                    localDeclaration.GetNextStatement(),
                    (s, g) => s.WithPrependedNonIndentationTriviaFrom(localDeclaration));

                removeStatement(localDeclaration);
            }
            else
            {
                editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            }

            editor.ReplaceNode(comparison, isExpression.WithTriviaFrom(comparison));
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_pattern_matching, createChangedDocument)
            {
            }
        }
    }
}
