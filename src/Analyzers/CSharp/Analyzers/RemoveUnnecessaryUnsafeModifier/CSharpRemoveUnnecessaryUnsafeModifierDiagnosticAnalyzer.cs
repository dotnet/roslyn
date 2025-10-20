﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryUnsafeModifier;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpRemoveUnnecessaryUnsafeModifierDiagnosticAnalyzer()
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.RemoveUnnecessaryUnsafeModifier,
        EnforceOnBuildValues.RemoveUnnecessaryUnsafeModifier,
        option: null,
        fadingOption: null,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unnecessary_unsafe_modifier), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.unsafe_modifier_is_unnecessary), AnalyzersResources.ResourceManager, typeof(CompilerExtensionsResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

    private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        using var _ = ArrayBuilder<(MemberDeclarationSyntax memberDeclaration, SyntaxToken modifier)>.GetInstance(out var unnecessaryNodes);
        UnnecessaryUnsafeModifierUtilities.AddUnnecessaryNodes(context.SemanticModel, unnecessaryNodes, context.CancellationToken);

        foreach (var (declaration, modifier) in unnecessaryNodes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                modifier.GetLocation(),
                [declaration.GetLocation()]));
        }
    }
}
