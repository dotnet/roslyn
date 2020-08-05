// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Analyzer.Utilities.Options;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable RS1012 // Start action has no registered actions.

namespace Analyzer.Utilities
{
    internal static partial class AnalyzerOptionsExtensions
    {
        private static readonly ConditionalWeakTable<AnalyzerOptions, ICategorizedAnalyzerConfigOptions> s_cachedOptions
            = new ConditionalWeakTable<AnalyzerOptions, ICategorizedAnalyzerConfigOptions>();
        private static readonly ImmutableHashSet<OutputKind> s_defaultOutputKinds =
            ImmutableHashSet.CreateRange(Enum.GetValues(typeof(OutputKind)).Cast<OutputKind>());

        private static bool TryGetSyntaxTreeForOption(ISymbol symbol, [NotNullWhen(returnValue: true)] out SyntaxTree? tree)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Assembly:
                case SymbolKind.Namespace when ((INamespaceSymbol)symbol).IsGlobalNamespace:
                    tree = null;
                    return false;
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
            SymbolVisibilityGroup defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetSymbolVisibilityGroupOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static SymbolVisibilityGroup GetSymbolVisibilityGroupOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            SymbolVisibilityGroup defaultValue,
            CancellationToken cancellationToken)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.ApiSurface, rule, tree, compilation, defaultValue, cancellationToken);

        public static SymbolModifiers GetRequiredModifiersOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            SymbolModifiers defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetRequiredModifiersOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static SymbolModifiers GetRequiredModifiersOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            SymbolModifiers defaultValue,
            CancellationToken cancellationToken)
            => options.GetFlagsEnumOptionValue(EditorConfigOptionNames.RequiredModifiers, rule, tree, compilation, defaultValue, cancellationToken);

        public static EnumValuesPrefixTrigger GetEnumValuesPrefixTriggerOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            EnumValuesPrefixTrigger defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetEnumValuesPrefixTriggerOption(rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static EnumValuesPrefixTrigger GetEnumValuesPrefixTriggerOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            EnumValuesPrefixTrigger defaultValue,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.EnumValuesPrefixTrigger, rule, tree, compilation, defaultValue, cancellationToken);

        public static ImmutableHashSet<OutputKind> GetOutputKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.OutputKind, rule, tree, compilation, s_defaultOutputKinds, cancellationToken);

        public static ImmutableHashSet<SymbolKind> GetAnalyzedSymbolKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            ImmutableHashSet<SymbolKind> defaultSymbolKinds,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetAnalyzedSymbolKindsOption(rule, tree, compilation, defaultSymbolKinds, cancellationToken)
            : defaultSymbolKinds;

        public static ImmutableHashSet<SymbolKind> GetAnalyzedSymbolKindsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            ImmutableHashSet<SymbolKind> defaultSymbolKinds,
            CancellationToken cancellationToken)
            => options.GetNonFlagsEnumOptionValue(EditorConfigOptionNames.AnalyzedSymbolKinds, rule, tree, compilation, defaultSymbolKinds, cancellationToken);

        private static TEnum GetFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(
                optionName, tree, rule,
                tryParseValue: (string value, out TEnum result) => Enum.TryParse(value, ignoreCase: true, result: out result),
                defaultValue: defaultValue);
        }

        private static ImmutableHashSet<TEnum> GetNonFlagsEnumOptionValue<TEnum>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            ImmutableHashSet<TEnum> defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
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

