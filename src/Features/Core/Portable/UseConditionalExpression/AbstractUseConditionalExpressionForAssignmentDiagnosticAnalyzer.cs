// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<
        TSyntaxKind,
        TStatementSyntax,
        TIfStatementSyntax,
        TAssignmentStatementSyntax,
        TLocalDeclarationStatementSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TStatementSyntax : SyntaxNode
        where TIfStatementSyntax : TStatementSyntax
        where TAssignmentStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatementSyntax : TStatementSyntax
    {
        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() 
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_assignment), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Assignment_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract ImmutableArray<TSyntaxKind> GetIfStatementKinds();
        protected abstract (TStatementSyntax, TStatementSyntax) GetTrueFalseStatements(TIfStatementSyntax ifStatement);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetIfStatementKinds());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = (TIfStatementSyntax)context.Node;

            var language = ifStatement.Language;
            var syntaxTree = ifStatement.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferConditionalExpressionOverAssignment, language);
            if (!option.Value)
            {
                return;
            }

            var parentStatement = ifStatement.Parent as TStatementSyntax;
            if (parentStatement == null)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            var statements = syntaxFacts.GetExecutableBlockStatements(parentStatement);
            var ifIndex = statements.IndexOf(ifStatement);
            if (ifIndex <= 0)
            {
                return;
            }

            var localDeclarationStatement = statements[ifIndex - 1] as TLocalDeclarationStatementSyntax;
            if (localDeclarationStatement == null)
            {
                return;
            }

            var variables = syntaxFacts.GetVariablesOfLocalDeclarationStatement(localDeclarationStatement);
            if (variables.Count != 1)
            {
                return;
            }

            var variableIdentifier = syntaxFacts.GetIdentifierOfVariableDeclarator(variables[0]);

            var (trueStatement, falseStatement) = GetTrueFalseStatements(ifStatement);
            if (trueStatement == null || falseStatement == null)
            {
                return;
            }

            if (!syntaxFacts.IsSimpleAssignmentStatement(trueStatement) ||
                !syntaxFacts.IsSimpleAssignmentStatement(falseStatement))
            {
                return;
            }

            syntaxFacts.GetPartsOfAssignmentStatement(trueStatement, out var trueLeft, out var trueRight);
            syntaxFacts.GetPartsOfAssignmentStatement(falseStatement, out var falseLeft, out var falseRight);

            if (!syntaxFacts.IsIdentifierName(trueLeft) || 
                !syntaxFacts.IsIdentifierName(trueRight))
            {
                return;
            }


        }
    }
}
