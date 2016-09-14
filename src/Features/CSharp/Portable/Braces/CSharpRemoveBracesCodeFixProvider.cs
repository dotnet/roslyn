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

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.Braces
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

            var blockSyntaxNode = GetBlockSyntax(statement);
            var newNode = GetNewNode(statement, blockSyntaxNode).WithTrailingTrivia(blockSyntaxNode.GetTrailingTrivia());
            var newRoot = root.ReplaceNode(statement, newNode.WithAdditionalAnnotations(Formatter.Annotation));
            return context.Document.WithSyntaxRoot(newRoot);
        }

        private BlockSyntax GetBlockSyntax(SyntaxNode statement)
        {
            switch (statement.Kind())
            {
                case SyntaxKind.IfStatement:
                    var ifSyntax = (IfStatementSyntax)statement;
                    return (BlockSyntax)ifSyntax.Statement;

                case SyntaxKind.ElseClause:
                    var elseClause = (ElseClauseSyntax)statement;
                    return (BlockSyntax)elseClause.Statement;

                case SyntaxKind.ForStatement:
                    var forSyntax = (ForStatementSyntax)statement;
                    return (BlockSyntax)forSyntax.Statement;

                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachComponentStatement:
                    var forEachSyntax = (CommonForEachStatementSyntax)statement;
                    return (BlockSyntax)forEachSyntax.Statement;

                case SyntaxKind.WhileStatement:
                    var whileSyntax = (WhileStatementSyntax)statement;
                    return (BlockSyntax)whileSyntax.Statement;

                case SyntaxKind.DoStatement:
                    var doSyntax = (DoStatementSyntax)statement;
                    return (BlockSyntax)doSyntax.Statement;

                case SyntaxKind.UsingStatement:
                    var usingSyntax = (UsingStatementSyntax)statement;
                    return (BlockSyntax)usingSyntax.Statement;

                case SyntaxKind.LockStatement:
                    var lockSyntax = (LockStatementSyntax)statement;
                    return (BlockSyntax)lockSyntax.Statement;

                case SyntaxKind.FixedStatement:
                    var fixedSyntax = (FixedStatementSyntax)statement;
                    return (BlockSyntax)fixedSyntax.Statement;
            }

            return default(BlockSyntax);
        }

        private SyntaxNode GetNewNode(SyntaxNode statement, BlockSyntax block) =>
            statement.ReplaceNode(block, block.Statements.Single());

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Remove_braces, createChangedDocument)
            {
            }
        }
    }
}