#pragma warning disable IDE0051 // Remove unused private members - Used in some projects that include this shared project.
        private static TEnum GetNonFlagsEnumOptionValue<TEnum>(
#pragma warning restore IDE0051 // Remove unused private members
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            TEnum defaultValue,
            CancellationToken cancellationToken)
            where TEnum : struct
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(
                optionName, tree, rule,
                tryParseValue: (string value, out TEnum result) => Enum.TryParse(value, ignoreCase: true, result: out result),
                defaultValue: defaultValue);
        }

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetBoolOptionValue(optionName, rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static bool GetBoolOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            bool defaultValue,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, bool.TryParse, defaultValue);
        }

        public static uint GetUnsignedIntegralOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            uint defaultValue,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetUnsignedIntegralOptionValue(optionName, rule, tree, compilation, defaultValue, cancellationToken)
            : defaultValue;

        public static uint GetUnsignedIntegralOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            uint defaultValue,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, uint.TryParse, defaultValue);
        }

        public static string GetStringOptionValue(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
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
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.NullCheckValidationMethods, rule, tree, compilation, cancellationToken, namePrefix: "M:");

        public static SymbolNamesWithValueOption<Unit> GetAdditionalStringFormattingMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalStringFormattingMethods, rule, tree, compilation, cancellationToken, namePrefix: "M:");

        public static SymbolNamesWithValueOption<Unit> GetExcludedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetExcludedSymbolNamesWithValueOption(rule, tree, compilation, cancellationToken)
            : SymbolNamesWithValueOption<Unit>.Empty;

        public static SymbolNamesWithValueOption<Unit> GetExcludedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.ExcludedSymbolNames, rule, tree, compilation, cancellationToken);

        public static SymbolNamesWithValueOption<Unit> GetExcludedTypeNamesWithDerivedTypesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        => TryGetSyntaxTreeForOption(symbol, out var tree)
            ? options.GetExcludedTypeNamesWithDerivedTypesOption(rule, tree, compilation, cancellationToken)
            : SymbolNamesWithValueOption<Unit>.Empty;

        public static SymbolNamesWithValueOption<Unit> GetExcludedTypeNamesWithDerivedTypesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.ExcludedTypeNamesWithDerivedTypes, rule, tree, compilation, cancellationToken, namePrefix: "T:");

        public static SymbolNamesWithValueOption<Unit> GetDisallowedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        => options.GetDisallowedSymbolNamesWithValueOption(rule, symbol.Locations[0].SourceTree, compilation, cancellationToken);

        public static SymbolNamesWithValueOption<Unit> GetDisallowedSymbolNamesWithValueOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.DisallowedSymbolNames, rule, tree, compilation, cancellationToken);

        public static SymbolNamesWithValueOption<string> GetAdditionalRequiredSuffixesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        => options.GetAdditionalRequiredSuffixesOption(rule, symbol.Locations[0].SourceTree, compilation, cancellationToken);

        public static SymbolNamesWithValueOption<string> GetAdditionalRequiredSuffixesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            return options.GetSymbolNamesWithValueOption(EditorConfigOptionNames.AdditionalRequiredSuffixes, rule, tree, compilation, cancellationToken, namePrefix: "T:", getTypeAndSuffixFunc: GetParts);

            static SymbolNamesWithValueOption<string>.NameParts GetParts(string name)
            {
                var split = name.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

                // If we don't find exactly one '->', we assume that there is no given suffix.
                if (split.Length != 2)
                {
                    return new SymbolNamesWithValueOption<string>.NameParts(name);
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
                            return new SymbolNamesWithValueOption<string>.NameParts(split[0], trimmedSuffix);
                        }
                    }

                    // Replace the special empty suffix symbol by an empty string
                    return new SymbolNamesWithValueOption<string>.NameParts(split[0], string.Empty);
                }

                return new SymbolNamesWithValueOption<string>.NameParts(split[0], trimmedSuffix);
            }
        }

        public static SymbolNamesWithValueOption<INamedTypeSymbol> GetAdditionalRequiredGenericInterfaces(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            ISymbol symbol,
            Compilation compilation,
            CancellationToken cancellationToken)
        => options.GetAdditionalRequiredGenericInterfaces(rule, symbol.Locations[0].SourceTree, compilation, cancellationToken);

        public static SymbolNamesWithValueOption<INamedTypeSymbol> GetAdditionalRequiredGenericInterfaces(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            return options.GetSymbolNamesWithValueOption(EditorConfigOptionNames.AdditionalRequiredGenericInterfaces, rule, tree, compilation, cancellationToken, namePrefix: "T:", getTypeAndSuffixFunc: x => GetParts(x, compilation));

            static SymbolNamesWithValueOption<INamedTypeSymbol>.NameParts GetParts(string name, Compilation compilation)
            {
                var split = name.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);

                // If we don't find exactly one '->', we assume that there is no given suffix.
                if (split.Length != 2)
                {
                    return new SymbolNamesWithValueOption<INamedTypeSymbol>.NameParts(name);
                }

                var genericInterfaceFullName = split[1].Trim();
                if (!genericInterfaceFullName.StartsWith("T:", StringComparison.Ordinal))
                {
                    genericInterfaceFullName = $"T:{genericInterfaceFullName}";
                }

                var matchingSymbols = DocumentationCommentId.GetSymbolsForDeclarationId(genericInterfaceFullName, compilation);

                if (matchingSymbols.Length != 1 ||
                    !(matchingSymbols[0] is INamedTypeSymbol namedType) ||
                    namedType.TypeKind != TypeKind.Interface ||
                    !namedType.IsGenericType)
                {
                    // Invalid matching type so we assume there was no associated type
                    return new SymbolNamesWithValueOption<INamedTypeSymbol>.NameParts(split[0]);
                }

                return new SymbolNamesWithValueOption<INamedTypeSymbol>.NameParts(split[0], namedType);
            }
        }

        public static SymbolNamesWithValueOption<Unit> GetInheritanceExcludedSymbolNamesOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            string defaultForcedValue,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalInheritanceExcludedSymbolNames, rule, tree, compilation, cancellationToken, optionForcedValue: defaultForcedValue);

        public static SymbolNamesWithValueOption<Unit> GetAdditionalUseResultsMethodsOption(
            this AnalyzerOptions options,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken)
            => options.GetSymbolNamesWithValueOption<Unit>(EditorConfigOptionNames.AdditionalUseResultsMethods, rule, tree, compilation, cancellationToken, namePrefix: "M:");

        private static SymbolNamesWithValueOption<TValue> GetSymbolNamesWithValueOption<TValue>(
            this AnalyzerOptions options,
            string optionName,
            DiagnosticDescriptor rule,
            SyntaxTree tree,
            Compilation compilation,
            CancellationToken cancellationToken,
            string? namePrefix = null,
            string? optionDefaultValue = null,
            string? optionForcedValue = null,
            Func<string, SymbolNamesWithValueOption<TValue>.NameParts>? getTypeAndSuffixFunc = null)
            where TValue : notnull
        {
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule, TryParse, defaultValue: GetDefaultValue());

            // Local functions.
            bool TryParse(string s, out SymbolNamesWithValueOption<TValue> option)
            {
                var optionValue = s;

                if (!RoslynString.IsNullOrEmpty(optionForcedValue) &&
                    (optionValue == null || !optionValue.Contains(optionForcedValue, StringComparison.Ordinal)))
                {
                    optionValue = $"{optionForcedValue}|{optionValue}";
                }

                if (string.IsNullOrEmpty(optionValue))
                {
                    option = SymbolNamesWithValueOption<TValue>.Empty;
                    return false;
                }

                var names = optionValue.Split('|').ToImmutableArray();
                option = SymbolNamesWithValueOption<TValue>.Create(names, compilation, namePrefix, getTypeAndSuffixFunc);
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

                return TryParse(optionValue, out var option)
                    ? option
                    : SymbolNamesWithValueOption<TValue>.Empty;
            }
        }

        public static string? GetMSBuildPropertyValue(
            this AnalyzerOptions options,
            string optionName,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            MSBuildPropertyOptionNamesHelpers.VerifySupportedPropertyOptionName(optionName);

            // MSBuild property values should be set at compilation level, and cannot have different values per-tree.
            // So, we default to first syntax tree.
            if (!(compilation.SyntaxTrees.FirstOrDefault() is { } tree))
            {
                return null;
            }

            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            return analyzerConfigOptions.GetOptionValue(optionName, tree, rule: null,
                tryParseValue: (string value, out string? result) => { result = value; return true; },
                defaultValue: null, OptionKind.BuildProperty);
        }

        public static ImmutableArray<string> GetMSBuildItemMetadataValues(
            this AnalyzerOptions options,
            string itemOptionName,
            Compilation compilation,
            CancellationToken cancellationToken)
        {
            MSBuildItemOptionNamesHelpers.VerifySupportedItemOptionName(itemOptionName);

            // MSBuild property values should be set at compilation level, and cannot have different values per-tree.
            // So, we default to first syntax tree.
            if (!(compilation.SyntaxTrees.FirstOrDefault() is { } tree))
            {
                return ImmutableArray<string>.Empty;
            }

            var propertyOptionName = MSBuildItemOptionNamesHelpers.GetPropertyNameForItemOptionName(itemOptionName);
            var analyzerConfigOptions = options.GetOrComputeCategorizedAnalyzerConfigOptions(compilation, cancellationToken);
            var propertyValue = analyzerConfigOptions.GetOptionValue(propertyOptionName, tree, rule: null,
                tryParseValue: (string value, out string? result) => { result = value; return true; },
                defaultValue: null, OptionKind.BuildProperty);
            return MSBuildItemOptionNamesHelpers.ParseItemOptionValue(propertyValue);
        }

