﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvokeDelegateWithConditionalAccessCodeFixProvider)), Shared]
    internal partial class InvokeDelegateWithConditionalAccessCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public InvokeDelegateWithConditionalAccessCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        // Filter out the diagnostics we created for the faded out code.  We don't want
        // to try to fix those as well as the normal diagnostics we created.
        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic)
            => !diagnostic.Properties.ContainsKey(WellKnownDiagnosticTags.Unnecessary);

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
            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(editor, diagnostic, cancellationToken);
            }

            return Task.CompletedTask;
        }

        private static void AddEdits(
            SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            if (diagnostic.Properties[Constants.Kind] == Constants.VariableAndIfStatementForm)
            {
                HandleVariableAndIfStatementForm(editor, diagnostic, cancellationToken);
            }
            else
            {
                Debug.Assert(diagnostic.Properties[Constants.Kind] == Constants.SingleIfStatementForm);
                HandleSingleIfStatementForm(editor, diagnostic, cancellationToken);
            }
        }

        private static void HandleSingleIfStatementForm(
            SyntaxEditor editor,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

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

            if (ifStatement.Parent.IsKind(SyntaxKind.ElseClause) &&
                ifStatement.Statement.IsKind(SyntaxKind.Block, out BlockSyntax block))
            {
                newStatement = block.WithStatements(SyntaxFactory.SingletonList(newStatement));
            }

            newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);
            cancellationToken.ThrowIfCancellationRequested();

            editor.ReplaceNode(ifStatement, newStatement);
        }

        private static void HandleVariableAndIfStatementForm(
            SyntaxEditor editor, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

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

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Delegate_invocation_can_be_simplified, createChangedDocument)
            {
            }
        }
    }
}
