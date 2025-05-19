// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitlyTypedLambdaExpression;

using static SyntaxFactory;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseImplicitlyTypedLambdaExpressionDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(IDEDiagnosticIds.UseImplicitlyTypedLambdaExpressionDiagnosticId,
        EnforceOnBuildValues.UseImplicitObjectCreation,
        CSharpCodeStyleOptions.PreferImplicitlyTypedLambdaExpression,
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_implicitly_typed_lambda), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Lambda_expression_can_be_simplified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeIfEnabled,
            SyntaxKind.ParenthesizedLambdaExpression);

    private void AnalyzeIfEnabled(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var analyzerOptions = context.Options;
        var semanticModel = context.SemanticModel;
        var option = analyzerOptions.GetCSharpAnalyzerOptions(semanticModel.SyntaxTree).PreferImplicitlyTypedLambdaExpression;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var explicitLambda = (ParenthesizedLambdaExpressionSyntax)context.Node;
        if (!Analyze(semanticModel, explicitLambda, cancellationToken))
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            explicitLambda.ParameterList.OpenParenToken.GetLocation(),
            option.Notification,
            context.Options,
            [explicitLambda.GetLocation()],
            properties: null));
    }

    public static bool Analyze(
        SemanticModel semanticModel,
        ParenthesizedLambdaExpressionSyntax explicitLambda,
        CancellationToken cancellationToken)
    {
        // If the lambda has an explicit return type, then do not offer the feature.  Explicit return types are used to
        // provide full semantic information to the compiler so it does not need to perform speculative lambda binding.
        // Removing may cause code compilation performance to regress.
        if (explicitLambda.ReturnType != null)
            return false;

        // Needs to have at least one parameter, all parameters need to have a provided type, and no parameters can have a
        // default value provided.
        if (explicitLambda.ParameterList.Parameters.Count == 0 ||
            explicitLambda.ParameterList.Parameters.Any(p => p.Type is null || p.Default != null))
        {
            return false;
        }

        // Prior to C# 14, implicitly typed lambdas can't have modifiers on parameters.
        var languageVersion = semanticModel.Compilation.LanguageVersion();
        if (!languageVersion.IsCSharp14OrAbove() && explicitLambda.ParameterList.Parameters.Any(p => p.Modifiers.Count > 0))
            return false;

        var implicitLambda = ConvertToImplicitlyTypedLambda(explicitLambda);

        var analyzer = new SpeculationAnalyzer(
            explicitLambda, implicitLambda, semanticModel, cancellationToken);
        if (analyzer.ReplacementChangesSemantics())
            return false;

        if (semanticModel.GetSymbolInfo(explicitLambda, cancellationToken).Symbol is not IMethodSymbol explicitLambdaMethod ||
            analyzer.SpeculativeSemanticModel.GetSymbolInfo(analyzer.ReplacedExpression, cancellationToken).Symbol is not IMethodSymbol implicitLambdaMethod)
        {
            return false;
        }

        if (!SignatureComparer.Instance.HaveSameSignature(explicitLambdaMethod, implicitLambdaMethod, caseSensitive: true))
            return false;

        return true;
    }

    public static LambdaExpressionSyntax ConvertToImplicitlyTypedLambda(ParenthesizedLambdaExpressionSyntax explicitLambda)
    {
        var implicitLambda = explicitLambda.ReplaceNodes(
            explicitLambda.ParameterList.Parameters,
            (parameter, _) => RemoveParamsModifier(
                parameter.WithType(null)
                         .WithIdentifier(parameter.Identifier.WithPrependedLeadingTrivia(parameter.Type!.GetLeadingTrivia()))));

        // If the lambda only has one parameter, then convert it to the non-parenthesized form.
        if (implicitLambda.ParameterList.Parameters is not ([{ AttributeLists.Count: 0, Modifiers.Count: 0 } parameter]))
            return implicitLambda;

        return SimpleLambdaExpression(
            explicitLambda.AttributeLists,
            explicitLambda.Modifiers,
            parameter.WithTriviaFrom(explicitLambda.ParameterList),
            explicitLambda.Block,
            explicitLambda.ExpressionBody);
    }

    private static ParameterSyntax RemoveParamsModifier(ParameterSyntax parameter)
    {
        // Implicitly typed lambdas aren't ever allowed to have the 'params' modifier.
        var paramsModifierIndex = parameter.Modifiers.IndexOf(SyntaxKind.ParamsKeyword);
        return paramsModifierIndex >= 0 ? parameter.WithModifiers(parameter.Modifiers.RemoveAt(paramsModifierIndex)) : parameter;
    }
}
