// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseImplicitlyTypedLambdaExpression;

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
        var syntaxTree = semanticModel.SyntaxTree;
        var option = analyzerOptions.GetCSharpAnalyzerOptions(syntaxTree).PreferImplicitlyTypedLambdaExpression;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var lambda = (ParenthesizedLambdaExpressionSyntax)context.Node;

        // If the lambda has an explicit return type, then do not offer the feature.  Explicit return types are used to
        // provide full semantic information to the compiler so it does not need to perform speculative lambda binding.
        // Removing may cause code compilation performance to regress.
        if (lambda.ReturnType != null)
            return;

        // Needs to have at least one parameter, and all parameters need to have a provided type.
        if (lambda.ParameterList.Parameters.Count == 0 ||
            lambda.ParameterList.Parameters.Any(p => p.Type is null))
        {
            return;
        }

        // Prior to C# 14, implicitly typed lambdas can't have modifiers on parameters.
        var languageVersion = semanticModel.Compilation.LanguageVersion();
        if (!languageVersion.IsCSharp14OrAbove() && lambda.ParameterList.Parameters.Any(p => p.Modifiers.Count > 0))
            return;

        var operation = semanticModel.GetOperation(lambda, cancellationToken);
        Console.WriteLine(operation);
    }
}
