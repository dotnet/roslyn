// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

/// <summary>
/// Base type for all analyzers that offer to update code to use a collection-expression.
/// </summary>
internal abstract class AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    protected new readonly DiagnosticDescriptor Descriptor;
    protected readonly DiagnosticDescriptor UnnecessaryCodeDescriptor;

    protected AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(string diagnosticId, EnforceOnBuild enforceOnBuild)
        : base(ImmutableDictionary<DiagnosticDescriptor, IOption2>.Empty
            // Ugly hack.  We need to create a descriptor to pass to our base *and* assign to one of our fields.
            // The conditional pattern form lets us do that.
            .Add(CreateDescriptor(diagnosticId, enforceOnBuild, isUnnecessary: false) is var descriptor ? descriptor : null, CodeStyleOptions2.PreferCollectionExpression)
            .Add(CreateDescriptor(diagnosticId, enforceOnBuild, isUnnecessary: true) is var unnecessaryCodeDescriptor ? unnecessaryCodeDescriptor : null, CodeStyleOptions2.PreferCollectionExpression))
    {
        Descriptor = descriptor;
        UnnecessaryCodeDescriptor = unnecessaryCodeDescriptor;
    }

    private static DiagnosticDescriptor CreateDescriptor(string diagnosticId, EnforceOnBuild enforceOnBuild, bool isUnnecessary)
        => CreateDescriptorWithId(
            diagnosticId,
            enforceOnBuild,
            new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            isUnnecessary: isUnnecessary);

    protected abstract void InitializeWorker(CompilationStartAnalysisContext context);

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            if (!context.Compilation.LanguageVersion().SupportsCollectionExpressions())
                return;

            InitializeWorker(context);
        });
}
