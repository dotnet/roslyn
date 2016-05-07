using System.Collections.Immutable;
using Microsoft.CodeAnalysis.AddBraces;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer : AbstractAddBracesDiagnosticAnalyzer<SyntaxKind>
    {
        protected override ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get; } =
            ImmutableArray.Create(SyntaxKind.IfStatement,
                SyntaxKind.ElseClause,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.UsingStatement);

        public override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax) context.Node;
                if (AnalyzeIfStatement(ifStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        ifStatement.IfKeyword.GetLocation(), "if"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.ElseClause))
            {
                var elseClause = (ElseClauseSyntax)context.Node;
                if (AnalyzeElseClause(elseClause))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        elseClause.ElseKeyword.GetLocation(), "else"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)context.Node;
                if (AnalyzeForStatement(forStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        forStatement.ForKeyword.GetLocation(), "for"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.ForEachStatement))
            {
                var forEachStatement = (ForEachStatementSyntax)context.Node;
                if (AnalyzeForEachStatement(forEachStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        forEachStatement.ForEachKeyword.GetLocation(), "foreach"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.WhileStatement))
            {
                var whileStatement = (WhileStatementSyntax)context.Node;
                if (AnalyzeWhileStatement(whileStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        whileStatement.WhileKeyword.GetLocation(), "while"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.DoStatement))
            {
                var doStatement = (DoStatementSyntax)context.Node;
                if (AnalyzeDoStatement(doStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        doStatement.DoKeyword.GetLocation(), "do"));
                }
            }

            if (context.Node.IsKind(SyntaxKind.UsingStatement))
            {
                var usingStatement = (UsingStatementSyntax)context.Node;
                if (AnalyzeUsingStatement(usingStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0],
                        usingStatement.UsingKeyword.GetLocation(), "using"));
                }
            }
        }

        private bool AnalyzeIfStatement(IfStatementSyntax ifStatement) => 
            !ifStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeElseClause(ElseClauseSyntax elseClause) =>
                !elseClause.Statement.IsKind(SyntaxKind.Block) &&
                !elseClause.Statement.IsKind(SyntaxKind.IfStatement);

        private bool AnalyzeForStatement(ForStatementSyntax forStatement) =>
                !forStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeForEachStatement(ForEachStatementSyntax forEachStatement) =>
                !forEachStatement.Statement.IsKind(SyntaxKind.Block);
            
        private bool AnalyzeWhileStatement(WhileStatementSyntax whileStatement) =>
                !whileStatement.Statement.IsKind(SyntaxKind.Block);
            
        private bool AnalyzeDoStatement(DoStatementSyntax doStatement) =>
                !doStatement.Statement.IsKind(SyntaxKind.Block);
            
        private bool AnalyzeUsingStatement(UsingStatementSyntax usingStatement) =>
                !usingStatement.Statement.IsKind(SyntaxKind.Block) &&
                !usingStatement.Statement.IsKind(SyntaxKind.UsingStatement);
    }
}