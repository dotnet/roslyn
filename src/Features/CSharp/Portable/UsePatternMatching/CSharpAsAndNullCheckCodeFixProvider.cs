// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            return SpecializedTasks.EmptyTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, diagnostic, cancellationToken);
            }

            return SpecializedTasks.EmptyTask;
        }

        private static ExpressionSyntax GetCondition(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.WhileStatement:
                    return ((WhileStatementSyntax)node).Condition;
                case SyntaxKind.IfStatement:
                    return ((IfStatementSyntax)node).Condition;
                case SyntaxKind.ReturnStatement:
                    return ((ReturnStatementSyntax)node).Expression;
                case SyntaxKind.LocalDeclarationStatement:
                    return ((LocalDeclarationStatementSyntax)node).Declaration.Variables[0].Initializer.Value;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        private static void AddEdits(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var localDeclarationLocation = diagnostic.AdditionalLocations[0];
            var targetStatementLocation = diagnostic.AdditionalLocations[1];
            var conditionLocation = diagnostic.AdditionalLocations[2];
            var asExpressionLocation = diagnostic.AdditionalLocations[3];

            var localDeclaration = (LocalDeclarationStatementSyntax)localDeclarationLocation.FindNode(cancellationToken);
            var targetStatement = (StatementSyntax)targetStatementLocation.FindNode(cancellationToken);
            var conditionPart = (BinaryExpressionSyntax)conditionLocation.FindNode(cancellationToken);
            var asExpression = (BinaryExpressionSyntax)asExpressionLocation.FindNode(cancellationToken);

            var updatedConditionPart = SyntaxFactory.IsPatternExpression(
                asExpression.Left, SyntaxFactory.DeclarationPattern(
                    ((TypeSyntax)asExpression.Right).WithoutTrivia(),
                    SyntaxFactory.SingleVariableDesignation(
                        localDeclaration.Declaration.Variables[0].Identifier.WithoutTrivia())));

            var currentCondition = GetCondition(targetStatement);
            var updatedCondition = currentCondition.ReplaceNode(conditionPart, updatedConditionPart);

            var block = (BlockSyntax)localDeclaration.Parent;
            var declarationIndex = block.Statements.IndexOf(localDeclaration);

            // Trivia on the local declaration will move to the next statement.
            // use the callback form as the next statement may be the place where we're
            // inlining the declaration, and thus need to see the effects of that change.
            editor.ReplaceNode(
                block.Statements[declarationIndex + 1],
                (s, g) => s.WithPrependedNonIndentationTriviaFrom(localDeclaration));
            editor.RemoveNode(localDeclaration, SyntaxRemoveOptions.KeepUnbalancedDirectives);

            editor.ReplaceNode(targetStatement, (currentStatement, g) =>
            {
                var updatedStatement = currentStatement.ReplaceNode(GetCondition(currentStatement), updatedCondition);
                return updatedStatement.WithAdditionalAnnotations(Formatter.Annotation);
            });
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Use_pattern_matching, createChangedDocument)
            {
            }
        }
    }
}
