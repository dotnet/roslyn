// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using Constants = ConvertSwitchStatementToExpressionConstants;

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
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SwitchStatement);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = context.Node;
            if (switchStatement.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            var analysisResult = Analyzer.Analyze(switchStatement, out var shouldRemoveNextStatement);
            if (!analysisResult.Success)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Descriptor,
                // Report the diagnostic on the "switch" keyword.
                location: switchStatement.GetFirstToken().GetLocation(),
                additionalLocations: new[] { switchStatement.GetLocation() },
                properties: ImmutableDictionary<string, string>.Empty
                    .Add(Constants.NodeToGenerateKey, ((int)analysisResult.GetSyntaxKind()).ToString())
                    .Add(Constants.ShouldRemoveNextStatementKey, shouldRemoveNextStatement.ToString())));
        }

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxAnalysis;
    }
}
