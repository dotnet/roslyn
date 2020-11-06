// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
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

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info)
        {
            var node = context.Node;
            var semanticModel = context.SemanticModel;
            Action<Diagnostic> reportDiagnostic = context.ReportDiagnostic;
            var cancellationToken = context.CancellationToken;

            if (semanticModel.GetOperation(node, cancellationToken) is not IInvocationOperation invocationOperation)
            {
                return;
            }

            var targetMethod = invocationOperation.TargetMethod;

            if (targetMethod.IsOverride)
            {
                CheckNonOverridenMethodOnStruct(targetMethod, reportDiagnostic, node);
            }

            bool compilationHasSystemArrayEmpty = !semanticModel.Compilation.GetSpecialType(SpecialType.System_Array).GetMembers("Empty").IsEmpty;

            // Loop on every argument because params argument may not be the last one.
            //     static void Fun1() => Fun2(args: "", i: 5);
            //     static void Fun2(int i = 0, params object[] args) {}
            foreach (var argument in invocationOperation.Arguments)
            {
                if (argument.ArgumentKind == ArgumentKind.ParamArray)
                {
                    // Up to net45 the System.Array.Empty<T> singleton didn't existed so an empty params array was still causing some memory allocation.
                    if (argument.IsImplicit &&
                        (!compilationHasSystemArrayEmpty || (argument.Value as IArrayCreationOperation)?.Initializer.ElementValues.IsEmpty == true))
                    {
                        reportDiagnostic(node.CreateDiagnostic(ParamsParameterRule));
                    }
                    break;
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
                    reportDiagnostic(node.CreateDiagnostic(ValueTypeNonOverridenCallRule));
                }
            }
        }
    }
}
