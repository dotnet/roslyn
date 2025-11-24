// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryCast;

internal abstract class AbstractRemoveUnnecessaryCastDiagnosticAnalyzer<
    TLanguageKindEnum,
    TCastExpression> : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer
    where TLanguageKindEnum : struct
    where TCastExpression : SyntaxNode
{
    protected AbstractRemoveUnnecessaryCastDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId,
               EnforceOnBuildValues.RemoveUnnecessaryCast,
               option: null,
               fadingOption: null,
               new LocalizableResourceString(nameof(AnalyzersResources.Remove_Unnecessary_Cast), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               new LocalizableResourceString(nameof(CompilerExtensionsResources.Cast_is_redundant), CompilerExtensionsResources.ResourceManager, typeof(CompilerExtensionsResources)))
    {
    }

    protected abstract ImmutableArray<TLanguageKindEnum> SyntaxKindsOfInterest { get; }
    protected abstract TextSpan GetFadeSpan(TCastExpression node);
    protected abstract bool IsUnnecessaryCast(SemanticModel model, TCastExpression node, CancellationToken cancellationToken);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKindsOfInterest);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        var diagnostic = TryRemoveCastExpression(
            context.SemanticModel,
            (TCastExpression)context.Node,
            context.CancellationToken);

        if (diagnostic != null)
        {
            context.ReportDiagnostic(diagnostic);
        }
    }

    private Diagnostic? TryRemoveCastExpression(SemanticModel model, TCastExpression node, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsUnnecessaryCast(model, node, cancellationToken))
        {
            return null;
        }

        var tree = model.SyntaxTree;
        if (tree.OverlapsHiddenPosition(node.Span, cancellationToken))
        {
            return null;
        }

        return Diagnostic.Create(
            Descriptor,
            node.SyntaxTree.GetLocation(GetFadeSpan(node)),
            ImmutableArray.Create(node.GetLocation()));
    }
}
