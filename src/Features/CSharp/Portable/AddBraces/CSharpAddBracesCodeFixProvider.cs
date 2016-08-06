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
                    FeaturesResources.Add_braces,
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

            var newRoot = root.ReplaceNode(statement, GetReplacementNode(statement));
            return context.Document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode GetReplacementNode(SyntaxNode statement)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    var ifSyntax = (IfStatementSyntax)statement;
                    return GetNewBlock(statement, ifSyntax.Statement);

                case SyntaxKind.ElseClause:
                    var elseClause = (ElseClauseSyntax)statement;
                    return GetNewBlock(statement, elseClause.Statement);

                case SyntaxKind.ForStatement:
                    var forSyntax = (ForStatementSyntax)statement;
                    return GetNewBlock(statement, forSyntax.Statement);

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachComponentStatement:
                    var forEachSyntax = (CommonForEachStatementSyntax)statement;
                    return GetNewBlock(statement, forEachSyntax.Statement);

                case SyntaxKind.WhileStatement:
                    var whileSyntax = (WhileStatementSyntax)statement;
                    return GetNewBlock(statement, whileSyntax.Statement);

                case SyntaxKind.DoStatement:
                    var doSyntax = (DoStatementSyntax)statement;
                    return GetNewBlock(statement, doSyntax.Statement);

                case SyntaxKind.UsingStatement:
                    var usingSyntax = (UsingStatementSyntax)statement;
                    return GetNewBlock(statement, usingSyntax.Statement);

                case SyntaxKind.LockStatement:
                    var lockSyntax = (LockStatementSyntax)statement;
                    return GetNewBlock(statement, lockSyntax.Statement);
            }

            return default(SyntaxNode);
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