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
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveBraces), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddAwait)]
    internal class CSharpRemoveBracesCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(IDEDiagnosticIds.RemoveBracesDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    FeaturesResources.Remove_braces,
                    c => RemoveBracesAsync(context, c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected async Task<Document> RemoveBracesAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var statement = root.FindNode(diagnosticSpan);

            var newRoot = root.ReplaceNode(statement, GetReplacementNode(statement).WithAdditionalAnnotations(Formatter.Annotation));
            return context.Document.WithSyntaxRoot(newRoot);
        }

        private SyntaxNode GetReplacementNode(SyntaxNode statement)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    var ifSyntax = (IfStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)ifSyntax.Statement).WithTrailingTrivia(ifSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.ElseClause:
                    var elseClause = (ElseClauseSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)elseClause.Statement).WithTrailingTrivia(elseClause.Statement.GetTrailingTrivia());

                case SyntaxKind.ForStatement:
                    var forSyntax = (ForStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)forSyntax.Statement).WithTrailingTrivia(forSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachComponentStatement:
                    var forEachSyntax = (CommonForEachStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)forEachSyntax.Statement).WithTrailingTrivia(forEachSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.WhileStatement:
                    var whileSyntax = (WhileStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)whileSyntax.Statement).WithTrailingTrivia(whileSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.DoStatement:
                    var doSyntax = (DoStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)doSyntax.Statement).WithTrailingTrivia(doSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.UsingStatement:
                    var usingSyntax = (UsingStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)usingSyntax.Statement).WithTrailingTrivia(usingSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.LockStatement:
                    var lockSyntax = (LockStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)lockSyntax.Statement).WithTrailingTrivia(lockSyntax.Statement.GetTrailingTrivia());

                case SyntaxKind.FixedStatement:
                    var fixedSyntax = (FixedStatementSyntax)statement;
                    return GetNewNode(statement, (BlockSyntax)fixedSyntax.Statement).WithTrailingTrivia(fixedSyntax.Statement.GetTrailingTrivia());
            }

            return default(SyntaxNode);
        }

        private SyntaxNode GetNewNode(SyntaxNode statement, BlockSyntax block) =>
            statement.ReplaceNode(block, block.Statements.Single());

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}