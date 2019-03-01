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
            var firstTracker = new FirstTracker<StatementSyntax>(
                s => s.GetNextStatement(),
                s => s.IsFirstStatementInEnclosingBlock() || s.IsFirstStatementInSwitchSection());
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (declaratorLocations.Add(diagnostic.AdditionalLocations[0]))
                {
                    AddEdits(editor, diagnostic, cancellationToken, (s, opt) =>
                    {
                        editor.RemoveNode(s, opt);
                        firstTracker.Remove(s);
                    });
                }
            }

            foreach (var firstStatement in firstTracker.Firsts)
            {
                editor.ReplaceNode(firstStatement, (fs, gen) =>
                    fs.WithLeadingTrivia(fs.GetLeadingTrivia().SkipInitialWhiteLines()));
            }

            return Task.CompletedTask;
        }

        private static void AddEdits(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken,
            Action<StatementSyntax,SyntaxRemoveOptions> removeStatement)
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

                removeStatement(localDeclaration, SyntaxRemoveOptions.KeepUnbalancedDirectives);
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

        private class FirstTracker<T> where T : class
        {
            private readonly Func<T, T> _getNext;
            private readonly Func<T, bool> _isFirst;
            private readonly Collection<T> _removed;
            public ICollection<T> Firsts { get; }

            public FirstTracker(Func<T,T> getNext, Func<T,bool> isFirst)
            {
                _getNext = getNext;
                _isFirst = isFirst;
                _removed = new Collection<T>();
                Firsts = new Collection<T>();
            }

            private void MakeNextFirst(T item)
            {
                var next = item;
                do
                {
                    next = _getNext(next);
                } while (_removed.Contains(next));

                if (next != null)
                {
                    Firsts.Add(next);
                }
            }

            public void Remove(T item)
            {
                if (_isFirst(item) || Firsts.Remove(item))
                {
                    MakeNextFirst(item);
                }

                _removed.Add(item);
            }
        }
    }
}
