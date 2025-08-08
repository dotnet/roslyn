// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    using static CategorizedAnalyzerConfigOptionsExtensions;

    internal abstract class AbstractCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        private const string DotnetCodeQualityKeyPrefix = "dotnet_code_quality.";
        private const string BuildPropertyKeyPrefix = "build_property.";

        private readonly ConcurrentDictionary<OptionKey, (bool found, object? value)> _computedOptionValuesMap;

        protected AbstractCategorizedAnalyzerConfigOptions()
        {
            _computedOptionValuesMap = new ConcurrentDictionary<OptionKey, (bool found, object? value)>();
        }

        public abstract bool IsEmpty { get; }
        protected abstract bool TryGetOptionValue(string optionKeyPrefix, string? optionKeySuffix, string optionName, [NotNullWhen(returnValue: true)] out string? valueString);

        public T GetOptionValue<T>(string optionName, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T> tryParseValue, T defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            if (TryGetOptionValue(
                optionName,
                kind,
                rule,
                static (s, tryParseValue, [MaybeNullWhen(returnValue: false)] out parsedValue) => tryParseValue(s, out parsedValue),
                tryParseValue,
                defaultValue,
                out var value))
            {
                return value;
            }

            return defaultValue;
        }

        public T GetOptionValue<T, TArg>(string optionName, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T, TArg> tryParseValue, TArg arg, T defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            if (TryGetOptionValue(optionName, kind, rule, tryParseValue, arg, defaultValue, out var value))
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

        [PerformanceSensitive("https://github.com/dotnet/roslyn-analyzers/issues/4905", AllowCaptures = false)]
        public bool TryGetOptionValue<T, TArg>(string optionName, OptionKind kind, DiagnosticDescriptor? rule, TryParseValue<T, TArg> tryParseValue, TArg arg, T defaultValue, out T value)
        {
            if (this.IsEmpty)
            {
                value = defaultValue;
                return false;
            }

            var key = OptionKey.GetOrCreate(rule?.Id, optionName);
            if (!_computedOptionValuesMap.TryGetValue(key, out var computedValue))
            {
                computedValue = _computedOptionValuesMap.GetOrAdd(key, ComputeOptionValue(optionName, kind, rule, tryParseValue, arg));
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

        private (bool found, object? value) ComputeOptionValue<T, TArg>(string optionName, OptionKind kind, DiagnosticDescriptor? rule, TryParseValue<T, TArg> tryParseValue, TArg arg)
        {
            var optionKeyPrefix = MapOptionKindToKeyPrefix(kind);

            if (rule != null
                && (TryGetSpecificOptionValue(rule.Id, optionKeyPrefix, out T? optionValue)
                || TryGetSpecificOptionValue(rule.Category, optionKeyPrefix, out optionValue)
                || TryGetAnySpecificOptionValue(rule.CustomTags, optionKeyPrefix, out optionValue)))
            {
                return (true, optionValue);
            }

            if (TryGetGeneralOptionValue(optionKeyPrefix, out optionValue))
            {
                return (true, optionValue);
            }

            return (false, null);

            // Local functions.
            bool TryGetSpecificOptionValue(string specificOptionKey, string optionKeyPrefix, [MaybeNullWhen(false)] out T specificOptionValue)
            {
                if (TryGetOptionValue(optionKeyPrefix, specificOptionKey, optionName, out var valueString))
                {
                    return tryParseValue(valueString, arg, out specificOptionValue);
                }

                specificOptionValue = default;
                return false;
            }

            bool TryGetAnySpecificOptionValue(IEnumerable<string> specificOptionKeys, string optionKeyPrefix, [MaybeNullWhen(false)] out T specificOptionValue)
            {
                foreach (var specificOptionKey in specificOptionKeys)
                {
                    if (TryGetSpecificOptionValue(specificOptionKey, optionKeyPrefix, out specificOptionValue))
                    {
                        return true;
                    }
                }

                specificOptionValue = default;
                return false;
            }

            bool TryGetGeneralOptionValue(string optionKeyPrefix, [MaybeNullWhen(false)] out T generalOptionValue)
            {
                if (TryGetOptionValue(optionKeyPrefix, optionKeySuffix: null, optionName, out var valueString))
                {
                    return tryParseValue(valueString, arg, out generalOptionValue);
                }

                generalOptionValue = default;
                return false;
            }
        }
    }
}
