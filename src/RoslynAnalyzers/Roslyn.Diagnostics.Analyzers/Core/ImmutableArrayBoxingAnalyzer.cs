// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0066: <inheritdoc cref="ImmutableArrayBoxingTitle"/>
    /// </summary>
#pragma warning disable RS1004 // Recommend adding language support to diagnostic analyzer
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1004 // Recommend adding language support to diagnostic analyzer
    public class ImmutableArrayBoxingAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.ImmutableArrayBoxingRuleId,
            CreateLocalizableResourceString(nameof(ImmutableArrayBoxingTitle)),
            CreateLocalizableResourceString(nameof(ImmutableArrayBoxingMessage)),
            DiagnosticCategory.RoslynDiagnosticsPerformance,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ImmutableArrayBoxingDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var immutableArray = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsImmutableImmutableArray1);
                if (immutableArray is null)
                    return;

                var readOnlyListExtensions = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericReadOnlyListExtensions);
                if (readOnlyListExtensions is null)
                    return;

                var enumerableExtensions = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericEnumerableExtensions);
                if (enumerableExtensions is null)
                    return;

                context.RegisterOperationAction(
                    context => AnalyzeInvocation(context, immutableArray, readOnlyListExtensions, enumerableExtensions),
                    OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(
            OperationAnalysisContext context,
            INamedTypeSymbol immutableArrayType,
            INamedTypeSymbol readOnlyListExtensionsType,
            INamedTypeSymbol enumerableExtensionsType)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var targetMethod = invocation.TargetMethod.ReducedFrom ?? invocation.TargetMethod;

            var isReadOnlyListExtensions = Equals(targetMethod.OriginalDefinition.ContainingType, readOnlyListExtensionsType);
            var isEnumerableExtensions = Equals(targetMethod.OriginalDefinition.ContainingType, enumerableExtensionsType);

            if (!isReadOnlyListExtensions && !isEnumerableExtensions)
                return;

            var instance = invocation.Instance ?? invocation.Arguments.FirstOrDefault()?.Value;

            if (instance is not IConversionOperation conversionOperation)
                return;

            if (conversionOperation.Type?.TypeKind != TypeKind.Interface)
                return;

            if (conversionOperation.Operand.Type is not INamedTypeSymbol operandType ||
                !Equals(operandType.OriginalDefinition, immutableArrayType))
            {
                return;
            }

            var typeName = isReadOnlyListExtensions
                ? WellKnownTypeNames.SystemCollectionsGenericReadOnlyListExtensions
                : WellKnownTypeNames.SystemCollectionsGenericEnumerableExtensions;

            context.ReportDiagnostic(instance.CreateDiagnostic(Rule, $"{typeName}.{targetMethod.Name}"));
        }
    }
}
