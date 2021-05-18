// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class CodeActionCreateAnalyzer : DiagnosticAnalyzer
    {
        internal const string CodeActionMetadataName = "Microsoft.CodeAnalysis.CodeActions.CodeAction";
        internal const string CreateMethodName = "Create";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotUseGenericCodeActionCreateToCreateCodeActionTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.DoNotUseGenericCodeActionCreateToCreateCodeActionMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static readonly DiagnosticDescriptor DoNotUseCodeActionCreateRule = new(
            RoslynDiagnosticIds.DoNotUseCodeActionCreateRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DoNotUseCodeActionCreateRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var codeActionSymbol = context.Compilation.GetOrCreateTypeByMetadataName(CodeActionMetadataName);
            if (codeActionSymbol == null)
            {
                return;
            }

            var createSymbols = codeActionSymbol.GetMembers(CreateMethodName).OfType<IMethodSymbol>();
            ImmutableHashSet<IMethodSymbol> createSymbolsSet = ImmutableHashSet.CreateRange(createSymbols);
            if (createSymbolsSet.IsEmpty)
            {
                return;
            }

            context.RegisterOperationAction(context => AnalyzeInvocation(context, createSymbolsSet), OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context, ImmutableHashSet<IMethodSymbol> symbols)
        {
            if (symbols.Contains(((IInvocationOperation)context.Operation).TargetMethod))
            {
                context.ReportDiagnostic(context.Operation.CreateDiagnostic(DoNotUseCodeActionCreateRule));
            }
        }
    }
}
