// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers;

namespace Microsoft.CodeAnalysis.CSharp.PerformanceSensitiveAnalyzers
{
    using static AnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CallSiteImplicitAllocationAnalyzer : AbstractAllocationAnalyzer<SyntaxKind>
    {
        public const string ParamsParameterRuleId = "HAA0101";
        public const string ValueTypeNonOverridenCallRuleId = "HAA0102";

        internal static readonly DiagnosticDescriptor ParamsParameterRule = new(
            ParamsParameterRuleId,
            CreateLocalizableResourceString(nameof(ParamsParameterRuleTitle)),
            CreateLocalizableResourceString(nameof(ParamsParameterRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor ValueTypeNonOverridenCallRule = new(
            ValueTypeNonOverridenCallRuleId,
            CreateLocalizableResourceString(nameof(ValueTypeNonOverridenCallRuleTitle)),
            CreateLocalizableResourceString(nameof(ValueTypeNonOverridenCallRuleMessage)),
            DiagnosticCategory.Performance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(ParamsParameterRule, ValueTypeNonOverridenCallRule);

        protected override ImmutableArray<SyntaxKind> Expressions { get; } = ImmutableArray.Create(SyntaxKind.InvocationExpression);

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
                        (!compilationHasSystemArrayEmpty || (argument.Value as IArrayCreationOperation)?.Initializer?.ElementValues.IsEmpty != true))
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
