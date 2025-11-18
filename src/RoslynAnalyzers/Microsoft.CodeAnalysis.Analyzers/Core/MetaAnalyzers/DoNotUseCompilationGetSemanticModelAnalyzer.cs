// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1030: <inheritdoc cref="DoNotUseCompilationGetSemanticModelTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseCompilationGetSemanticModelAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.DoNotUseCompilationGetSemanticModelRuleId,
            CreateLocalizableResourceString(nameof(DoNotUseCompilationGetSemanticModelTitle)),
            CreateLocalizableResourceString(nameof(DoNotUseCompilationGetSemanticModelMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotUseCompilationGetSemanticModelDescription)),
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilationContext.Compilation);

                if (!wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzer, out var diagnosticAnalyzerType) ||
                    !wellKnownTypeProvider.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCompilation, out var compilationType))
                {
                    return;
                }

                var csharpCompilation = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpCSharpCompilation);
                var visualBasicCompilation = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisVisualBasicVisualBasicCompilation);

                compilationContext.RegisterOperationBlockStartAction(operationBlockContext =>
                {
                    if (operationBlockContext.OwningSymbol is IMethodSymbol methodSymbol &&
                        methodSymbol.ContainingType.Inherits(diagnosticAnalyzerType))
                    {
                        operationBlockContext.RegisterOperationAction(operationContext =>
                        {
                            var invocation = (IInvocationOperation)operationContext.Operation;

                            if (invocation.TargetMethod.Name.Equals("GetSemanticModel", StringComparison.Ordinal) &&
                                (
                                    SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, compilationType) ||
                                    SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, csharpCompilation) ||
                                    SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, visualBasicCompilation)
                                ))
                            {
                                operationContext.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(Rule));
                            }
                        }, OperationKind.Invocation);
                    }
                });
            });
        }
    }
}
