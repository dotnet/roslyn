// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Roslyn.Diagnostics.Analyzers;

namespace Roslyn.Diagnostics.CSharp.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DoNotCapturePrimaryConstructorParametersAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.DoNotCapturePrimaryConstructorParametersRuleId,
            CreateLocalizableResourceString(nameof(DoNotCapturePrimaryConstructorParametersTitle)),
            CreateLocalizableResourceString(nameof(DoNotCapturePrimaryConstructorParametersMessage)),
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Error,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(DoNotCapturePrimaryConstructorParametersDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterOperationAction(AnalyzeOperation, OperationKind.ParameterReference);
        }

        private static void AnalyzeOperation(OperationAnalysisContext context)
        {
            var operation = (IParameterReferenceOperation)context.Operation;

            if (operation.Parameter.ContainingSymbol == context.ContainingSymbol || operation.Parameter.ContainingSymbol is not IMethodSymbol { MethodKind: MethodKind.Constructor })
            {
                // We're in the primary constructor itself, so no capture.
                // Or, this isn't a primary constructor parameter at all.
                return;
            }

            IOperation rootOperation = operation;
            for (; rootOperation.Parent != null; rootOperation = rootOperation.Parent)
            {
            }

            if (rootOperation is IPropertyInitializerOperation or IFieldInitializerOperation)
            {
                // This is an explicit capture into member state. That's fine.
                return;
            }

            // This must be a capture. Error
            context.ReportDiagnostic(Diagnostic.Create(Rule, operation.Syntax.GetLocation(), operation.Parameter.Name));
        }
    }
}
