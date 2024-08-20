// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseCollectionInitializer;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

/// <summary>
/// Base type for all analyzers that offer to update code to use a collection-expression.
/// </summary>
internal abstract class AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public static readonly ImmutableDictionary<string, string?> ChangesSemantics =
        ImmutableDictionary<string, string?>.Empty.Add(UseCollectionInitializerHelpers.ChangesSemanticsName, "");

    protected AbstractCSharpUseCollectionExpressionDiagnosticAnalyzer(string diagnosticId, EnforceOnBuild enforceOnBuild)
        : base(
            diagnosticId,
            enforceOnBuild,
            CodeStyleOptions2.PreferCollectionExpression,
            new LocalizableResourceString(nameof(AnalyzersResources.Simplify_collection_initialization), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
            new LocalizableResourceString(nameof(AnalyzersResources.Collection_initialization_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    protected abstract void InitializeWorker(CodeBlockStartAnalysisContext<SyntaxKind> context, INamedTypeSymbol? expressionType);

    protected virtual bool IsSupported(Compilation compilation)
        => true;

    public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected sealed override void InitializeWorker(AnalysisContext context)
        => context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            if (!compilation.LanguageVersion().SupportsCollectionExpressions())
                return;

            if (!IsSupported(compilation))
                return;

            // We wrap the SyntaxNodeAction within a CodeBlockStartAction, which allows us to get callbacks for object
            // creation expression nodes, but analyze nodes across the entire code block and eventually report fading
            // diagnostics with location outside this node. Without the containing CodeBlockStartAction, our reported
            // diagnostic would be classified as a non-local diagnostic and would not participate in lightbulb for
            // computing code fixes.
            var expressionType = compilation.ExpressionOfTType();
            context.RegisterCodeBlockStartAction<SyntaxKind>(context => InitializeWorker(context, expressionType));
        });
}
