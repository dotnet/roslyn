// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression
{
    using Constants = ConvertSwitchStatementToExpressionConstants;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public ConvertSwitchStatementToExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId,
                CSharpCodeStyleOptions.PreferSwitchExpression,
                LanguageNames.CSharp,
                new LocalizableResourceString(nameof(CSharpFeaturesResources.Convert_switch_statement_to_expression), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                new LocalizableResourceString(nameof(CSharpFeaturesResources.Use_switch_expression), CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SwitchStatement);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = context.Node;
            if (switchStatement.ContainsDirectives)
            {
                return;
            }

            var syntaxTree = switchStatement.SyntaxTree;

            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp8)
            {
                return;
            }

            var options = context.Options;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var styleOption = optionSet.GetOption(CSharpCodeStyleOptions.PreferSwitchExpression);
            if (!styleOption.Value)
            {
                // User has disabled this feature.
                return;
            }

            if (switchStatement.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                return;
            }

            var (nodeToGenerate, variableSymbolOpt, declaratorToRemoveOpt) =
                Analyzer.Analyze(
                    (SwitchStatementSyntax)switchStatement,
                    context.SemanticModel,
                    out var shouldRemoveNextStatement);
            if (nodeToGenerate == default)
            {
                return;
            }

            var additionalLocations = ArrayBuilder<Location>.GetInstance();
            additionalLocations.Add(switchStatement.GetLocation());
            additionalLocations.AddOptional(variableSymbolOpt?.Locations.FirstOrDefault());
            additionalLocations.AddOptional(declaratorToRemoveOpt?.GetLocation());

            context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor,
                // Report the diagnostic on the "switch" keyword.
                location: switchStatement.GetFirstToken().GetLocation(),
                effectiveSeverity: styleOption.Notification.Severity,
                additionalLocations: additionalLocations.ToArrayAndFree(),
                properties: ImmutableDictionary<string, string>.Empty
                    .Add(Constants.NodeToGenerateKey, ((int)nodeToGenerate).ToString(CultureInfo.InvariantCulture))
                    .Add(Constants.ShouldRemoveNextStatementKey, shouldRemoveNextStatement.ToString(CultureInfo.InvariantCulture))));
        }

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
