// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.MakeAnonymousFunctionStatic;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class MakeAnonymousFunctionStaticDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public MakeAnonymousFunctionStaticDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.MakeAnonymousFunctionStaticDiagnosticId,
               EnforceOnBuildValues.MakeAnonymousFunctionStatic,
               CSharpCodeStyleOptions.PreferStaticAnonymousFunction,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_anonymous_function_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Anonymous_function_can_be_made_static), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp9)
                return;

            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.AnonymousMethodExpression);
        });
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferStaticAnonymousFunction;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var anonymousFunction = (AnonymousFunctionExpressionSyntax)context.Node;
        if (anonymousFunction.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        if (TryGetCaptures(anonymousFunction, context.SemanticModel, out var captures) && captures.IsEmpty)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptor,
                    anonymousFunction.GetLocation()));
        }
    }

    private static bool TryGetCaptures(AnonymousFunctionExpressionSyntax anonymousFunction, SemanticModel semanticModel, out ImmutableArray<ISymbol> captures)
    {
        var dataFlow = semanticModel.AnalyzeDataFlow(anonymousFunction);
        if (dataFlow is null)
        {
            captures = default;
            return false;
        }

        captures = dataFlow.Captured;
        return dataFlow.Succeeded;
    }
}
