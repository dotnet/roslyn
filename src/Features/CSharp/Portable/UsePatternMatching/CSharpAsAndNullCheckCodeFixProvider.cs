// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternMatching
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpAsAndNullCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineAsTypeCheckId);

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
            var declaratorLocations = new HashSet<Location>();
            var firstStatementTracker = new FirstStatementTracker();
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (declaratorLocations.Add(diagnostic.AdditionalLocations[0]))
                {
                    AddEdits(editor, diagnostic, firstStatementTracker, cancellationToken);
                }
            }

            foreach (var firstStatement in firstStatementTracker.FirstStatements)
            {
                //each statement that is now at the top of its block or switch section
                //should have no blank lines preceding it
                editor.ReplaceNode(firstStatement, (fs, gen) =>
                    fs.WithLeadingTrivia(fs.GetLeadingTrivia().WithoutLeadingBlankLines()));
            }

            return Task.CompletedTask;
        }

        private static void AddEdits(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            FirstStatementTracker firstStatementTracker,
            CancellationToken cancellationToken)
        {
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var comparisonLocation = diagnostic.AdditionalLocations[1];
            var asExpressionLocation = diagnostic.AdditionalLocations[2];

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var comparison = (BinaryExpressionSyntax)comparisonLocation.FindNode(cancellationToken);
            var asExpression = (BinaryExpressionSyntax)asExpressionLocation.FindNode(cancellationToken);
            var newIdentifier = declarator.Identifier
                .WithoutTrivia().WithTrailingTrivia(comparison.Right.GetTrailingTrivia());

            ExpressionSyntax isExpression = SyntaxFactory.IsPatternExpression(
                asExpression.Left, SyntaxFactory.DeclarationPattern(
                    ((TypeSyntax)asExpression.Right).WithoutTrivia(),
                    SyntaxFactory.SingleVariableDesignation(newIdentifier)));

            // We should negate the is-expression if we have something like "x == null"
            if (comparison.IsKind(SyntaxKind.EqualsExpression))
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

                editor.RemoveNode(localDeclaration, SyntaxRemoveOptions.KeepUnbalancedDirectives);
                firstStatementTracker.RemoveStatement(localDeclaration);
            }
            else
            {
                editor.RemoveNode(declarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            }

            editor.ReplaceNode(comparison, isExpression.WithTriviaFrom(comparison));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_pattern_matching, createChangedDocument)
            {
            }
        }

        private class FirstStatementTracker
        {
            private HashSet<StatementSyntax> _removed = new HashSet<StatementSyntax>();
            public HashSet<StatementSyntax> FirstStatements { get; } = new HashSet<StatementSyntax>();

            private bool WasFirst(StatementSyntax statementToRemove)
            {
                if (statementToRemove.IsFirstStatementInEnclosingBlock() || statementToRemove.IsFirstStatementInSwitchSection())
                {
                    return true;
                }

                bool wasMadeFirstByRemovalOfPredecessor = FirstStatements.Remove(statementToRemove);
                if (wasMadeFirstByRemovalOfPredecessor)
                {
                    return true;
                }

                return false;
            }

            private bool TryFindFirstUnremovedSuccessor(StatementSyntax statement, out StatementSyntax result)
            {
                result = statement;
                do
                {
                    result = result.GetNextStatement();
                } while (_removed.Contains(result));

                return result != null;
            }

            private void MakeFirstUnremovedSuccessorFirst(StatementSyntax statement)
            {
                if (TryFindFirstUnremovedSuccessor(statement, out var firstUnremovedSuccessor))
                {
                    FirstStatements.Add(firstUnremovedSuccessor);
                }
            }

            public void RemoveStatement(StatementSyntax statement)
            {
                if (WasFirst(statement))
                {
                    MakeFirstUnremovedSuccessorFirst(statement);
                }

                _removed.Add(statement);
            }
        }
    }
}
