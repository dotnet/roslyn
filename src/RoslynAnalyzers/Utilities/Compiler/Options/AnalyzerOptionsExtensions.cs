// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Analyzer.Utilities.Extensions;
using Analyzer.Utilities.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
#if !TEST_UTILITIES
    public static partial class AnalyzerOptionsExtensions
#else
    internal static partial class AnalyzerOptionsExtensions
#endif
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, ICategorizedAnalyzerConfigOptions> s_cachedOptions = new();
        private static readonly ImmutableHashSet<OutputKind> s_defaultOutputKinds =
            ImmutableHashSet.CreateRange(Enum.GetValues<OutputKind>());

        private static bool TryGetSyntaxTreeForOption(ISymbol symbol, [NotNullWhen(returnValue: true)] out SyntaxTree? tree)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                case SymbolKind.Namespace when ((INamespaceSymbol)symbol).IsGlobalNamespace:
                    tree = null;
                    return false;
                case SymbolKind.Parameter:
                    return TryGetSyntaxTreeForOption(symbol.ContainingSymbol, out tree);
                default:
                    tree = symbol.Locations[0].SourceTree;
                    return tree != null;
            }
        }

        public static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            SymbolVisibilityGroup defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetSymbolVisibilityGroupOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        private static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            SymbolVisibilityGroup defaultValue)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.ApiSurface, rule, tree, compilation, defaultValue);

        private static SymbolModifiers GetRequiredModifiersOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            SymbolModifiers defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetRequiredModifiersOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        private static SymbolModifiers GetRequiredModifiersOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            SymbolModifiers defaultValue)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.RequiredModifiers, rule, tree, compilation, defaultValue);

        public static EnumValuesPrefixTrigger GetEnumValuesPrefixTriggerOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            EnumValuesPrefixTrigger defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetEnumValuesPrefixTriggerOption(rule, tree, compilation, defaultValue)
            : defaultValue;

        private static EnumValuesPrefixTrigger GetEnumValuesPrefixTriggerOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            EnumValuesPrefixTrigger defaultValue)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.EnumValuesPrefixTrigger, rule, tree, compilation, defaultValue);

        public static ImmutableHashSet<OutputKind> GetOutputKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => AnalyzerOptionsExtensions.GetOutputKindsOption(options, rule, tree, compilation, s_defaultOutputKinds);

        public static ImmutableHashSet<OutputKind> GetOutputKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            ImmutableHashSet<OutputKind> defaultValue)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.OutputKind, rule, tree, compilation, defaultValue);

        public static ImmutableHashSet<SymbolKind> GetAnalyzedSymbolKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            ImmutableHashSet<SymbolKind> defaultSymbolKinds)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetAnalyzedSymbolKindsOption(rule, tree, compilation, defaultSymbolKinds)
            : defaultSymbolKinds;

        private static ImmutableHashSet<SymbolKind> GetAnalyzedSymbolKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            ImmutableHashSet<SymbolKind> defaultSymbolKinds)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.AnalyzedSymbolKinds, rule, tree, compilation, defaultSymbolKinds);

        private static TEnum GetFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            TEnum defaultValue)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(
                optionName, tree, rule,
                tryParseValue: static (value, out result) => Enum.TryParse(value, ignoreCase: true, result: out result),
                defaultValue: defaultValue);
        }

        private static ImmutableHashSet<TEnum> GetNonFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            ImmutableHashSet<TEnum> defaultValue)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, TryParseValue, defaultValue);
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

        private static TEnum GetNonFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            TEnum defaultValue)
            where TEnum : struct
            => GetFlagsEnumOptionValue(options, optionName, rule, tree, compilation, defaultValue);

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? GetBoolOptionValue(options, optionName, rule, tree, compilation, defaultValue)
            : defaultValue;

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor? rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, bool.TryParse, defaultValue);
        }

        public static uint GetUnsignedIntegralOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            uint defaultValue)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, uint.TryParse, defaultValue);
        }

        public static string GetStringOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, TryParseValue, string.Empty);

            static bool TryParseValue(string value, out string result)
            {
                result = value;
                return !string.IsNullOrEmpty(value);
            }
        }

        public static SymbolNamesWithValueOption<Unit> GetNullCheckValidationMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.NullCheckValidationMethods, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "M:");

        public static SymbolNamesWithValueOption<Unit> GetAdditionalStringFormattingMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalStringFormattingMethods, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "M:");

        public static bool IsConfiguredToSkipAnalysis(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation)
            => IsConfiguredToSkipAnalysis(options, rule, symbol, symbol, compilation);

        public static bool IsConfiguredToSkipAnalysis(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            ISymbol containingContextSymbol,
            Compilation compilation)
        {
            var excludedSymbols = GetExcludedSymbolNamesWithValueOption(options, rule, containingContextSymbol, compilation);
            var excludedTypeNamesWithDerivedTypes = GetExcludedTypeNamesWithDerivedTypesOption(options, rule, containingContextSymbol, compilation);
            if (excludedSymbols.IsEmpty && excludedTypeNamesWithDerivedTypes.IsEmpty)
            {
                return false;
            }

            while (symbol != null)
            {
                if (excludedSymbols.Contains(symbol))
                {
                    return true;
                }

                if (symbol is INamedTypeSymbol namedType && !excludedTypeNamesWithDerivedTypes.IsEmpty)
                {
                    foreach (var type in namedType.GetBaseTypesAndThis())
                    {
                        if (excludedTypeNamesWithDerivedTypes.Contains(type))
                        {
                            return true;
                        }
                    }
                }

                symbol = symbol.ContainingSymbol;
            }

            return false;

            static SymbolNamesWithValueOption<Unit> GetExcludedSymbolNamesWithValueOption(
                AnalyzerOptions options,
                DiagnosticDescriptor rule,
                ISymbol symbol,
                Compilation compilation)
                => TryGetSyntaxTreeForOption(symbol, out var tree)
                    ? options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.ExcludedSymbolNames, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default))
                    : SymbolNamesWithValueOption<Unit>.Empty;

            static SymbolNamesWithValueOption<Unit> GetExcludedTypeNamesWithDerivedTypesOption(
                AnalyzerOptions options,
                DiagnosticDescriptor rule,
                ISymbol symbol,
                Compilation compilation)
                => TryGetSyntaxTreeForOption(symbol, out var tree)
                    ? options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.ExcludedTypeNamesWithDerivedTypes, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "T:")
                    : SymbolNamesWithValueOption<Unit>.Empty;
        }

        public static SymbolNamesWithValueOption<Unit> GetDisallowedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation)
        => options.GetDisallowedSymbolNamesWithValueOption(rule, symbol.Locations[0].SourceTree, compilation);

        private static SymbolNamesWithValueOption<Unit> GetDisallowedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree? tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.DisallowedSymbolNames, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default));

        public static SymbolNamesWithValueOption<string?> GetAdditionalRequiredSuffixesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation)
        => options.GetAdditionalRequiredSuffixesOption(rule, symbol.Locations[0].SourceTree, compilation);

        private static SymbolNamesWithValueOption<string?> GetAdditionalRequiredSuffixesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree? tree,
            Compilation compilation)
        {
            return options.GetSymbolNamesWithValueOption(EditorConfigOptionNames.AdditionalRequiredSuffixes, rule, tree, compilation, getTypeAndSuffixFunc: GetParts, namePrefix: "T:");

            static SymbolNamesWithValueOption<string?>.NameParts GetParts(string name)
            {
                var split = name.Split(["->"], StringSplitOptions.RemoveEmptyEntries);

                // If we don't find exactly one '->', we assume that there is no given suffix.
                if (split.Length != 2)
                {
                    return new SymbolNamesWithValueOption<string?>.NameParts(name, null);
                }

                // Note that we do not validate if the suffix will give a valid class name.
                var trimmedSuffix = split[1].Trim();

                // Check if the given suffix is the special suffix symbol "{[ ]*?}" (opening curly brace '{', 0..N spaces and a closing curly brace '}')
                if (trimmedSuffix.Length >= 2 &&
                    trimmedSuffix[0] == '{' &&
                    trimmedSuffix[^1] == '}')
                {
                    for (int i = 1; i < trimmedSuffix.Length - 2; i++)
                    {
                        if (trimmedSuffix[i] != ' ')
                        {
                            return new SymbolNamesWithValueOption<string?>.NameParts(split[0], trimmedSuffix);
                        }
                    }

                    // Replace the special empty suffix symbol by an empty string
                    return new SymbolNamesWithValueOption<string?>.NameParts(split[0], string.Empty);
                }

                return new SymbolNamesWithValueOption<string?>.NameParts(split[0], trimmedSuffix);
            }
        }

        public static SymbolNamesWithValueOption<INamedTypeSymbol?> GetAdditionalRequiredGenericInterfaces(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation)
        => options.GetAdditionalRequiredGenericInterfaces(rule, symbol.Locations[0].SourceTree, compilation);

        private static SymbolNamesWithValueOption<INamedTypeSymbol?> GetAdditionalRequiredGenericInterfaces(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree? tree,
            Compilation compilation)
        {
            return options.GetSymbolNamesWithValueOption(EditorConfigOptionNames.AdditionalRequiredGenericInterfaces, rule, tree, compilation, getTypeAndSuffixFunc: x => GetParts(x, compilation), namePrefix: "T:");

            static SymbolNamesWithValueOption<INamedTypeSymbol?>.NameParts GetParts(string name, Compilation compilation)
            {
                var split = name.Split(["->"], StringSplitOptions.RemoveEmptyEntries);

                // If we don't find exactly one '->', we assume that there is no given suffix.
                if (split.Length != 2)
                {
                    return new SymbolNamesWithValueOption<INamedTypeSymbol?>.NameParts(name, null);
                }

                var genericInterfaceFullName = split[1].Trim();
                if (!genericInterfaceFullName.StartsWith("T:", StringComparison.Ordinal))
                {
                    genericInterfaceFullName = $"T:{genericInterfaceFullName}";
                }

                var matchingSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(genericInterfaceFullName, compilation);

                if (matchingSymbols.Length != 1 ||
                    matchingSymbols[0] is not INamedTypeSymbol namedType ||
                    namedType.TypeKind != TypeKind.Interface ||
                    !namedType.IsGenericType)
                {
                    // Invalid matching type so we assume there was no associated type
                    return new SymbolNamesWithValueOption<INamedTypeSymbol?>.NameParts(split[0], null);
                }

                return new SymbolNamesWithValueOption<INamedTypeSymbol?>.NameParts(split[0], namedType);
            }
        }

        public static SymbolNamesWithValueOption<Unit> GetInheritanceExcludedSymbolNamesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            string defaultForcedValue)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalInheritanceExcludedSymbolNames, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), optionForcedValue: defaultForcedValue);

        public static SymbolNamesWithValueOption<Unit> GetAdditionalUseResultsMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalUseResultsMethods, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "M:");

        public static SymbolNamesWithValueOption<Unit> GetEnumerationMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.EnumerationMethods, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "M:");

        public static SymbolNamesWithValueOption<Unit> GetLinqChainMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.LinqChainMethods, rule, tree, compilation, static name => new SymbolNamesWithValueOption<Unit>.NameParts(name, Unit.Default), namePrefix: "M:");

        private static SymbolNamesWithValueOption<TValue> GetSymbolNamesWithValueOption<TValue>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree? tree,
            Compilation compilation,
            Func<string, SymbolNamesWithValueOption<TValue>.NameParts> getTypeAndSuffixFunc,
            string? namePrefix = null,
            string? optionDefaultValue = null,
            string? optionForcedValue = null)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(
                optionName,
                tree,
                rule,
                TryParse,
                (compilation, getTypeAndSuffixFunc, namePrefix, optionForcedValue),
                defaultValue: GetDefaultValue());

            // Local functions.
            static bool TryParse(string s, (Compilation compilation, Func<string, SymbolNamesWithValueOption<TValue>.NameParts> getTypeAndSuffixFunc, string? namePrefix, string? optionForcedValue) arg, out SymbolNamesWithValueOption<TValue> option)
            {
                var optionValue = s;

                if (!RoslynString.IsNullOrEmpty(arg.optionForcedValue) &&
                    (optionValue == null || !optionValue.Contains(arg.optionForcedValue, StringComparison.Ordinal)))
                {
                    optionValue = $"{arg.optionForcedValue}|{optionValue}";
                }

                if (string.IsNullOrEmpty(optionValue))
                {
                    option = SymbolNamesWithValueOption<TValue>.Empty;
                    return false;
                }

                var names = optionValue.Split(['|'], StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
                option = SymbolNamesWithValueOption<TValue>.Create(names, arg.compilation, arg.namePrefix, arg.getTypeAndSuffixFunc);
                return true;
            }

            SymbolNamesWithValueOption<TValue> GetDefaultValue()
            {
                string optionValue = string.Empty;

                if (!string.IsNullOrEmpty(optionDefaultValue))
                {
                    RoslynDebug.Assert(optionDefaultValue != null);
                    optionValue = optionDefaultValue;
                }

                if (!RoslynString.IsNullOrEmpty(optionForcedValue) &&
                    (optionValue == null || !optionValue.Contains(optionForcedValue, StringComparison.Ordinal)))
                {
                    optionValue = $"{optionForcedValue}|{optionValue}";
                }

                RoslynDebug.Assert(optionValue != null);

                return TryParse(optionValue, (compilation, getTypeAndSuffixFunc, namePrefix, optionForcedValue), out var option)
                    ? option
                    : SymbolNamesWithValueOption<TValue>.Empty;
            }
        }

        public static string? GetMSBuildPropertyValue(
            this AnalyzerOptions options,
            string optionName,
            Compilation compilation)
        {
            MSBuildPropertyOptionNamesHelpers.VerifySupportedPropertyOptionName(optionName);

            // MSBuild property values should be set at compilation level, and cannot have different values per-tree.
            // So, we default to first syntax tree.
            if (compilation.SyntaxTrees.FirstOrDefault() is not { } tree)
            {
                return null;
            }

            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule: null,
                tryParseValue: static (string value, out string? result) =>
                {
                    result = value;
                    return true;
                },
                defaultValue: null, OptionKind.BuildProperty);
        }

        public static ImmutableArray<string> GetMSBuildItemMetadataValues(
            this AnalyzerOptions options,
            string itemOptionName,
            Compilation compilation)
        {
            MSBuildItemOptionNamesHelpers.VerifySupportedItemOptionName(itemOptionName);

            // MSBuild property values should be set at compilation level, and cannot have different values per-tree.
            // So, we default to first syntax tree.
            if (compilation.SyntaxTrees.FirstOrDefault() is not { } tree)
            {
                return ImmutableArray<string>.Empty;
            }

            var propertyOptionName = MSBuildItemOptionNamesHelpers.GetPropertyNameForItemOptionName(itemOptionName);
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation);
            var propertyValue = analyzerConfigOptions.GetOptionValue(propertyOptionName, tree, rule: null,
                tryParseValue: static (string value, out string? result) =>
                {
                    result = value;
                    return true;
                },
                defaultValue: null, OptionKind.BuildProperty);
            return MSBuildItemOptionNamesHelpers.ParseItemOptionValue(propertyValue);
        }

        /// <summary>
        /// Returns true if the given source symbol has required visibility based on options:
        ///   1. If user has explicitly configured candidate <see cref="SymbolVisibilityGroup"/> in editor config options and
        ///      given symbol's visibility is one of the candidate visibilities.
        ///   2. Otherwise, if user has not configured visibility, and given symbol's visibility
        ///      matches the given default symbol visibility.
        /// </summary>
        public static bool MatchesConfiguredVisibility(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            SymbolVisibilityGroup defaultRequiredVisibility = SymbolVisibilityGroup.Public)
            => options.MatchesConfiguredVisibility(rule, symbol, symbol, compilation, defaultRequiredVisibility);

        /// <summary>
        /// Returns true if the given symbol has required visibility based on options in context of the given containing symbol:
        ///   1. If user has explicitly configured candidate <see cref="SymbolVisibilityGroup"/> in editor config options and
        ///      given symbol's visibility is one of the candidate visibilities.
        ///   2. Otherwise, if user has not configured visibility, and given symbol's visibility
        ///      matches the given default symbol visibility.
        /// </summary>
        public static bool MatchesConfiguredVisibility(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            ISymbol containingContextSymbol,
            Compilation compilation,
            SymbolVisibilityGroup defaultRequiredVisibility = SymbolVisibilityGroup.Public)
        {
            var allowedVisibilities = options.GetSymbolVisibilityGroupOption(rule, containingContextSymbol, compilation, defaultRequiredVisibility);
            return allowedVisibilities == SymbolVisibilityGroup.All ||
                allowedVisibilities.Contains(symbol.GetResultantVisibility());
        }

        /// <summary>
        /// Returns true if the given symbol has required symbol modifiers based on options:
        ///   1. If user has explicitly configured candidate <see cref="SymbolModifiers"/> in editor config options and
        ///      given symbol has all the required modifiers.
        ///   2. Otherwise, if user has not configured modifiers.
        /// </summary>
        public static bool MatchesConfiguredModifiers(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            SymbolModifiers defaultRequiredModifiers = SymbolModifiers.None)
        {
            var requiredModifiers = options.GetRequiredModifiersOption(rule, symbol, compilation, defaultRequiredModifiers);
            return symbol.GetSymbolModifiers().Contains(requiredModifiers);
        }

        private static ICategorizedAnalyzerConfigOptions GetOrComputeCategorizedAnalyzerConfigOptions(
            this AnalyzerOptions options, Compilation compilation)
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists.
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            // Fall back to a slow version of the method, which allocates both for the CreateValueCallback and for
            // capturing the Compilation argument.
            return GetOrComputeCategorizedAnalyzerConfigOptions_Slow(options, compilation);

            static ICategorizedAnalyzerConfigOptions GetOrComputeCategorizedAnalyzerConfigOptions_Slow(
                AnalyzerOptions options,
                Compilation compilation)
            {
                return s_cachedOptions.GetValue(
                    options,
                    options => AggregateCategorizedAnalyzerConfigOptions.Create(options.AnalyzerConfigOptionsProvider, compilation));
            }
        }
    }
}
