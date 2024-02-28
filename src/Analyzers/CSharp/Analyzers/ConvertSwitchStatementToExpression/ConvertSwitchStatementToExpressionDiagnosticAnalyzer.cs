// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.ConvertSwitchStatementToExpression;

using Constants = ConvertSwitchStatementToExpressionConstants;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class ConvertSwitchStatementToExpressionDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public ConvertSwitchStatementToExpressionDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.ConvertSwitchStatementToExpressionDiagnosticId,
            EnforceOnBuildValues.ConvertSwitchStatementToExpression,
            CSharpCodeStyleOptions.PreferSwitchExpression,
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Convert_switch_statement_to_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
            new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_switch_expression), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp8)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SwitchStatement);
        });

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var styleOption = context.GetCSharpAnalyzerOptions().PreferSwitchExpression;
        if (!styleOption.Value || ShouldSkipAnalysis(context, styleOption.Notification))
        {
            // User has disabled this feature.
            return;
        }

        var switchStatement = context.Node;
        if (switchStatement.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            return;
        }

        var (nodeToGenerate, declaratorToRemoveOpt) = Analyzer.Analyze(
            (SwitchStatementSyntax)switchStatement,
            context.SemanticModel,
            out var shouldRemoveNextStatement);
        if (nodeToGenerate == default)
        {
            return;
        }

        var additionalLocations = ArrayBuilder<Location>.GetInstance();
        additionalLocations.Add(switchStatement.GetLocation());
        additionalLocations.AddOptional(declaratorToRemoveOpt?.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.Create(Descriptor,
            // Report the diagnostic on the "switch" keyword.
            location: switchStatement.GetFirstToken().GetLocation(),
            notificationOption: styleOption.Notification,
            context.Options,
            additionalLocations: additionalLocations.ToArrayAndFree(),
            properties: ImmutableDictionary<string, string?>.Empty
                .Add(Constants.NodeToGenerateKey, ((int)nodeToGenerate).ToString(CultureInfo.InvariantCulture))
                .Add(Constants.ShouldRemoveNextStatementKey, shouldRemoveNextStatement.ToString(CultureInfo.InvariantCulture))));
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
}
