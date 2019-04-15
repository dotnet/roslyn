// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CallSiteImplicitAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ParamsParameterRuleId = "HAA0101";
        public const string ValueTypeNonOverridenCallRuleId = "HAA0102";

        private static readonly LocalizableString s_localizableParamsParameterRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ParamsParameterRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableParamsParameterRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ParamsParameterRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));

        private static readonly LocalizableString s_localizableValueTypeNonOverridenCallRuleTitle = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeNonOverridenCallRuleTitle), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));
        private static readonly LocalizableString s_localizableValueTypeNonOverridenCallRuleMessage = new LocalizableResourceString(nameof(AnalyzersResources.ValueTypeNonOverridenCallRuleMessage), AnalyzersResources.ResourceManager, typeof(AnalyzersResources));


        internal static DiagnosticDescriptor ParamsParameterRule = new DiagnosticDescriptor(
            ParamsParameterRuleId,
            s_localizableParamsParameterRuleTitle,
            s_localizableParamsParameterRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor ValueTypeNonOverridenCallRule = new DiagnosticDescriptor(
            ValueTypeNonOverridenCallRuleId,
            s_localizableValueTypeNonOverridenCallRuleTitle,
            s_localizableValueTypeNonOverridenCallRuleMessage,
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ParamsParameterRule, ValueTypeNonOverridenCallRule);

        protected override ImmutableArray<SyntaxKind> Expressions => ImmutableArray.Create(SyntaxKind.InvocationExpression);

        private static readonly object[] EmptyMessageArgs = Array.Empty<object>();

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;

            var invocationExpression = node as InvocationExpressionSyntax;

            if (semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol is IMethodSymbol methodInfo)
            {
                if (methodInfo.IsOverride)
                {
                    CheckNonOverridenMethodOnStruct(methodInfo, reportDiagnostic, invocationExpression);
                }

                if (methodInfo.Parameters.Length > 0 && invocationExpression.ArgumentList != null)
                {
                    var lastParam = methodInfo.Parameters[methodInfo.Parameters.Length - 1];
                    if (lastParam.IsParams)
                    {
                        CheckParam(invocationExpression, methodInfo, semanticModel, reportDiagnostic, cancellationToken);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CheckParam(InvocationExpressionSyntax invocationExpression, IMethodSymbol methodInfo, SemanticModel semanticModel, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
        {
            var arguments = invocationExpression.ArgumentList.Arguments;
            if (arguments.Count != methodInfo.Parameters.Length)
            {
                reportDiagnostic(Diagnostic.Create(ParamsParameterRule, invocationExpression.GetLocation(), EmptyMessageArgs));
            }
            else
            {
                var lastIndex = arguments.Count - 1;
                var lastArgumentTypeInfo = semanticModel.GetTypeInfo(arguments[lastIndex].Expression, cancellationToken);
                if (lastArgumentTypeInfo.Type != null && !lastArgumentTypeInfo.Type.Equals(methodInfo.Parameters[lastIndex].Type))
                {
                    reportDiagnostic(Diagnostic.Create(ParamsParameterRule, invocationExpression.GetLocation(), EmptyMessageArgs));
                }
            }
        }

        private static void CheckNonOverridenMethodOnStruct(IMethodSymbol methodInfo, Action<Diagnostic> reportDiagnostic, SyntaxNode node)
        {
            if (methodInfo.ContainingType != null)
            {
                // hack? Hmmm.
                var containingType = methodInfo.ContainingType.ToString();
                if (string.Equals(containingType, "System.ValueType", StringComparison.OrdinalIgnoreCase) || string.Equals(containingType, "System.Enum", StringComparison.OrdinalIgnoreCase))
                {
                    reportDiagnostic(Diagnostic.Create(ValueTypeNonOverridenCallRule, node.GetLocation(), EmptyMessageArgs));
                }
            }
        }
    }
}