// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Analyzer configuration options from an .editorconfig file that are parsed into general
    /// and specific configuration options.
    /// 
    /// .editorconfig format:
    ///  1) General configuration option:
    ///     (a) "dotnet_code_quality.OptionName = OptionValue"
    ///  2) Specific configuration option:
    ///     (a) "dotnet_code_quality.RuleId.OptionName = OptionValue"
    ///     (b) "dotnet_code_quality.RuleCategory.OptionName = OptionValue"
    ///    
    /// .editorconfig examples to configure API surface analyzed by analyzers:
    ///  1) General configuration option:
    ///     (a) "dotnet_code_quality.api_surface = all"
    ///  2) Specific configuration option:
    ///     (a) "dotnet_code_quality.CA1040.api_surface = public, internal"
    ///     (b) "dotnet_code_quality.Naming.api_surface = public"
    ///  See <see cref="SymbolVisibilityGroup"/> for allowed symbol visibility value combinations.
    /// </summary>
    internal sealed class CategorizedAnalyzerConfigOptions
    {
        private const string KeyPrefix = "dotnet_code_quality.";

        public static readonly CategorizedAnalyzerConfigOptions Empty = new CategorizedAnalyzerConfigOptions(
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        private CategorizedAnalyzerConfigOptions(
            ImmutableDictionary<string, string> generalOptions,
            ImmutableDictionary<string, ImmutableDictionary<string, string>> specificOptions)
        {
            GeneralOptions = generalOptions;
            SpecificOptions = specificOptions;
        }

        public ImmutableDictionary<string, string> GeneralOptions { get; }
        public ImmutableDictionary<string, ImmutableDictionary<string, string>> SpecificOptions { get; }

        public static CategorizedAnalyzerConfigOptions Create(IDictionary<string, string> options)
        {
            options = options ?? throw new ArgumentNullException(nameof(options));

            if (options.Count == 0)
            {
                return Empty;
            }

            var generalOptionsBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            var specificOptionsBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, string>.Builder>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in options)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                if (!key.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                key = key.Substring(KeyPrefix.Length);
                var keyParts = key.Split('.').Select(s => s.Trim()).ToImmutableArray();
                switch (keyParts.Length)
                {
                    case 1:
                        generalOptionsBuilder.Add(keyParts[0], value);
                        break;

                    case 2:
                        if (!specificOptionsBuilder.TryGetValue(keyParts[0], out var optionsForKeyBuilder))
                        {
                            optionsForKeyBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
                            specificOptionsBuilder.Add(keyParts[0], optionsForKeyBuilder);
                        }

                        optionsForKeyBuilder[keyParts[1]] = value;
                        break;
                }
            }

            var generalOptions = generalOptionsBuilder.Count > 0 ?
                generalOptionsBuilder.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) :
                ImmutableDictionary<string, string>.Empty;

            var specificOptions = specificOptionsBuilder.Count > 0 ?
                specificOptionsBuilder
                    .Select(kvp => new KeyValuePair<string, ImmutableDictionary<string, string>>(kvp.Key, kvp.Value.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase)))
                    .ToImmutableDictionary(StringComparer.OrdinalIgnoreCase) :
                ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty;

            return new CategorizedAnalyzerConfigOptions(generalOptions, specificOptions);
        }

        public TEnum GetEnumOptionValue<TEnum>(string optionName, DiagnosticDescriptor rule, TEnum defaultValue)
            where TEnum : struct
        {
            if (TryGetSpecificOptionValue(rule.Id, out var optionValue) ||
                TryGetSpecificOptionValue(rule.Category, out optionValue) ||
                TryGetGeneralOptionValue(out optionValue))
            {
                return optionValue;
            }

            return defaultValue;

            // Local functions.
            bool TryGetSpecificOptionValue(string specificOptionKey, out TEnum specificOptionValue)
            {
                if (SpecificOptions.TryGetValue(specificOptionKey, out var specificRuleOptions) &&
                    specificRuleOptions.TryGetValue(optionName, out var valueString))
                {
                    return TryParse(valueString, out specificOptionValue);
                }

                specificOptionValue = defaultValue;
                return false;
            }

            bool TryGetGeneralOptionValue(out TEnum generalOptionValue)
            {
                if (GeneralOptions.TryGetValue(optionName, out var valueString))
                {
                    return TryParse(valueString, out generalOptionValue);
                }

                generalOptionValue = defaultValue;
                return false;
            }

            bool TryParse(string value, out TEnum result)
                => Enum.TryParse(value, ignoreCase: true, result: out result);
        }

        public int GetIntegralOptionValue(string optionName, DiagnosticDescriptor rule, int defaultValue)
        {
            if (TryGetSpecificOptionValue(rule.Id, out var optionValue) ||
                TryGetSpecificOptionValue(rule.Category, out optionValue) ||
                TryGetGeneralOptionValue(out optionValue))
            {
                return optionValue;
            }

            return defaultValue;

            // Local functions.
            bool TryGetSpecificOptionValue(string specificOptionKey, out int specificOptionValue)
            {
                if (SpecificOptions.TryGetValue(specificOptionKey, out var specificRuleOptions) &&
                    specificRuleOptions.TryGetValue(optionName, out var valueString))
                {
                    return int.TryParse(valueString, out specificOptionValue);
                }

                specificOptionValue = defaultValue;
                return false;
            }

            bool TryGetGeneralOptionValue(out int generalOptionValue)
            {
                if (GeneralOptions.TryGetValue(optionName, out var valueString))
                {
                    return int.TryParse(valueString, out generalOptionValue);
                }

                generalOptionValue = defaultValue;
                return false;
            }
        }
    }
}
