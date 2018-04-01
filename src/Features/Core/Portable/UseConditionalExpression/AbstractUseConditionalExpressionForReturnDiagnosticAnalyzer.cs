// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer<TSyntaxKind>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() 
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionForReturnDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseConditionalExpressionForReturnDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_return_statement), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Return_statement_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }
         
        protected abstract ImmutableArray<TSyntaxKind> GetIfStatementKinds();

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

            var option = optionSet.GetOption(CodeStyleOptions.PreferConditionalExpressionOverReturn, language);
            if (!option.Value)
            {
                return;
            }

            var ifOperation = (IConditionalOperation)context.SemanticModel.GetOperation(ifStatement);
            if (!UseConditionalExpressionForReturnHelpers.TryMatchPattern(
                    ifOperation, out _, out _))
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
