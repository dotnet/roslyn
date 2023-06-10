using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveRedundantElseStatement
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveRedundantElseStatement), Shared]
    internal class RemoveRedundantElseStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveRedundantElseStatementCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveRedundantElseStatementDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Remove_redundant_else_statement, nameof(CSharpAnalyzersResources.Remove_redundant_else_statement));
            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var root = editor.OriginalRoot;

                var topMostIf = (IfStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken);
                var elseClause = (ElseClauseSyntax)diagnostic.AdditionalLocations[1].FindNode(cancellationToken);
                var parent = topMostIf.Parent;

                var updatedRoot = parent switch
                {
                    BlockSyntax block => RewriteBlock(root, block, topMostIf, elseClause),
                    SwitchSectionSyntax switchSection => RewriteSwitchSection(root, switchSection, topMostIf, elseClause),
                    GlobalStatementSyntax global => RewriteGlobal(root, global, topMostIf, elseClause),
                    _ => throw new ArgumentException(nameof(topMostIf))
                };

                editor.ReplaceNode(root, updatedRoot);
            }

            return Task.CompletedTask;
        }

        private static SyntaxNode RewriteBlock(SyntaxNode root, BlockSyntax block, IfStatementSyntax topMostIf, ElseClauseSyntax elseClause)
        {
            var topMostIfIdx = block.Statements.IndexOf(topMostIf);

            var newTopMostIf = topMostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newBlock = block.ReplaceNode(topMostIf, newTopMostIf);
            var newStatements = newBlock.Statements.InsertRange(topMostIfIdx + 1, Expand(elseClause));

            newBlock = newBlock.WithStatements(newStatements);

            return root.ReplaceNode(block, newBlock);
        }

        private static SyntaxNode RewriteSwitchSection(SyntaxNode root, SwitchSectionSyntax switchSection, IfStatementSyntax topMostIf, ElseClauseSyntax elseClause)
        {
            var topMostIfIdx = switchSection.Statements.IndexOf(topMostIf);

            var newTopMostIf = topMostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newBlock = switchSection.ReplaceNode(topMostIf, newTopMostIf);
            var newStatements = newBlock.Statements.InsertRange(topMostIfIdx + 1, Expand(elseClause));

            newBlock = newBlock.WithStatements(newStatements);

            return root.ReplaceNode(switchSection, newBlock);
        }

        private static SyntaxNode RewriteGlobal(SyntaxNode root, GlobalStatementSyntax globalStatement, IfStatementSyntax topMostIf, ElseClauseSyntax elseClause)
        {
            var newTopMostIf = topMostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            globalStatement = globalStatement.ReplaceNode(topMostIf, newTopMostIf);

            var compilationUnit = (CompilationUnitSyntax?)globalStatement.Parent;

            if (compilationUnit is not null)
            {
                var memberToUpdateIndex = compilationUnit.Members.IndexOf(globalStatement);

                var updatedStatements = Expand(elseClause)
                    .Select(GlobalStatement);

                var newMembers = compilationUnit.Members
                    .RemoveAt(memberToUpdateIndex)
                    .InsertRange(memberToUpdateIndex, updatedStatements);

                var newCompilationUnit = compilationUnit.WithMembers(newMembers);
                return root.ReplaceNode(compilationUnit, newCompilationUnit);
            }

            return root;
        }

        private static ImmutableArray<StatementSyntax> Expand(ElseClauseSyntax elseClause)
        {
            using var _ = ArrayBuilder<StatementSyntax>.GetInstance(out var result);

            switch (elseClause.Statement)
            {
                case BlockSyntax block:
                    result.AddRange(block.Statements);
                    break;

                case StatementSyntax anythingElse:
                    result.Add(anythingElse);
                    break;
            }

            return result
                .Select(statement => statement.WithAdditionalAnnotations(Formatter.Annotation))
                .AsImmutable();
        }
    }
}
