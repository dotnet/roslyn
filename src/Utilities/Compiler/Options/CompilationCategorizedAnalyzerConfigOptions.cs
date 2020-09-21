// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Analyzer.Utilities
{
    /// <inheritdoc cref="ICategorizedAnalyzerConfigOptions"/>
    internal sealed class CompilationCategorizedAnalyzerConfigOptions : AbstractCategorizedAnalyzerConfigOptions
    {
        public static readonly CompilationCategorizedAnalyzerConfigOptions Empty = new CompilationCategorizedAnalyzerConfigOptions(
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<string, ImmutableDictionary<string, string>>.Empty);

        private readonly ImmutableDictionary<string, string> _generalOptions;
        private readonly ImmutableDictionary<string, ImmutableDictionary<string, string>> _specificOptions;

        private CompilationCategorizedAnalyzerConfigOptions(
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
                Debug.Assert(ReferenceEquals(this, Empty) || !_generalOptions.IsEmpty || !_specificOptions.IsEmpty);
                return ReferenceEquals(this, Empty);
            }
        }

        public static CompilationCategorizedAnalyzerConfigOptions Create(IDictionary<string, string> options)
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

                if (!HasSupportedKeyPrefix(key, out var keyPrefix))
                {
                    continue;
                }

                key = key[keyPrefix.Length..];
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

            return new CompilationCategorizedAnalyzerConfigOptions(generalOptions, specificOptions);
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
