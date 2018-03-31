// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<
        TSyntaxKind>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
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
         
        // protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract ImmutableArray<TSyntaxKind> GetIfStatementKinds();
        // protected abstract (TStatementSyntax, TStatementSyntax) GetTrueFalseStatements(TIfStatementSyntax ifStatement);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetIfStatementKinds());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = context.Node;
            if (ifStatement == null)
            {
                return;
            }

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

            var ifOperation = (IConditionalOperation)context.SemanticModel.GetOperation(ifStatement);
            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    ifOperation, out _, out _, out _))
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(ifStatement.GetLocation());
            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptorWithSeverity(option.Notification.Value),
                ifStatement.GetFirstToken().GetLocation(),
                additionalLocations));
        }
    }
}
