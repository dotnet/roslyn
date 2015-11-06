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
using Microsoft.CodeAnalysis.Formatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InvokeDelegateWithConditionalAccess
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvokeDelegateWithConditionalAccessCodeFixProvider)), Shared]
    internal class InvokeDelegateWithConditionalAccessCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(IDEDiagnosticIds.InvokeDelegateWithConditionalAccessId);

        public override FixAllProvider GetFixAllProvider() => BatchFixAllProvider.Instance;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                CSharpFeaturesResources.DelegateInvocationCanBeSimplified,
                async c => await UpdateDocumentAsync(context).ConfigureAwait(false),
               equivalenceKey: nameof(InvokeDelegateWithConditionalAccessCodeFixProvider)),
               context.Diagnostics);
            return Task.FromResult(false);
        }

        private async Task<Document> UpdateDocumentAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            if (diagnostic.Properties[Constants.Kind] == Constants.VariableAndIfStatementForm)
            {
                return HandVariableAndIfStatementFormAsync(document, root, diagnostic);
            }
            else
            {
                Debug.Assert(diagnostic.Properties[Constants.Kind] == Constants.SingleIfStatementForm);
                return HandleSingleIfStatementForm(document, root, diagnostic);
            }
        }

        private Document HandleSingleIfStatementForm(Document document, SyntaxNode root, Diagnostic diagnostic)
        {
            var ifStatementLocation = diagnostic.AdditionalLocations[0];
            var expressionStatementLocation = diagnostic.AdditionalLocations[1];

            var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
            var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);
            var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;

            var newStatement = expressionStatement.WithExpression(
                SyntaxFactory.ConditionalAccessExpression(
                    invocationExpression.Expression,
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(Action.Invoke))), invocationExpression.ArgumentList)));
            newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(ifStatement, newStatement);
            return document.WithSyntaxRoot(newRoot);
        }

        private static Document HandVariableAndIfStatementFormAsync(Document document, SyntaxNode root, Diagnostic diagnostic)
        {
            var localDeclarationLocation = diagnostic.AdditionalLocations[0];
            var ifStatementLocation = diagnostic.AdditionalLocations[1];
            var expressionStatementLocation = diagnostic.AdditionalLocations[2];

            var localDeclarationStatement = (LocalDeclarationStatementSyntax)root.FindNode(localDeclarationLocation.SourceSpan);
            var ifStatement = (IfStatementSyntax)root.FindNode(ifStatementLocation.SourceSpan);
            var expressionStatement = (ExpressionStatementSyntax)root.FindNode(expressionStatementLocation.SourceSpan);

            var invocationExpression = (InvocationExpressionSyntax)expressionStatement.Expression;
            var parentBlock = (BlockSyntax)localDeclarationStatement.Parent;

            var newStatement = expressionStatement.WithExpression(
                SyntaxFactory.ConditionalAccessExpression(
                    localDeclarationStatement.Declaration.Variables[0].Initializer.Value.Parenthesize(),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(nameof(Action.Invoke))), invocationExpression.ArgumentList)));
            newStatement = newStatement.WithAdditionalAnnotations(Formatter.Annotation);

            var newStatements = parentBlock.Statements.TakeWhile(s => s != localDeclarationStatement)
                .Concat(newStatement)
                .Concat(parentBlock.Statements.SkipWhile(s => s != ifStatement).Skip(1));

            var newBlock = parentBlock.WithStatements(SyntaxFactory.List(newStatements));
            return document.WithSyntaxRoot(root.ReplaceNode(parentBlock, newBlock));
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
