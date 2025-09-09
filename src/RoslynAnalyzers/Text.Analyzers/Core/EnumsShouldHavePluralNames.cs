// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Text.Analyzers
{
    using static TextAnalyzersResources;

    /// <summary>
    /// CA1714: <inheritdoc cref="FlagsEnumsShouldHavePluralNamesTitle"/>
    /// CA1717: <inheritdoc cref="OnlyFlagsEnumsShouldHavePluralNamesTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class EnumsShouldHavePluralNamesAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId_Plural = "CA1714";

        internal static readonly DiagnosticDescriptor Rule_CA1714 =
            DiagnosticDescriptorHelper.Create(
                RuleId_Plural,
                CreateLocalizableResourceString(nameof(FlagsEnumsShouldHavePluralNamesTitle)),
                CreateLocalizableResourceString(nameof(FlagsEnumsShouldHavePluralNamesMessage)),
                DiagnosticCategory.Naming,
                RuleLevel.Disabled,
                description: CreateLocalizableResourceString(nameof(FlagsEnumsShouldHavePluralNamesDescription)),
                isPortedFxCopRule: true,
                isDataflowRule: false);

        internal const string RuleId_NoPlural = "CA1717";

        internal static readonly DiagnosticDescriptor Rule_CA1717 =
            DiagnosticDescriptorHelper.Create(
                RuleId_NoPlural,
                CreateLocalizableResourceString(nameof(OnlyFlagsEnumsShouldHavePluralNamesTitle)),
                CreateLocalizableResourceString(nameof(OnlyFlagsEnumsShouldHavePluralNamesMessage)),
                DiagnosticCategory.Naming,
                RuleLevel.Disabled,
                description: CreateLocalizableResourceString(nameof(OnlyFlagsEnumsShouldHavePluralNamesDescription)),
                isPortedFxCopRule: true,
                isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule_CA1714, Rule_CA1717);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? flagsAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemFlagsAttribute);
                if (flagsAttribute == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, flagsAttribute), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol flagsAttribute)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind != TypeKind.Enum)
            {
                return;
            }

            var reportCA1714 = context.Options.MatchesConfiguredVisibility(Rule_CA1714, symbol, context.Compilation);
            var reportCA1717 = context.Options.MatchesConfiguredVisibility(Rule_CA1717, symbol, context.Compilation);
            if (!reportCA1714 && !reportCA1717)
            {
                return;
            }

            if (symbol.Name.EndsWith("i", StringComparison.OrdinalIgnoreCase) || symbol.Name.EndsWith("ae", StringComparison.OrdinalIgnoreCase))
            {
                // Skip words ending with 'i' and 'ae' to avoid flagging irregular plurals.
                // Humanizer does not recognize these as plurals, such as 'formulae', 'trophi', etc.
                return;
            }

            if (!symbol.Name.IsASCII())
            {
                // Skip non-ASCII names.
                return;
            }

            if (symbol.HasAnyAttribute(flagsAttribute))
            {
                if (reportCA1714 && !IsPlural(symbol.Name)) // Checking Rule CA1714
                {
                    context.ReportDiagnostic(symbol.CreateDiagnostic(Rule_CA1714, symbol.OriginalDefinition.Locations.First(), symbol.Name));
                }
            }
            else
            {
                if (reportCA1717 && IsPlural(symbol.Name)) // Checking Rule CA1717
                {
                    context.ReportDiagnostic(symbol.CreateDiagnostic(Rule_CA1717, symbol.OriginalDefinition.Locations.First(), symbol.Name));
                }
            }
        }

        private static bool IsPlural(string word)
            => word.Equals(word.Pluralize(inputIsKnownToBeSingular: false), StringComparison.OrdinalIgnoreCase);
    }
}
