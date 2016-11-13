// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.AddBraces
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAddBracesDiagnosticAnalyzer : 
        AbstractCodeStyleDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle =
            new LocalizableResourceString(nameof(FeaturesResources.Add_braces), FeaturesResources.ResourceManager,
                typeof(FeaturesResources));

        private static readonly LocalizableString s_localizableMessage =
            new LocalizableResourceString(nameof(WorkspacesResources.Add_braces_to_0_statement), WorkspacesResources.ResourceManager,
                typeof(WorkspacesResources));

        public CSharpAddBracesDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.AddBracesDiagnosticId,
                   s_localizableTitle, s_localizableMessage)
        {
        }

        public bool OpenFileOnly(Workspace workspace) => false;

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKindsOfInterest =
            ImmutableArray.Create(SyntaxKind.IfStatement,
                SyntaxKind.ElseClause,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.ForEachVariableStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.UsingStatement,
                SyntaxKind.LockStatement);


        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode, s_syntaxKindsOfInterest);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node;

            if (node.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax)node;
                if (AnalyzeIfStatement(ifStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        ifStatement.IfKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.IfKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.ElseClause))
            {
                var elseClause = (ElseClauseSyntax)node;
                if (AnalyzeElseClause(elseClause))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        elseClause.ElseKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ElseKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.ForStatement))
            {
                var forStatement = (ForStatementSyntax)node;
                if (AnalyzeForStatement(forStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        forStatement.ForKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.ForEachStatement) || node.IsKind(SyntaxKind.ForEachVariableStatement))
            {
                var forEachStatement = (CommonForEachStatementSyntax)node;
                if (AnalyzeForEachStatement(forEachStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        forEachStatement.ForEachKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.ForEachKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.WhileStatement))
            {
                var whileStatement = (WhileStatementSyntax)node;
                if (AnalyzeWhileStatement(whileStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        whileStatement.WhileKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.WhileKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.DoStatement))
            {
                var doStatement = (DoStatementSyntax)node;
                if (AnalyzeDoStatement(doStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        doStatement.DoKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.DoKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.UsingStatement))
            {
                var usingStatement = (UsingStatementSyntax)context.Node;
                if (AnalyzeUsingStatement(usingStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        usingStatement.UsingKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.UsingKeyword)));
                }
            }

            if (node.IsKind(SyntaxKind.LockStatement))
            {
                var lockStatement = (LockStatementSyntax)context.Node;
                if (AnalyzeLockStatement(lockStatement))
                {
                    context.ReportDiagnostic(Diagnostic.Create(HiddenDescriptor,
                        lockStatement.LockKeyword.GetLocation(), SyntaxFacts.GetText(SyntaxKind.LockKeyword)));
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

        private bool AnalyzeForEachStatement(CommonForEachStatementSyntax forEachStatement) =>
            !forEachStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeWhileStatement(WhileStatementSyntax whileStatement) =>
            !whileStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeDoStatement(DoStatementSyntax doStatement) =>
            !doStatement.Statement.IsKind(SyntaxKind.Block);

        private bool AnalyzeUsingStatement(UsingStatementSyntax usingStatement) =>
            !usingStatement.Statement.IsKind(SyntaxKind.Block) &&
            !usingStatement.Statement.IsKind(SyntaxKind.UsingStatement);

        private bool AnalyzeLockStatement(LockStatementSyntax lockStatement) =>
            !lockStatement.Statement.IsKind(SyntaxKind.Block) &&
            !lockStatement.Statement.IsKind(SyntaxKind.LockStatement);
    }
}