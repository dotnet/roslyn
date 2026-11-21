// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
    using static AnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class DisplayClassAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ClosureDriverRuleId = "HAA0301";
        public const string ClosureCaptureRuleId = "HAA0302";
        public const string LambaOrAnonymousMethodInGenericMethodRuleId = "HAA0303";
        public const string LocalFunctionClosureRuleId = "HAA0304";

        internal static readonly DiagnosticDescriptor ClosureDriverRule = new(
            ClosureDriverRuleId,
            CreateLocalizableResourceString(nameof(ClosureDriverRuleTitle)),
            CreateLocalizableResourceString(nameof(ClosureDriverRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ClosureCaptureRule = new(
            ClosureCaptureRuleId,
            CreateLocalizableResourceString(nameof(ClosureCaptureRuleTitle)),
            CreateLocalizableResourceString(nameof(ClosureCaptureRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor LambaOrAnonymousMethodInGenericMethodRule = new(
            LambaOrAnonymousMethodInGenericMethodRuleId,
            CreateLocalizableResourceString(nameof(LambaOrAnonymousMethodInGenericMethodRuleTitle)),
            CreateLocalizableResourceString(nameof(LambaOrAnonymousMethodInGenericMethodRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor LocalFunctionClosureRule = new(
            LocalFunctionClosureRuleId,
            CreateLocalizableResourceString(nameof(LocalFunctionClosureRuleTitle)),
            CreateLocalizableResourceString(nameof(LocalFunctionClosureRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ClosureCaptureRule, ClosureDriverRule, LambaOrAnonymousMethodInGenericMethodRule, LocalFunctionClosureRule);

        protected override ImmutableArray<SyntaxKind> Expressions { get; } = ImmutableArray.Create(SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression, SyntaxKind.AnonymousMethodExpression, SyntaxKind.IdentifierName, SyntaxKind.GenericName);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;

            // IdentifierNameSyntax covers 'Local', GenericNameSyntax covers 'Local<int>'.
            if (node is SimpleNameSyntax simpleName)
            {
                LocalFunctionDelegateConversionCheck(semanticModel, simpleName, reportDiagnostic, cancellationToken);
                return;
            }

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

        /// <summary>
        /// Reports <see cref="LocalFunctionClosureRule"/> when a reference to a capturing local function is converted
        /// to a delegate. Directly invoking a local function needs no heap allocation, because the compiler passes the
        /// captured state as a by-ref struct; converting it to a delegate is what forces that state onto the heap in a
        /// display class. The check runs at the reference site so every conversion position is covered uniformly, and
        /// it deliberately does not depend on <c>HAA0603</c>, which is not reported for every position.
        /// </summary>
        private static void LocalFunctionDelegateConversionCheck(SemanticModel semanticModel, SimpleNameSyntax node, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            // A direct invocation such as 'LocalFunction()' does not create a delegate.
            if (node.Parent is InvocationExpressionSyntax invocation && invocation.Expression == node)
            {
                return;
            }

            // Local functions are always referenced by a bare identifier, never through a member access.
            if (node.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == node)
            {
                return;
            }

            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is not IMethodSymbol { MethodKind: MethodKind.LocalFunction } localFunction)
            {
                return;
            }

            // Only a conversion to a delegate type forces the captured state onto the heap.
            if (semanticModel.GetTypeInfo(node, cancellationToken).ConvertedType?.TypeKind != TypeKind.Delegate)
            {
                return;
            }

            if (localFunction.DeclaringSyntaxReferences.IsDefaultOrEmpty ||
                localFunction.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is not LocalFunctionStatementSyntax declaration)
            {
                return;
            }

            // Analyzing the local function statement itself, rather than its body, is what makes the local function
            // count as a capturing function inside the analyzed region.
            var flow = semanticModel.AnalyzeDataFlow(declaration);
            if (flow is not { Succeeded: true })
            {
                return;
            }

            // CapturedInside holds the variables captured by functions declared inside the region, which for this
            // region is the local function itself. Variables declared inside the local function are filtered out:
            // when a nested lambda captures one of those, that display class is allocated as the local function runs,
            // whether or not the local function was ever converted to a delegate.
            var captured = flow.CapturedInside.WhereAsArray(symbol => !IsDeclaredWithin(symbol, declaration));
            if (captured.IsEmpty)
            {
                return;
            }

            reportDiagnostic(Diagnostic.Create(
                LocalFunctionClosureRule,
                node.GetLocation(),
                new object[] { localFunction.Name, string.Join(",", captured.Select(x => x.Name)) }));
        }

        private static bool IsDeclaredWithin(ISymbol symbol, SyntaxNode node)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree == node.SyntaxTree && node.Span.Contains(location.SourceSpan))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ClosureCaptureDataFlowAnalysis(DataFlowAnalysis? flow, Action<Diagnostic> reportDiagnostic, Location location)
        {
            if (flow == null ||
                flow.Captured.IsEmpty)
            {
                return;
            }

            foreach (var capture in flow.Captured)
            {
                if (capture is { Name: not null, Locations.IsDefault: false })
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
            if (semanticModel.GetSymbolInfo(node, cancellationToken).Symbol is { } symbol)
            {
                var containingSymbol = symbol.ContainingSymbol;
                if (containingSymbol is IMethodSymbol methodSymbol && methodSymbol.Arity > 0)
                {
                    reportDiagnostic(Diagnostic.Create(LambaOrAnonymousMethodInGenericMethodRule, location, EmptyMessageArgs));
                }
            }
        }
    }
}
