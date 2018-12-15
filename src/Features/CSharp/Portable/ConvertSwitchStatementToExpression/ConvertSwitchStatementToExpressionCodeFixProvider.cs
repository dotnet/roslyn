// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal sealed class ConvertSwitchStatementToExpressionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan);
                editor.ReplaceNode(node, (currentStatement, _) =>
                {
                    var switchStatement = (SwitchStatementSyntax)currentStatement;
                    var switchSections = switchStatement.Sections;
                    var switchArms = switchSections.Select(
                        section => SwitchExpressionArm(GetPattern(section.Labels[0]), GetArmExpression(section.Statements)));
                    var switchExpression = SwitchExpression(switchStatement.Expression, SeparatedList(switchArms));
                    var finalStatement = GetFinalStatement(switchExpression, switchSections[0].Statements);
                    return finalStatement.WithAdditionalAnnotations(Formatter.Annotation);
                });
            }

            return Task.CompletedTask;
        }

        private StatementSyntax GetFinalStatement(SwitchExpressionSyntax switchExpression, SyntaxList<StatementSyntax> statements)
        {
            switch (statements.Count)
            {
                case 1:
                case 2:
                    switch (statements[0])
                    {
                        case ReturnStatementSyntax _:
                            return ReturnStatement(switchExpression);
                        case ExpressionStatementSyntax n:
                            return ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, ((AssignmentExpressionSyntax)n.Expression).Left, switchExpression));
                        case BlockSyntax n:
                            return GetFinalStatement(switchExpression, n.Statements);
                        case var value:
                            throw ExceptionUtilities.UnexpectedValue(value.Kind());
                    }

                default:
                    Debug.Assert(statements.Last() is BreakStatementSyntax);
                    return ExpressionStatement(
                        AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, 
                            TupleExpression(SeparatedList(statements.Remove(statements.Last()).Select(statement =>
                            {
                                var expressionStatement = (ExpressionStatementSyntax)statement;
                                var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
                                return Argument(assignment.Left);
                            }))), switchExpression));
            }
        }

        private static PatternSyntax GetPattern(SwitchLabelSyntax switchLabel)
        {
            switch (switchLabel)
            {
                case CasePatternSwitchLabelSyntax n:
                    return n.Pattern;
                case CaseSwitchLabelSyntax n:
                    return ConstantPattern(n.Value);
                case DefaultSwitchLabelSyntax n:
                    return DiscardPattern();
                case var value:
                    throw ExceptionUtilities.UnexpectedValue(value.Kind());
            }
        }

        private static ExpressionSyntax GetArmExpression(SyntaxList<StatementSyntax> statements)
        {
            Debug.Assert(statements.Count > 0);
            switch (statements.Count)
            {
                case 1:
                case 2:
                    switch (statements[0])
                    {
                        case ReturnStatementSyntax n:
                            return n.Expression;
                        case ExpressionStatementSyntax n:
                            return ((AssignmentExpressionSyntax)n.Expression).Right;
                        case BlockSyntax n:
                            return GetArmExpression(n.Statements);
                        case var value:
                            throw ExceptionUtilities.UnexpectedValue(value.Kind());
                    }
                default:
                    Debug.Assert(statements.Last() is BreakStatementSyntax);
                    return TupleExpression(SeparatedList(statements.Remove(statements.Last()).Select(statement =>
                    {
                        var expressionStatement = (ExpressionStatementSyntax)statement;
                        var assignment = (AssignmentExpressionSyntax)expressionStatement.Expression;
                        return Argument(assignment.Right);
                    })));
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Convert_switch_statement_to_expression, createChangedDocument)
            {
            }
        }
    }
}
