// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using static Razor.Diagnostics.Analyzers.Resources;

namespace Razor.Diagnostics.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ImmutableArrayBoxingAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticIds.ImmutableArrayBoxing,
        CreateLocalizableResourceString(nameof(ImmutableArrayBoxingTitle)),
        CreateLocalizableResourceString(nameof(ImmutableArrayBoxingMessage)),
        DiagnosticCategory.Performance,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateLocalizableResourceString(nameof(ImmutableArrayBoxingDescription)));

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(context =>
        {
            var immutableArray = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.ImmutableArray_T);
            if (immutableArray is null)
            {
                return;
            }

            var readOnlyListExtensions = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.ReadOnlyListExtensions);
            if (readOnlyListExtensions is null)
            {
                return;
            }

            var enumerableExtensions = context.Compilation.GetTypeByMetadataName(WellKnownTypeNames.EnumerableExtensions);
            if (enumerableExtensions is null)
            {
                return;
            }

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

        var isReadOnlyListExtensions = SymbolEqualityComparer.Default.Equals(targetMethod.OriginalDefinition.ContainingType, readOnlyListExtensionsType);
        var isEnumerableExtensions = SymbolEqualityComparer.Default.Equals(targetMethod.OriginalDefinition.ContainingType, enumerableExtensionsType);

        if (!isReadOnlyListExtensions && !isEnumerableExtensions)
        {
            return;
        }

        var instance = invocation.Instance ?? invocation.Arguments.FirstOrDefault()?.Value;

        if (instance is not IConversionOperation conversionOperation)
        {
            return;
        }

        var conversion = conversionOperation.GetConversion();
        if (!conversion.IsBoxing)
        {
            return;
        }

        if (conversionOperation.Operand.Type is not INamedTypeSymbol operandType ||
            !SymbolEqualityComparer.Default.Equals(operandType.OriginalDefinition, immutableArrayType))
        {
            return;
        }

        var typeName = isReadOnlyListExtensions
            ? WellKnownTypeNames.ReadOnlyListExtensions
            : WellKnownTypeNames.EnumerableExtensions;

        context.ReportDiagnostic(instance.CreateDiagnostic(Rule, $"{typeName}.{targetMethod.Name}"));
    }
}
