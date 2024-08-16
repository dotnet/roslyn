// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBodyForLambda;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class UseExpressionBodyForLambdaDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor s_useExpressionBodyForLambda = CreateDescriptorWithId(UseExpressionBodyForLambdaHelpers.UseExpressionBodyTitle, UseExpressionBodyForLambdaHelpers.UseExpressionBodyTitle);
    private static readonly DiagnosticDescriptor s_useBlockBodyForLambda = CreateDescriptorWithId(UseExpressionBodyForLambdaHelpers.UseBlockBodyTitle, UseExpressionBodyForLambdaHelpers.UseBlockBodyTitle);

    public UseExpressionBodyForLambdaDiagnosticAnalyzer() : base(
        [
            (s_useExpressionBodyForLambda, CSharpCodeStyleOptions.PreferExpressionBodiedLambdas),
            (s_useBlockBodyForLambda, CSharpCodeStyleOptions.PreferExpressionBodiedLambdas)
        ])
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeIfEnabled,
            SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression);

    private void AnalyzeIfEnabled(SyntaxNodeAnalysisContext context)
    {
        var analyzerOptions = context.Options;
        var syntaxTree = context.SemanticModel.SyntaxTree;
        var optionValue = UseExpressionBodyForLambdaHelpers.GetCodeStyleOption(analyzerOptions.GetAnalyzerOptions(syntaxTree));
        if (ShouldSkipAnalysis(context, optionValue.Notification))
            return;

        var severity = UseExpressionBodyForLambdaHelpers.GetOptionSeverity(optionValue);
        switch (severity)
        {
            case ReportDiagnostic.Error:
            case ReportDiagnostic.Warn:
            case ReportDiagnostic.Info:
                break;
            default:
                // don't analyze if it's any other value.
                return;
        }

        AnalyzeSyntax(context, optionValue);
    }

    private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context, CodeStyleOption2<ExpressionBodyPreference> option)
    {
        var declaration = (LambdaExpressionSyntax)context.Node;
        var diagnostic = AnalyzeSyntax(context.SemanticModel, option, declaration, context.Options, context.CancellationToken);
        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static Diagnostic? AnalyzeSyntax(
        SemanticModel semanticModel, CodeStyleOption2<ExpressionBodyPreference> option,
        LambdaExpressionSyntax declaration, AnalyzerOptions analyzerOptions, CancellationToken cancellationToken)
    {
        if (UseExpressionBodyForLambdaHelpers.CanOfferUseExpressionBody(option.Value, declaration, declaration.GetLanguageVersion(), cancellationToken))
        {
            var location = GetDiagnosticLocation(declaration);

            var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
            var properties = ImmutableDictionary<string, string?>.Empty;
            return DiagnosticHelper.Create(
                s_useExpressionBodyForLambda,
                location, option.Notification,
                analyzerOptions, additionalLocations, properties);
        }

        if (UseExpressionBodyForLambdaHelpers.CanOfferUseBlockBody(semanticModel, option.Value, declaration, cancellationToken))
        {
            // They have an expression body.  Create a diagnostic to convert it to a block
            // if they don't want expression bodies for this member.  
            var location = GetDiagnosticLocation(declaration);

            var properties = ImmutableDictionary<string, string?>.Empty;
            var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
            return DiagnosticHelper.Create(
                s_useBlockBodyForLambda,
                location, option.Notification,
                analyzerOptions, additionalLocations, properties);
        }

        return null;
    }

    private static Location GetDiagnosticLocation(LambdaExpressionSyntax declaration)
        => Location.Create(declaration.SyntaxTree,
                TextSpan.FromBounds(declaration.SpanStart, declaration.ArrowToken.Span.End));

    private static DiagnosticDescriptor CreateDescriptorWithId(
        LocalizableString title, LocalizableString message)
    {
        return CreateDescriptorWithId(IDEDiagnosticIds.UseExpressionBodyForLambdaExpressionsDiagnosticId, EnforceOnBuildValues.UseExpressionBodyForLambdaExpressions, hasAnyCodeStyleOption: true, title, message);
    }
}
