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
                    async c => await AddBracesAsync(context).ConfigureAwait(false)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected async Task<Document> AddBracesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var statement = root.FindNode(diagnosticSpan);

            SyntaxNode newBlock = null;

            var ifSyntax = statement as IfStatementSyntax;
            if (ifSyntax != null)
            {
                newBlock = GetNewBlock(statement, ifSyntax.Statement);
            }

            var elseSyntax = statement as ElseClauseSyntax;
            if (elseSyntax != null)
            {
                newBlock = GetNewBlock(statement, elseSyntax.Statement);
            }

            var forSyntax = statement as ForStatementSyntax;
            if (forSyntax != null)
            {
                newBlock = GetNewBlock(statement, forSyntax.Statement);
            }

            var forEachSyntax = statement as ForEachStatementSyntax;
            if (forEachSyntax != null)
            {
                newBlock = GetNewBlock(statement, forEachSyntax.Statement);
            }

            var whileSyntax = statement as WhileStatementSyntax;
            if (whileSyntax != null)
            {
                newBlock = GetNewBlock(statement, whileSyntax.Statement);
            }

            var doSyntax = statement as DoStatementSyntax;
            if (doSyntax != null)
            {
                newBlock = GetNewBlock(statement, doSyntax.Statement);
            }

            var usingSyntax = statement as UsingStatementSyntax;
            if (usingSyntax != null)
            {
                newBlock = GetNewBlock(statement, usingSyntax.Statement);
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