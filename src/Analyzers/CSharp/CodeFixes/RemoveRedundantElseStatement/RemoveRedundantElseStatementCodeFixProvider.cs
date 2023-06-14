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
using Microsoft.CodeAnalysis.CSharp.ConditionalExpressionInStringInterpolation;
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
    internal sealed class RemoveRedundantElseStatementCodeFixProvider : SyntaxEditorBasedCodeFixProvider
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

        protected override Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var ifStatements = diagnostics.Select(x => (IfStatementSyntax)x.AdditionalLocations[0].FindNode(cancellationToken)).ToSet();
            var blocks = ifStatements.Select(u => u.Parent);

            var root = editor.OriginalRoot;
            var updatedRoot = root.ReplaceNodes(
                blocks,
                (original, current) => current switch
                {
                    BlockSyntax block => RewriteBlock((BlockSyntax)original, block, ifStatements),
                    SwitchSectionSyntax switchSection => RewriteSwitchSection((SwitchSectionSyntax)original, switchSection, ifStatements),
                    //GlobalStatementSyntax global => RewriteGlobal(global, dic[original].Item1, dic[original].Item2),
                }
            );

            editor.ReplaceNode(root, updatedRoot);

            return Task.CompletedTask;
        }

        private static SyntaxNode RewriteBlock(BlockSyntax original, BlockSyntax current, ISet<IfStatementSyntax> ifStatements)
        {
            var statementToUpdateIndex = original.Statements.IndexOf(statement => ifStatements.Contains(statement));
            var statementToUpdate = (IfStatementSyntax)current.Statements[statementToUpdateIndex];

            var elseClause = GetLastElse(statementToUpdate);
            var ifWithoutElse = statementToUpdate.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newStatements = new[] { ifWithoutElse }.Concat(Expand(elseClause));

            var updatedStatements = current.Statements.ReplaceRange(statementToUpdate, newStatements);
            return current.WithStatements(updatedStatements);
        }

        private static SyntaxNode RewriteSwitchSection(SwitchSectionSyntax original, SwitchSectionSyntax current, ISet<IfStatementSyntax> ifStatements)
        {
            var statementToUpdateIndex = original.Statements.IndexOf(statement => ifStatements.Contains(statement));
            var statementToUpdate = (IfStatementSyntax)current.Statements[statementToUpdateIndex];

            var elseClause = GetLastElse(statementToUpdate);
            var ifWithoutElse = statementToUpdate.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newStatements = new[] { ifWithoutElse }.Concat(Expand(elseClause));

            var updatedStatements = current.Statements.ReplaceRange(statementToUpdate, newStatements);
            return current.WithStatements(updatedStatements);
        }

        private static SyntaxNode RewriteGlobal(GlobalStatementSyntax globalStatement, IfStatementSyntax topMostIf, ElseClauseSyntax elseClause)
        {
            var updatedTopMostIf = topMostIf.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var updatedGlobalStatement = globalStatement.ReplaceNode(topMostIf, updatedTopMostIf);

            var compilationUnit = (CompilationUnitSyntax?)globalStatement.Parent;

            if (compilationUnit is not null)
            {
                var memberToUpdateIndex = compilationUnit.Members.IndexOf(globalStatement);

                var updatedCompilationUnit = compilationUnit.ReplaceNode(globalStatement, updatedGlobalStatement);

                var updatedStatements = Expand(elseClause)
                    .Select(GlobalStatement);

                var newMembers = updatedCompilationUnit.Members
                    .InsertRange(memberToUpdateIndex + 1, updatedStatements);

                updatedCompilationUnit = updatedCompilationUnit.WithMembers(newMembers);

                return updatedCompilationUnit;
            }

            return compilationUnit;
        }

        private static ElseClauseSyntax GetLastElse(IfStatementSyntax ifStatement)
        {
            while (ifStatement.Else?.Statement is IfStatementSyntax elseIfStatement)
            {
                ifStatement = elseIfStatement;
            }

            return ifStatement.Else ?? throw new ArgumentException("Else can't be null", nameof(ifStatement));
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
