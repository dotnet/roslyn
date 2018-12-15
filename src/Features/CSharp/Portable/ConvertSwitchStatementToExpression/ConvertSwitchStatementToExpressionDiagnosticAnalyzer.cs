// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConvertSwitchStatementToExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId,
                new LocalizableResourceString(nameof(CSharpFeaturesResources.Convert_switch_statement_to_expression), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_switch_expression), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Switch);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var switchOperation = (ISwitchOperation)context.Operation;
            if (switchOperation.Syntax.ContainsDiagnostics)
            {
                return;
            }

            if (!Analyzer.CanConvertToSwitchExpression(switchOperation))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                switchOperation.Syntax.GetFirstToken().GetLocation()));
        }

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
