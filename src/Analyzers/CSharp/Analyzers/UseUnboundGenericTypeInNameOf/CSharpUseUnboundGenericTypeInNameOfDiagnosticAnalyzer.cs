// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.UseUnboundGenericTypeInNameOf;

/// <summary>
/// Looks for code of the form:
/// 
/// <code>
///     nameof(List&lt;...&gt;)
/// </code>
///
/// and converts it to:
/// 
/// <code>
///     nameof(List&lt;&gt;)
/// </code>
/// 
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseUnboundGenericTypeInNameOfDiagnosticAnalyzer()
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.UseUnboundGenericTypeInNameOfDiagnosticId,
        EnforceOnBuildValues.UseUnboundGenericTypeInNameOf,
        CSharpCodeStyleOptions.PreferUnboundGenericTypeInNameOf,
        new LocalizableResourceString(
            nameof(CSharpAnalyzersResources.Use_unbound_generic_type), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            // Tuples are only available in C# 14 and above.
            var compilation = context.Compilation;
            if (!compilation.LanguageVersion().IsCSharp14OrAbove())
                return;

            context.RegisterSyntaxNodeAction(
                AnalyzeInvocationExpression,
                SyntaxKind.InvocationExpression);
        });
    }

    private void AnalyzeInvocationExpression(SyntaxNodeAnalysisContext syntaxContext)
    {
        var cancellationToken = syntaxContext.CancellationToken;
        var styleOption = syntaxContext.GetCSharpAnalyzerOptions().PreferUnboundGenericTypeInNameOf;
        if (!styleOption.Value || ShouldSkipAnalysis(syntaxContext, styleOption.Notification))
            return;

        var invocation = (InvocationExpressionSyntax)syntaxContext.Node;
        if (!invocation.IsNameOfInvocation())
            return;

        foreach (var typeArgumentList in invocation.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>())
        {
            foreach (var argument in typeArgumentList.Arguments)
            {
                if (argument.Kind() != SyntaxKind.OmittedTypeArgument)
                {
                    syntaxContext.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                        Descriptor,
                        invocation.GetFirstToken().GetLocation(),
                        styleOption.Notification,
                        syntaxContext.Options,
                        [invocation.GetLocation()],
                        additionalUnnecessaryLocations: [invocation.SyntaxTree.GetLocation(
                            TextSpan.FromBounds(typeArgumentList.LessThanToken.Span.End, typeArgumentList.GreaterThanToken.Span.Start))]));

                    return;
                }
            }
        }
    }
}
