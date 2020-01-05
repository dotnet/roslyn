// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Diagnostics.RemoveUnnecessaryCast
{
    internal abstract class RemoveUnnecessaryCastDiagnosticAnalyzerBase<
        TLanguageKindEnum,
        TCastExpression> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TCastExpression : SyntaxNode
    {
        protected RemoveUnnecessaryCastDiagnosticAnalyzerBase()
            : base(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId,
                   option: null,
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_Unnecessary_Cast), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(WorkspacesResources.Cast_is_redundant), WorkspacesResources.ResourceManager, typeof(WorkspacesResources)))
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
            var diagnostic = TryRemoveCastExpression(
                context.SemanticModel,
                (TCastExpression)context.Node,
                context.CancellationToken);

            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        private Diagnostic TryRemoveCastExpression(SemanticModel model, TCastExpression node, CancellationToken cancellationToken)
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
                UnnecessaryWithSuggestionDescriptor,
                node.SyntaxTree.GetLocation(GetFadeSpan(node)),
                ImmutableArray.Create(node.GetLocation()));
        }
    }
}
