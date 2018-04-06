// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer<
        TIfStatementSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TIfStatementSyntax : SyntaxNode
    {
        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() 
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionForAssignmentDiagnosticAnalyzer(
            LocalizableResourceString message)
            : base(IDEDiagnosticIds.UseConditionalExpressionForAssignmentDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_to_conditional_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   message)
        {
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var ifOperation = (IConditionalOperation)context.Operation;
            var ifStatement = ifOperation.Syntax as TIfStatementSyntax;
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

            if (!UseConditionalExpressionForAssignmentHelpers.TryMatchPattern(
                    GetSyntaxFactsService(), ifOperation, out _, out _))
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
