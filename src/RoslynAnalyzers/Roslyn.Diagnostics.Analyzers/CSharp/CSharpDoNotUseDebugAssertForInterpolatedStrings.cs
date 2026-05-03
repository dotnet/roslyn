// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CSharpDoNotUseDebugAssertForInterpolatedStrings : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.DoNotUseInterpolatedStringsWithDebugAssertRuleId,
            CreateLocalizableResourceString(nameof(DoNotUseInterpolatedStringsWithDebugAssertTitle)),
            CreateLocalizableResourceString(nameof(DoNotUseInterpolatedStringsWithDebugAssertMessage)),
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(DoNotUseInterpolatedStringsWithDebugAssertDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(context =>
            {
                var debugType = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemDiagnosticsDebug);

                if (debugType is null)
                {
                    return;
                }

                IMethodSymbol? assertMethod = null;

                foreach (var member in debugType.GetMembers("Assert"))
                {
                    if (member is IMethodSymbol { Parameters: [{ Type.SpecialType: SpecialType.System_Boolean }, { Type.SpecialType: SpecialType.System_String }] } method)
                    {
                        assertMethod = method;
                        break;
                    }
                }

                if (assertMethod is null)
                {
                    return;
                }

                context.RegisterOperationAction(context =>
                {
                    var invocation = (IInvocationOperation)context.Operation;

                    if (invocation.TargetMethod.Equals(assertMethod) &&
                        invocation.Arguments is [_, IArgumentOperation { Value: IInterpolatedStringOperation { ConstantValue.HasValue: false } }])
                    {
                        context.ReportDiagnostic(invocation.CreateDiagnostic(Rule));
                    }
                }, OperationKind.Invocation);
            });
        }
    }
}
