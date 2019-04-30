// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ConfigureGeneratedCodeAnalysisAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ConfigureGeneratedCodeAnalysisTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ConfigureGeneratedCodeAnalysisMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ConfigureGeneratedCodeAnalysisDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.ConfigureGeneratedCodeAnalysisRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

#pragma warning disable RS1025 // Configure generated code analysis
        public override void Initialize(AnalysisContext context)
#pragma warning restore RS1025 // Configure generated code analysis
        {
            context.EnableConcurrentExecution();

            base.Initialize(context);
        }

        [SuppressMessage("AnalyzerPerformance", "RS1012:Start action has no registered actions.", Justification = "Method returns an analyzer that is registered by the caller.")]
        protected override DiagnosticAnalyzerSymbolAnalyzer GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var compilation = compilationContext.Compilation;

            var analysisContext = compilation.GetTypeByMetadataName(AnalysisContextFullName);
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

                IParameterSymbol analysisContextParameter = null;
                foreach (var parameter in method.Parameters)
                {
                    if (!Equals(parameter.Type, analysisContext))
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

                var analyzer = new ConfigureGeneratedCodeAnalyzer(analysisContextParameter);
                context.RegisterOperationAction(analyzer.HandleInvocationOperation, OperationKind.Invocation);
                context.RegisterOperationBlockEndAction(analyzer.HandleOperationBlockEnd);
            });

            // This analyzer only performs operation block analysis
            return null;
        }

        private sealed class ConfigureGeneratedCodeAnalyzer
        {
            private readonly IParameterSymbol _analysisContextParameter;

            public ConfigureGeneratedCodeAnalyzer(IParameterSymbol analysisContextParameter)
            {
                _analysisContextParameter = analysisContextParameter;
            }

            public bool ConfiguredGeneratedCodeAnalysis { get; private set; }

            internal void HandleInvocationOperation(OperationAnalysisContext context)
            {
                if (ConfiguredGeneratedCodeAnalysis)
                {
                    return;
                }

                var invocation = (IInvocationOperation)context.Operation;
                if (invocation.TargetMethod?.Name != nameof(AnalysisContext.ConfigureGeneratedCodeAnalysis))
                {
                    return;
                }

                if (invocation.Instance?.Kind != OperationKind.ParameterReference)
                {
                    return;
                }

                var parameterReference = (IParameterReferenceOperation)invocation.Instance;
                if (!Equals(parameterReference.Parameter, _analysisContextParameter))
                {
                    return;
                }

                ConfiguredGeneratedCodeAnalysis = true;
            }

            internal void HandleOperationBlockEnd(OperationBlockAnalysisContext context)
            {
                if (!ConfiguredGeneratedCodeAnalysis)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, _analysisContextParameter.Locations.FirstOrDefault()));
                }
            }
        }
    }
}
