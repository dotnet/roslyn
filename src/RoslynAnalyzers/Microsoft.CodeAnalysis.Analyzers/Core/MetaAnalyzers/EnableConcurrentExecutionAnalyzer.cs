// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1026: <inheritdoc cref="EnableConcurrentExecutionTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class EnableConcurrentExecutionAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.EnableConcurrentExecutionRuleId,
            CreateLocalizableResourceString(nameof(EnableConcurrentExecutionTitle)),
            CreateLocalizableResourceString(nameof(EnableConcurrentExecutionMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

#pragma warning disable RS1025 // Configure generated code analysis
        public override void Initialize(AnalysisContext context)
#pragma warning restore RS1025 // Configure generated code analysis
        {
            context.EnableConcurrentExecution();

            base.Initialize(context);
        }

        [SuppressMessage("AnalyzerPerformance", "RS1012:Start action has no registered actions.", Justification = "Method returns an analyzer that is registered by the caller.")]
        protected override DiagnosticAnalyzerSymbolAnalyzer? GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var compilation = compilationContext.Compilation;

            var analysisContext = compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsAnalysisContext);
            if (analysisContext is null)
            {
                return null;
            }

            compilationContext.RegisterOperationBlockStartAction(context =>
            {
                if (context.OwningSymbol?.Kind != SymbolKind.Method)
                {
                    return;
                }

                var method = (IMethodSymbol)context.OwningSymbol;
                if (method.Name != nameof(DiagnosticAnalyzer.Initialize))
                {
                    return;
                }

                IParameterSymbol? analysisContextParameter = null;
                foreach (var parameter in method.Parameters)
                {
                    if (!SymbolEqualityComparer.Default.Equals(parameter.Type, analysisContext))
                    {
                        continue;
                    }

                    analysisContextParameter = parameter;
                    break;
                }

                if (analysisContextParameter is null)
                {
                    return;
                }

                var analyzer = new EnableConcurrentExecutionOperationAnalyzer(analysisContextParameter);
                context.RegisterOperationAction(analyzer.HandleInvocationOperation, OperationKind.Invocation);
                context.RegisterOperationBlockEndAction(analyzer.HandleOperationBlockEnd);
            });

            // This analyzer only performs operation block analysis
            return null;
        }

        private sealed class EnableConcurrentExecutionOperationAnalyzer
        {
            private readonly IParameterSymbol _analysisContextParameter;

            public EnableConcurrentExecutionOperationAnalyzer(IParameterSymbol analysisContextParameter)
            {
                _analysisContextParameter = analysisContextParameter;
            }

            public bool EnabledConcurrentExecution { get; private set; }

            internal void HandleInvocationOperation(OperationAnalysisContext context)
            {
                if (EnabledConcurrentExecution)
                {
                    return;
                }

                var invocation = (IInvocationOperation)context.Operation;
                if (invocation.TargetMethod?.Name != nameof(AnalysisContext.EnableConcurrentExecution))
                {
                    return;
                }

                if (invocation.Instance?.Kind != OperationKind.ParameterReference)
                {
                    return;
                }

                var parameterReference = (IParameterReferenceOperation)invocation.Instance;
                if (!SymbolEqualityComparer.Default.Equals(parameterReference.Parameter, _analysisContextParameter))
                {
                    return;
                }

                EnabledConcurrentExecution = true;
            }

            internal void HandleOperationBlockEnd(OperationBlockAnalysisContext context)
            {
                if (!EnabledConcurrentExecution)
                {
                    context.ReportDiagnostic(_analysisContextParameter.CreateDiagnostic(Rule));
                }
            }
        }
    }
}
