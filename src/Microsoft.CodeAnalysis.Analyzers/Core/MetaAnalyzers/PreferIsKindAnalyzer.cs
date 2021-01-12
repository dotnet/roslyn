// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PreferIsKindAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.PreferIsKindTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.PreferIsKindMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.PreferIsKindDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        internal static DiagnosticDescriptor Rule = new(
            DiagnosticIds.PreferIsKindRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.MicrosoftCodeAnalysisPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(context =>
            {
                // Kind() methods
                //  Microsoft.CodeAnalysis.CSharp.CSharpExtensions.Kind
                //  Microsoft.CodeAnalysis.VisualBasic.VisualBasicExtensions.Kind
                //
                // IsKind() methods
                //  Microsoft.CodeAnalysis.CSharpExtensions.IsKind
                //  Microsoft.CodeAnalysis.VisualBasicExtensions.IsKind

                Dictionary<INamedTypeSymbol, INamedTypeSymbol> containingTypeMap = new Dictionary<INamedTypeSymbol, INamedTypeSymbol>();
                if (context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpCSharpExtensions) is { } csharpKindExtensions
                    && context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCSharpExtensions) is { } csharpIsKindExtensions)
                {
                    containingTypeMap[csharpKindExtensions] = csharpIsKindExtensions;
                }

                if (context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisVisualBasicVisualBasicExtensions) is { } vbKindExtensions
                    && context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisVisualBasicExtensions) is { } vbIsKindExtensions)
                {
                    containingTypeMap[vbKindExtensions] = vbIsKindExtensions;
                }

                if (containingTypeMap.Count > 0)
                {
                    context.RegisterOperationAction(context => HandleBinaryOperation(context, containingTypeMap), OperationKind.Binary);
                }
            });
        }

        private static void HandleBinaryOperation(OperationAnalysisContext context, Dictionary<INamedTypeSymbol, INamedTypeSymbol> containingTypeMap)
        {
            var operation = (IBinaryOperation)context.Operation;
            if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            {
                return;
            }

            if (operation.LeftOperand.WalkDownConversion() is IInvocationOperation { TargetMethod: { Name: "Kind", ContainingType: var containingType } }
                && containingTypeMap.TryGetValue(containingType, out _))
            {
                context.ReportDiagnostic(operation.LeftOperand.CreateDiagnostic(Rule));
            }
        }
    }
}
