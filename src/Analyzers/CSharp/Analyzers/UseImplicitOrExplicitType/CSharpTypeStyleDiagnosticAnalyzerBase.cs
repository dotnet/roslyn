// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.TypeStyle;

internal abstract partial class CSharpTypeStyleDiagnosticAnalyzerBase :
    AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    protected abstract CSharpTypeStyleHelper Helper { get; }

    protected CSharpTypeStyleDiagnosticAnalyzerBase(
        string diagnosticId, EnforceOnBuild enforceOnBuild, LocalizableString title, LocalizableString message)
        : base(diagnosticId,
               enforceOnBuild,
               [CSharpCodeStyleOptions.VarForBuiltInTypes, CSharpCodeStyleOptions.VarWhenTypeIsApparent, CSharpCodeStyleOptions.VarElsewhere],
               title, message)
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(
            HandleVariableDeclaration, SyntaxKind.VariableDeclaration, SyntaxKind.ForEachStatement, SyntaxKind.DeclarationExpression);

    private void HandleVariableDeclaration(SyntaxNodeAnalysisContext context)
    {
        var declarationStatement = context.Node;
        var cancellationToken = context.CancellationToken;

        var semanticModel = context.SemanticModel;
        var declaredType = Helper.FindAnalyzableType(declarationStatement, semanticModel, cancellationToken);
        if (declaredType == null)
        {
            return;
        }

        var simplifierOptions = context.GetCSharpAnalyzerOptions().GetSimplifierOptions();

        var typeStyle = Helper.AnalyzeTypeName(
            declaredType, semanticModel, simplifierOptions, cancellationToken);
        if (!typeStyle.IsStylePreferred
            || ShouldSkipAnalysis(context, typeStyle.Notification)
            || !typeStyle.CanConvert())
        {
            return;
        }

        // The severity preference is not Hidden, as indicated by IsStylePreferred.
        var descriptor = Descriptor;
        context.ReportDiagnostic(CreateDiagnostic(descriptor, declarationStatement, declaredType.StripRefIfNeeded().Span, typeStyle.Notification, context.Options));
    }

    private static Diagnostic CreateDiagnostic(DiagnosticDescriptor descriptor, SyntaxNode declaration, TextSpan diagnosticSpan, NotificationOption2 notificationOption, AnalyzerOptions analyzerOptions)
        => DiagnosticHelper.Create(descriptor, declaration.SyntaxTree.GetLocation(diagnosticSpan), notificationOption, analyzerOptions, additionalLocations: null, properties: null);
}
