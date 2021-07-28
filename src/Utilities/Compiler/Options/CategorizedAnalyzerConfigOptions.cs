// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    internal sealed class CategorizedAnalyzerConfigOptions : AbstractCategorizedAnalyzerConfigOptions
    {
        public static readonly CategorizedAnalyzerConfigOptions Empty = new(
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        private readonly ImmutableDictionary<string, string> _generalOptions;
        private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> _specificOptions;

        private CategorizedAnalyzerConfigOptions(
            ImmutableDictionary<string, string> generalOptions,
            ImmutableDictionary<string, ImmutableDictionary<string, string>> specificOptions)
        {
            _generalOptions = generalOptions;
            _specificOptions = specificOptions;
        }

        public override bool IsEmpty
        {
            get
            {
                Debug.Assert(ReferenceEquals(this, Empty) || _generalOptions.Count > 0 || _specificOptions.Count > 0);
                return ReferenceEquals(this, Empty);
            }
        }

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

            if (generalOptionsBuilder.Count == 0 && specificOptionsBuilder.Count == 0)
            {
                return Empty;
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

        protected override bool TryGetOptionValue(string optionKeyPrefix, string? optionKeySuffix, string optionName, [NotNullWhen(returnValue: true)] out string? valueString)
        {
            if (optionKeySuffix != null)
            {
                valueString = null;
                return _specificOptions.TryGetValue(optionKeySuffix, out var specificRuleOptions) &&
                    specificRuleOptions.TryGetValue(optionName, out valueString);
            }

            return _generalOptions.TryGetValue(optionName, out valueString);
        }
    }
}
