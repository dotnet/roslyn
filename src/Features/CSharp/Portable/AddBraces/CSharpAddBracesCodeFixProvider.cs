using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddBraces), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddAwait)]
    internal class CSharpAddBracesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.AddBracesDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.AddBraces,
                    c => AddBracesAsync(context, c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected async Task<Document> AddBracesAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var statement = root.FindNode(diagnosticSpan);

            SyntaxNode newBlock = null;

            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    var ifSyntax = (IfStatementSyntax) statement;
                    newBlock = GetNewBlock(statement, ifSyntax.Statement);
                    break;

                case SyntaxKind.ElseClause:
                    var elseClause = (ElseClauseSyntax)statement;
                    newBlock = GetNewBlock(statement, elseClause.Statement);
                    break;

                case SyntaxKind.ForStatement:
                    var forSyntax = (ForStatementSyntax)statement;
                    newBlock = GetNewBlock(statement, forSyntax.Statement);
                    break;

                case SyntaxKind.ForEachStatement:
                    var forEachSyntax = (ForEachStatementSyntax)statement;
                    newBlock = GetNewBlock(statement, forEachSyntax.Statement);
                    break;

                case SyntaxKind.WhileStatement:
                    var whileSyntax = (WhileStatementSyntax)statement;
                    newBlock = GetNewBlock(statement, whileSyntax.Statement);
                    break;

                case SyntaxKind.DoStatement:
                    var doSyntax = (DoStatementSyntax)statement;
                    newBlock = GetNewBlock(statement, doSyntax.Statement);
                    break;

                case SyntaxKind.UsingStatement:
                    var usingSyntax = (UsingStatementSyntax)statement;
                    newBlock = GetNewBlock(statement, usingSyntax.Statement);
                    break;
            }

            var newRoot = root.ReplaceNode(statement, newBlock);
            return context.Document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode GetNewBlock(SyntaxNode statement, StatementSyntax statementBody) =>
            statement.ReplaceNode(statementBody, SyntaxFactory.Block(statementBody));

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}