using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Data;
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
            var ifStatements = diagnostics.Select(diagnostic => diagnostic.AdditionalLocations[0].FindNode(cancellationToken)).ToSet();
            var nodesToUpdate = ifStatements.Select(statement => statement.Parent!);

            var root = editor.OriginalRoot;

            var updatedRoot = root.ReplaceNodes(
                nodesToUpdate,
                (original, current) => ComputeReplacementNode(original, current, ifStatements)
            );

            editor.ReplaceNode(root, updatedRoot);

            return Task.CompletedTask;
        }

        public static SyntaxNode ComputeReplacementNode(SyntaxNode original, SyntaxNode current, ISet<SyntaxNode> ifStatements)
        {
            return original switch
            {
                CompilationUnitSyntax compilationUnit => RewriteGlobalStatement(compilationUnit, (CompilationUnitSyntax)current, ifStatements),
                _ => RewriteNode(original, current, ifStatements),
            };
        }

        private static CompilationUnitSyntax RewriteGlobalStatement(CompilationUnitSyntax original, CompilationUnitSyntax current, ISet<SyntaxNode> ifStatements)
        {
            var memberToUpdateIndex = original.Members.IndexOf(ifStatements.Contains);
            var memberToUpdate = (GlobalStatementSyntax)original.Members[memberToUpdateIndex];

            var elseClause = GetLastElse((IfStatementSyntax)memberToUpdate.Statement);
            var ifWithoutElse = memberToUpdate.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);

            var updatedCompilationUnit = current.ReplaceNode(memberToUpdate, ifWithoutElse!);
            var newStatements = Expand(elseClause).Select(GlobalStatement);

            var updatedMembers = updatedCompilationUnit.Members
                .InsertRange(memberToUpdateIndex + 1, newStatements);

            return current.WithMembers(updatedMembers);
        }

        private static SyntaxNode RewriteNode(SyntaxNode original, SyntaxNode current, ISet<SyntaxNode> ifStatements)
        {
            var originalNodeStatements = GetStatements(original);
            var currentNodeStatements = GetStatements(current);

            var statementToUpdateIndex = originalNodeStatements.IndexOf(ifStatements.Contains);
            var statementToUpdate = currentNodeStatements[statementToUpdateIndex];

            var elseClause = GetLastElse((IfStatementSyntax)statementToUpdate);
            var ifWithoutElse = statementToUpdate.RemoveNode(elseClause, SyntaxRemoveOptions.KeepEndOfLine);
            var newStatements = new[] { ifWithoutElse }.Concat(Expand(elseClause));

            var updatedStatements = currentNodeStatements.ReplaceRange(statementToUpdate, newStatements!);

            return current switch
            {
                BlockSyntax block => block.WithStatements(updatedStatements),
                SwitchSectionSyntax switchSelection => switchSelection.WithStatements(updatedStatements),
                _ => throw new ArgumentException($"Unsupported node type: {current.GetType()}", nameof(current)),
            };
        }

        private static SyntaxList<StatementSyntax> GetStatements(SyntaxNode node)
        {
            return node switch
            {
                BlockSyntax block => block.Statements,
                SwitchSectionSyntax switchSelection => switchSelection.Statements,
                _ => throw new ArgumentException($"Unsupported node type: {node.GetType()}", nameof(node)),
            };
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