#pragma warning disable CA1801 // Review unused parameters - 'compilation' is used conditionally.
        private static ICategorizedAnalyzerConfigOptions GetOrComputeCategorizedAnalyzerConfigOptions(
            this AnalyzerOptions options, Compilation compilation, CancellationToken cancellationToken)
#pragma warning restore CA1801 // Review unused parameters
        {
            // TryGetValue upfront to avoid allocating createValueCallback if the entry already exists.
            if (s_cachedOptions.TryGetValue(options, out var categorizedAnalyzerConfigOptions))
            {
                return categorizedAnalyzerConfigOptions;
            }

            var createValueCallback = new ConditionalWeakTable<AnalyzerOptions, ICategorizedAnalyzerConfigOptions>.CreateValueCallback(_ => ComputeCategorizedAnalyzerConfigOptions());
            return s_cachedOptions.GetValue(options, createValueCallback);

            // Local functions.
            ICategorizedAnalyzerConfigOptions ComputeCategorizedAnalyzerConfigOptions()
            {
                var categorizedOptionsFromAdditionalFiles = ComputeCategorizedAnalyzerConfigOptionsFromAdditionalFiles();

#if CODEANALYSIS_V3_OR_BETTER
                return AggregateCategorizedAnalyzerConfigOptions.Create(options.AnalyzerConfigOptionsProvider, compilation, categorizedOptionsFromAdditionalFiles);
#else
                return categorizedOptionsFromAdditionalFiles;
#endif
            }

            CompilationCategorizedAnalyzerConfigOptions ComputeCategorizedAnalyzerConfigOptionsFromAdditionalFiles()
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

                return CompilationCategorizedAnalyzerConfigOptions.Empty;
            }
        }
    }
}
