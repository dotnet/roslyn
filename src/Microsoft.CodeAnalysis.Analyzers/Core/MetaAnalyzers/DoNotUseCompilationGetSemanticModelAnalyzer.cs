// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseCompilationGetSemanticModelAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseCompilationGetSemanticModelTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseCompilationGetSemanticModelMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.DoNotUseCompilationGetSemanticModelDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.DoNotUseCompilationGetSemanticModelRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            description: s_localizableDescription,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            base.Initialize(context);
        }

        protected override DiagnosticAnalyzerSymbolAnalyzer? GetDiagnosticAnalyzerSymbolAnalyzer(CompilationStartAnalysisContext compilationContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            if (!compilationContext.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCompilation, out var compilationType))
            {
                return null;
            }

            var csharpCompilation = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpCSharpCompilation);
            var visualBasicCompilation = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisVisualBasicVisualBasicCompilation);

            compilationContext.RegisterOperationAction(oac =>
            {
                var invocation = (IInvocationOperation)oac.Operation;

                if (invocation.TargetMethod.Name.Equals("GetSemanticModel", StringComparison.Ordinal) &&
                    invocation.TargetMethod.Parameters.Length == 2 &&
                    (
                        invocation.TargetMethod.ContainingType.Equals(compilationType) ||
                        invocation.TargetMethod.ContainingType.Equals(csharpCompilation) ||
                        invocation.TargetMethod.ContainingType.Equals(visualBasicCompilation)
                    ))
                {
                    oac.ReportDiagnostic(invocation.Syntax.CreateDiagnostic(Rule));
                }
            }, OperationKind.Invocation);

            return null;
        }
    }
}
