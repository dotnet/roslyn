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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvokeDelegateWithConditionalAccessCodeFixProvider)), Shared]
    internal partial class InvokeDelegateWithConditionalAccessCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId);

        public override FixAllProvider GetFixAllProvider()
            => new InvokeDelegateWithConditionalAccessFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
               context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(root, editor, diagnostic, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private void AddEdits(
            SyntaxNode root, SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            if (diagnostic.Properties[Constants.Kind] == Constants.VariableAndIfStatementForm)
            {
                HandleVariableAndIfStatementForm(root, editor, diagnostic, cancellationToken);
            }
            else
            {
                Debug.Assert(diagnostic.Properties[Constants.Kind] == Constants.SingleIfStatementForm);
                HandleSingleIfStatementForm(root, editor, diagnostic, cancellationToken);
            }
        }

        private void HandleSingleIfStatementForm(
            SyntaxNode root,
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var ifStatementLocation = diagnostic.AdditionalLocations[0];
            var expressionStatementLocation = diagnostic.AdditionalLocations[1];

            var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
            cancellationToken.ThrowIfCancellationRequested();

            var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);
            cancellationToken.ThrowIfCancellationRequested();

            var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
            cancellationToken.ThrowIfCancellationRequested();

            StatementSyntax newStatement = expressionStatement.WithExpression(
                SyntaxFactory.ConditionalAccessExpression(
                    invocationExpression.Expression,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(Action.Invoke))), invocationExpression.ArgumentList)));
            newStatement = newStatement.WithPrependedLeadingTrivia(ifStatement.GetLeadingTrivia());

            if (ifStatement.Parent.IsKind(SyntaxKind.ElseClause) && ifStatement.Statement.IsKind(SyntaxKind.Block))
            {
                newStatement = ((BlockSyntax)ifStatement.Statement).WithStatements(SyntaxFactory.SingletonList(newStatement));
            }

            newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);
            cancellationToken.ThrowIfCancellationRequested();

            editor.ReplaceNode(ifStatement, newStatement);
        }

        private static void HandleVariableAndIfStatementForm(
            SyntaxNode root, SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var localDeclarationLocation = diagnostic.AdditionalLocations[0];
            var ifStatementLocation = diagnostic.AdditionalLocations[1];
            var expressionStatementLocation = diagnostic.AdditionalLocations[2];

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)root.FindNode(localDeclarationLocation.SourceSpan);
            cancellationToken.ThrowIfCancellationRequested();

            var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
            cancellationToken.ThrowIfCancellationRequested();

            var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);
            cancellationToken.ThrowIfCancellationRequested();

            var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
            var parentBlock = (BlockSyntax)localDeclarationStatement.Parent;

            var newStatement = expressionStatement.WithExpression(
                SyntaxFactory.ConditionalAccessExpression(
                    localDeclarationStatement.Declaration.Variables[0].Initializer.Value.Parenthesize(),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(Action.Invoke))), invocationExpression.ArgumentList)));

            newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);

            editor.ReplaceNode(ifStatement, newStatement);
            editor.RemoveNode(localDeclarationStatement, SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.AddElasticMarker);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Delegate_invocation_can_be_simplified, createChangedDocument)
            {
            }
        }
    }
}