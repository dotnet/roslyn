// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Analyzer.Utilities
{
    using static CategorizedAnalyzerConfigOptionsExtensions;

    internal abstract class AbstractCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        protected const string KeyPrefix = "dotnet_code_quality.";

        private readonly ConcurrentDictionary<string, (bool found, object? value)> _computedOptionValuesMap;

        protected AbstractCategorizedAnalyzerConfigOptions()
        {
            _computedOptionValuesMap = new ConcurrentDictionary<string, (bool found, object? value)>();
        }

        public abstract bool IsEmpty { get; }
        protected abstract bool TryGetOptionValue(string optionKeyPrefix, string? optionKeySuffix, string optionName, [NotNullWhen(returnValue: true)] out string? valueString);

        public T GetOptionValue<T>(string optionName, SyntaxTree tree, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, T defaultValue)
        {
            if (TryGetOptionValue(optionName, rule, tryParseValue, defaultValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public bool TryGetOptionValue<T>(string optionName, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, T defaultValue, out T value)
        {
            if (this.IsEmpty)
            {
                value = defaultValue;
                return false;
            }

            var key = $"{rule.Id}.{optionName}";
            if (!_computedOptionValuesMap.TryGetValue(key, out var computedValue))
            {
                computedValue = _computedOptionValuesMap.GetOrAdd(key, _ => ComputeOptionValue(optionName, rule, tryParseValue, defaultValue));
            }

            value = (T)computedValue.value!;
            return computedValue.found;
        }

        private (bool found, object? value) ComputeOptionValue<T>(string optionName, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, T defaultValue)
        {
            if (TryGetSpecificOptionValue(rule.Id, out var optionValue) ||
                TryGetSpecificOptionValue(rule.Category, out optionValue) ||
                TryGetAnySpecificOptionValue(rule.CustomTags, out optionValue) ||
                TryGetGeneralOptionValue(out optionValue))
            {
                return (true, optionValue);
            }

            return (false, defaultValue);

            // Local functions.
            bool TryGetSpecificOptionValue(string specificOptionKey, out T specificOptionValue)
            {
                if (TryGetOptionValue(KeyPrefix, specificOptionKey, optionName, out var valueString))
                {
                    return tryParseValue(valueString, out specificOptionValue);
                }

                specificOptionValue = defaultValue;
                return false;
            }

            bool TryGetAnySpecificOptionValue(IEnumerable<string> specificOptionKeys, out T specificOptionValue)
            {
                foreach (var specificOptionKey in specificOptionKeys)
                {
                    if (TryGetSpecificOptionValue(specificOptionKey, out specificOptionValue))
                    {
                        return true;
                    }
                }

                specificOptionValue = defaultValue;
                return false;
            }

            bool TryGetGeneralOptionValue(out T generalOptionValue)
            {
                if (TryGetOptionValue(KeyPrefix, optionKeySuffix: null, optionName, out var valueString))
                {
                    return tryParseValue(valueString, out generalOptionValue);
                }

                generalOptionValue = defaultValue;
                return false;
            }
        }
    }
}