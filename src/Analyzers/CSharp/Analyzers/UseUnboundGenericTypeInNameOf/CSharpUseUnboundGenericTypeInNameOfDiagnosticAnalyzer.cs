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
        if (!invocation.IsNameOfInvocation() || invocation.ArgumentList.Arguments.Count != 1)
            return;

        foreach (var typeArgumentList in invocation.DescendantNodesAndSelf().OfType<TypeArgumentListSyntax>())
        {
            foreach (var argument in typeArgumentList.Arguments)
            {
                if (argument.Kind() != SyntaxKind.OmittedTypeArgument)
                {
                    if (!UnboundNameofBinds(syntaxContext.SemanticModel, invocation, cancellationToken))
                        return;

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

    /// <summary>
    /// Members reached through an unbound generic type only have what is known about its type parameters (the members
    /// of <see cref="object"/> and of their constraints).  For example, <c>nameof(Holder&lt;string&gt;.Value.Length)</c>
    /// can't become <c>nameof(Holder&lt;&gt;.Value.Length)</c> if <c>Value</c> is a <c>T</c>.  So only offer the fix if
    /// the rewritten <c>nameof</c> still binds.
    /// </summary>
    private static bool UnboundNameofBinds(
        SemanticModel semanticModel, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var speculationAnalyzer = new SpeculationAnalyzer(
            invocation, UseUnboundGenericTypeInNameOfHelpers.ConvertToUnboundGenericNameof(invocation), semanticModel, cancellationToken);
        var rewrittenInvocation = (InvocationExpressionSyntax)speculationAnalyzer.ReplacedExpression;
        var rewrittenArgument = rewrittenInvocation.ArgumentList.Arguments[0].Expression;
        return speculationAnalyzer.SpeculativeSemanticModel.GetSymbolInfo(rewrittenArgument, cancellationToken).GetAnySymbol() != null;
    }
}
