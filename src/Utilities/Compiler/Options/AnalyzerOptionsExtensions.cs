// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1012 // Start action has no registered actions.

namespace Analyzer.Utilities
{
    internal static partial class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions> s_cachedOptions
            = new ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions>();
        private static readonly ImmutableHashSet<OutputKind> s_defaultOutputKinds =
            ImmutableHashSet.CreateRange(Enum.GetValues(typeof(OutputKind)).Cast<OutputKind>());

        public static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SymbolVisibilityGroup defaultValue,
            CancellationToken cancellationToken)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.ApiSurface, rule, defaultValue, cancellationToken);

        public static SymbolModifiers GetRequiredModifiersOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SymbolModifiers defaultValue,
            CancellationToken cancellationToken)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.RequiredModifiers, rule, defaultValue, cancellationToken);

        public static ImmutableHashSet<OutputKind> GetOutputKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.OutputKind, rule, s_defaultOutputKinds, cancellationToken);

        private static TEnum GetFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(
                optionName, rule,
                tryParseValue: (string value, out TEnum result) => Enum.TryParse(value, ignoreCase: true, result: out result),
                defaultValue: defaultValue);
        }

        private static ImmutableHashSet<TEnum> GetNonFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            ImmutableHashSet<TEnum> defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, rule, TryParseValue, defaultValue);
            static bool TryParseValue(string value, out ImmutableHashSet<TEnum> result)
            {
                var builder = ImmutableHashSet.CreateBuilder<TEnum>();
                foreach (var kindStr in value.Split(','))
                {
                    if (Enum.TryParse(kindStr, ignoreCase: true, result: out TEnum kind))
                    {
                        builder.Add(kind);
                    }
                }

                result = builder.ToImmutable();
                return builder.Count > 0;
            }
        }

#pragma warning disable IDE0051 // Remove unused private members - Used in some projects that include this shared project.
        private static TEnum GetNonFlagsEnumOptionValue<TEnum>(
#pragma warning restore IDE0051 // Remove unused private members
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(
                optionName, rule,
                tryParseValue: (string value, out TEnum result) => Enum.TryParse(value, ignoreCase: true, result: out result),
                defaultValue: defaultValue);
        }

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            bool defaultValue,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, rule, bool.TryParse, defaultValue);
        }

        public static uint GetUnsignedIntegralOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            uint defaultValue,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, rule, uint.TryParse, defaultValue);
        }

        public static SymbolNamesOption GetNullCheckValidationMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesOption(EditorConfigOptionNames.NullCheckValidationMethods, namePrefixOpt: "M:", rule, compilation, cancellationToken);

        public static SymbolNamesOption GetAdditionalStringFormattingMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesOption(EditorConfigOptionNames.AdditionalStringFormattingMethods, namePrefixOpt: "M:", rule, compilation, cancellationToken);

        public static SymbolNamesOption GetExcludedSymbolNamesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesOption(EditorConfigOptionNames.ExcludedSymbolNames, namePrefixOpt: null, rule, compilation, cancellationToken);

        public static SymbolNamesOption GetExcludedTypeNamesWithDerivedTypesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesOption(EditorConfigOptionNames.ExcludedTypeNamesWithDerivedTypes, namePrefixOpt: "T:", rule, compilation, cancellationToken);

        public static SymbolNamesOption GetDisallowedSymbolNamesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesOption(EditorConfigOptionNames.DisallowedSymbolNames, namePrefixOpt: null, rule, compilation, cancellationToken);

        private static SymbolNamesOption GetSymbolNamesOption(
            this AnalyzerOptions options,
            string optionName,
            string namePrefixOpt,
            DiagnosticDescriptor rule,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, rule, TryParse, defaultValue: SymbolNamesOption.Empty);

            // Local functions.
            bool TryParse(string s, out SymbolNamesOption option)
            {
                if (string.IsNullOrEmpty(s))
                {
                    option = SymbolNamesOption.Empty;
                    return false;
                }

                var names = s.Split('|').ToImmutableArray();
                option = SymbolNamesOption.Create(names, compilation, namePrefixOpt);
                return true;
            }
        }

        private static CategorizedAnalyzerConfigOptions GetOrComputeCategorizedAnalyzerConfigOptions(
            this AnalyzerOptions options, CancellationToken cancellationToken)
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists. 
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            var createValueCallback = new ConditionalWeakTable<AnalyzerOptions, CategorizedAnalyzerConfigOptions>.CreateValueCallback(_ => ComputeCategorizedAnalyzerConfigOptions());
            return s_cachedOptions.GetValue(options, createValueCallback);

            // Local functions.
            CategorizedAnalyzerConfigOptions ComputeCategorizedAnalyzerConfigOptions()
            {
                foreach (var additionalFile in options.AdditionalFiles)
                {
                    var fileName = Path.GetFileName(additionalFile.Path);
                    if (fileName.Equals(".editorconfig", StringComparison.OrdinalIgnoreCase))
                    {
                        var text = additionalFile.GetText(cancellationToken);
                        return EditorConfigParser.Parse(text);
                    }
                }

                return CategorizedAnalyzerConfigOptions.Empty;
            }
        }
    }
}
