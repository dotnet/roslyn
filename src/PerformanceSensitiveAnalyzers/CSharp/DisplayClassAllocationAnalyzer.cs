// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class DisplayClassAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ClosureDriverRuleId = "HAA0301";
        public const string ClosureCaptureRuleId = "HAA0302";
        public const string LambaOrAnonymousMethodInGenericMethodRuleId = "HAA0303";

        private static readonly LocalizableString s_localizableClosureDriverRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ClosureDriverRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableClosureDriverRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ClosureDriverRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableClosureCaptureRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ClosureCaptureRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableClosureCaptureRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ClosureCaptureRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableLambaOrAnonymousMethodInGenericMethodRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.LambaOrAnonymousMethodInGenericMethodRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableLambaOrAnonymousMethodInGenericMethodRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.LambaOrAnonymousMethodInGenericMethodRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));


        internal static DiagnosticDescriptor ClosureDriverRule = new DiagnosticDescriptor(
            ClosureDriverRuleId,
            s_localizableClosureDriverRuleTitle,
            s_localizableClosureDriverRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor ClosureCaptureRule = new DiagnosticDescriptor(
            ClosureCaptureRuleId,
            s_localizableClosureCaptureRuleTitle,
            s_localizableClosureCaptureRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor LambaOrAnonymousMethodInGenericMethodRule = new DiagnosticDescriptor(
            LambaOrAnonymousMethodInGenericMethodRuleId,
            s_localizableLambaOrAnonymousMethodInGenericMethodRuleTitle,
            s_localizableLambaOrAnonymousMethodInGenericMethodRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ClosureCaptureRule, ClosureDriverRule, LambaOrAnonymousMethodInGenericMethodRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;

            var anonExpr = node as AnonymousMethodExpressionSyntax;
            if (anonExpr?.Block?.ChildNodes() != null && anonExpr.Block.ChildNodes().Any())
            {
                GenericMethodCheck(semanticModel, node, anonExpr.DelegateKeyword.GetLocation(), reportDiagnostic, cancellationToken);
                ClosureCaptureDataFlowAnalysis(semanticModel.AnalyzeDataFlow(anonExpr.Block.ChildNodes().First(), anonExpr.Block.ChildNodes().Last()), reportDiagnostic, anonExpr.DelegateKeyword.GetLocation());
                return;
            }

            if (node is SimpleLambdaExpressionSyntax lambdaExpr)
            {
                GenericMethodCheck(semanticModel, node, lambdaExpr.ArrowToken.GetLocation(), reportDiagnostic, cancellationToken);
                ClosureCaptureDataFlowAnalysis(semanticModel.AnalyzeDataFlow(lambdaExpr), reportDiagnostic, lambdaExpr.ArrowToken.GetLocation());
                return;
            }

            if (node is ParenthesizedLambdaExpressionSyntax parenLambdaExpr)
            {
                GenericMethodCheck(semanticModel, node, parenLambdaExpr.ArrowToken.GetLocation(), reportDiagnostic, cancellationToken);
                ClosureCaptureDataFlowAnalysis(semanticModel.AnalyzeDataFlow(parenLambdaExpr), reportDiagnostic, parenLambdaExpr.ArrowToken.GetLocation());
                return;
            }
        }

        private static void ClosureCaptureDataFlowAnalysis(DataFlowAnalysis? flow, Action<Diagnostic> reportDiagnostic, Location location)
        {
            if (flow == null ||
                flow.Captured.Length <= 0)
            {
                return;
            }

            foreach (var capture in flow.Captured)
            {
                if (capture.Name != null && capture.Locations != null)
                {
                    foreach (var l in capture.Locations)
                    {
                        reportDiagnostic(Diagnostic.Create(ClosureCaptureRule, l, EmptyMessageArgs));
                    }
                }
            }

            reportDiagnostic(Diagnostic.Create(ClosureDriverRule, location, new[] { string.Join(",", flow.Captured.Select(x => x.Name)) }));
        }

        private static void GenericMethodCheck(SemanticModel semanticModel, SyntaxNode node, Location location, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol != null)
            {
                var containingSymbol = semanticModel.GetSymbolInfo(node, cancellationToken).Symbol.ContainingSymbol;
                if (containingSymbol is IMethodSymbol methodSymbol && methodSymbol.Arity > 0)
                {
                    reportDiagnostic(Diagnostic.Create(LambaOrAnonymousMethodInGenericMethodRule, location, EmptyMessageArgs));
                }
            }
        }
    }
}