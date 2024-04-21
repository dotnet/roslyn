// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeQuality;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.UseInterpolatedString;

internal abstract class AbstractUseInterpolatedStringDiagnosticAnalyzer<TSyntaxKind, TExpressionSyntax, TStringLiteralExpressionSyntax>
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TStringLiteralExpressionSyntax : TExpressionSyntax
{
    protected AbstractUseInterpolatedStringDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseInterpolatedStringDiagnosticId,
                EnforceOnBuildValues.UseInterpolatedString,
                CodeStyleOptions2.PreferInterpolatedString,
                new LocalizableResourceString(nameof(AnalyzersResources.Use_interpolated_string), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                new LocalizableResourceString(nameof(AnalyzersResources.Interpolated_string_should_be_used_for_performance), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    private static readonly LocalizableResourceString s_localizableUseInterpolatedString = new(
       nameof(AnalyzersResources.Use_interpolated_string), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static readonly LocalizableResourceString s_localizableStringCanBeConvertedToInterpolatedString = new(
       nameof(AnalyzersResources.Use_interpolated_string), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

    private static DiagnosticDescriptor CreateDescriptor()
        => CreateDescriptorWithId(
            IDEDiagnosticIds.UseInterpolatedStringDiagnosticId,
            EnforceOnBuildValues.UseInterpolatedString,
            hasAnyCodeStyleOption: true,
            s_localizableUseInterpolatedString,
            s_localizableStringCanBeConvertedToInterpolatedString);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected abstract ISyntaxFacts GetSyntaxFacts();

    protected sealed override void InitializeWorker(AnalysisContext context)
    {
        var syntaxFacts = GetSyntaxFacts();
        var syntaxKinds = syntaxFacts.SyntaxKinds;
        context.RegisterSyntaxNodeAction(
            AnalyzeSyntax, syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.StringLiteralExpression));
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var stringLiteralExpression = (TStringLiteralExpressionSyntax)context.Node;
        if (this.CanConvertToInterpolatedString(stringLiteralExpression))
        {
            var diagnostic = Diagnostic.Create(CreateDescriptor(), stringLiteralExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }

    protected abstract bool CanConvertToInterpolatedString(TStringLiteralExpressionSyntax stringLiteralExpression);
}
