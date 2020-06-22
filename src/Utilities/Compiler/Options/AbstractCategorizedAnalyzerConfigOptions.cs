// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    using static CategorizedAnalyzerConfigOptionsExtensions;

    internal abstract class AbstractCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        private const string DotnetCodeQualityKeyPrefix = "dotnet_code_quality.";
        private const string BuildPropertyKeyPrefix = "build_property.";

        private readonly ConcurrentDictionary<string, (bool found, object? value)> _computedOptionValuesMap;

        protected AbstractCategorizedAnalyzerConfigOptions()
        {
            _computedOptionValuesMap = new ConcurrentDictionary<string, (bool found, object? value)>();
        }

        public abstract bool IsEmpty { get; }
        protected abstract bool TryGetOptionValue(string optionKeyPrefix, string? optionKeySuffix, string optionName, [NotNullWhen(returnValue: true)] out string? valueString);

        [return: MaybeNull, NotNullIfNotNull("defaultValue")]
        public T/*??*/ GetOptionValue<T>(string optionName, SyntaxTree tree, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, [MaybeNull] T/*??*/ defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            if (TryGetOptionValue(optionName, kind, rule, tryParseValue, defaultValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        private static string MapOptionKindToKeyPrefix(OptionKind optionKind)
            => optionKind switch
            {
                OptionKind.DotnetCodeQuality => DotnetCodeQualityKeyPrefix,
                OptionKind.BuildProperty => BuildPropertyKeyPrefix,
                _ => throw new NotImplementedException()
            };

        protected static bool HasSupportedKeyPrefix(string key, [NotNullWhen(returnValue: true)] out string? keyPrefix)
        {
            if (key.StartsWith(DotnetCodeQualityKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                keyPrefix = DotnetCodeQualityKeyPrefix;
                return true;
            }

            if (key.StartsWith(BuildPropertyKeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                keyPrefix = BuildPropertyKeyPrefix;
                return true;
            }

            keyPrefix = null;
            return false;
        }

        public bool TryGetOptionValue<T>(string optionName, OptionKind kind, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, [MaybeNull] T/*??*/ defaultValue, [MaybeNullWhen(false), NotNullIfNotNull("defaultValue")] out T value)
        {
            if (this.IsEmpty)
            {
                value = defaultValue;
                return false;
            }

            var key = $"{rule.Id}.{optionName}";
            if (!_computedOptionValuesMap.TryGetValue(key, out var computedValue))
            {
                computedValue = _computedOptionValuesMap.GetOrAdd(key, _ => ComputeOptionValue(optionName, kind, rule, tryParseValue));
            }

            if (computedValue.found)
            {
                value = (T)computedValue.value!;
                return true;
            }
            else
            {
                value = defaultValue;
                return false;
            }
        }

        private (bool found, object? value) ComputeOptionValue<T>(string optionName, OptionKind kind, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue)
        {
            var optionKeyPrefix = MapOptionKindToKeyPrefix(kind);
            if (TryGetSpecificOptionValue(rule.Id, optionKeyPrefix, out var optionValue) ||
                TryGetSpecificOptionValue(rule.Category, optionKeyPrefix, out optionValue) ||
                TryGetAnySpecificOptionValue(rule.CustomTags, optionKeyPrefix, out optionValue) ||
                TryGetGeneralOptionValue(optionKeyPrefix, out optionValue))
            {
                return (true, optionValue);
            }

            return (false, null);

            // Local functions.
            bool TryGetSpecificOptionValue(string specificOptionKey, string optionKeyPrefix, out T specificOptionValue)
            {
                if (TryGetOptionValue(optionKeyPrefix, specificOptionKey, optionName, out var valueString))
                {
#pragma warning disable CS8601 // Possible null reference assignment - Once local function attributes are supported, add "[MaybeNull]" on 'specificOptionValue'.
                    return tryParseValue(valueString, out specificOptionValue);
#pragma warning restore CS8601 // Possible null reference assignment.
                }

#pragma warning disable CS8601 // Possible null reference assignment - Once local function attributes are supported, add "[MaybeNull]" on 'specificOptionValue'.
                specificOptionValue = default;
#pragma warning restore CS8601 // Possible null reference assignment.
                return false;
            }

            bool TryGetAnySpecificOptionValue(IEnumerable<string> specificOptionKeys, string optionKeyPrefix, out T specificOptionValue)
            {
                foreach (var specificOptionKey in specificOptionKeys)
                {
                    if (TryGetSpecificOptionValue(specificOptionKey, optionKeyPrefix, out specificOptionValue))
                    {
                        return true;
                    }
                }

#pragma warning disable CS8601 // Possible null reference assignment - Once local function attributes are supported, add "[MaybeNull]" on 'specificOptionValue'.
                specificOptionValue = default;
#pragma warning restore CS8601 // Possible null reference assignment.
                return false;
            }

            bool TryGetGeneralOptionValue(string optionKeyPrefix, out T generalOptionValue)
            {
                if (TryGetOptionValue(optionKeyPrefix, optionKeySuffix: null, optionName, out var valueString))
                {
#pragma warning disable CS8601 // Possible null reference assignment - Once local function attributes are supported, add "[MaybeNull]" on 'generalOptionValue'.
                    return tryParseValue(valueString, out generalOptionValue);
#pragma warning restore CS8601 // Possible null reference assignment.
                }

#pragma warning disable CS8601 // Possible null reference assignment - Once local function attributes are supported, add "[MaybeNull]" on 'generalOptionValue'.
                generalOptionValue = default;
#pragma warning restore CS8601 // Possible null reference assignment.
                return false;
            }
        }
    }
}