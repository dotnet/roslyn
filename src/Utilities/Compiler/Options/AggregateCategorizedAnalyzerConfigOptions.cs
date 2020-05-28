// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if CODEANALYSIS_V3_OR_BETTER

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Utilities
{
    using static CategorizedAnalyzerConfigOptionsExtensions;

    /// <summary>
    /// Aggregate analyzer configuration options:
    /// 1. Per syntax tree options from <see cref="AnalyzerConfigOptionsProvider"/>.
    /// 2. Options from an .editorconfig file passed in as an additional file (back compat).
    /// 
    /// These options are parsed into general and specific configuration options.
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
    internal sealed class AggregateCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        public static readonly AggregateCategorizedAnalyzerConfigOptions Empty = new AggregateCategorizedAnalyzerConfigOptions(
            ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>>.Empty,
            CompilationCategorizedAnalyzerConfigOptions.Empty);

        private readonly ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> _perTreeOptions;
        private readonly CompilationCategorizedAnalyzerConfigOptions _additionalFileBasedOptions;

        private AggregateCategorizedAnalyzerConfigOptions(ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> perTreeOptions, CompilationCategorizedAnalyzerConfigOptions additionalFileBasedOptions)
        {
            _perTreeOptions = perTreeOptions;
            _additionalFileBasedOptions = additionalFileBasedOptions;
        }

        public bool IsEmpty
        {
            get
            {
                Debug.Assert(ReferenceEquals(this, Empty) || !_perTreeOptions.IsEmpty || !_additionalFileBasedOptions.IsEmpty);
                return ReferenceEquals(this, Empty);
            }
        }

        public static AggregateCategorizedAnalyzerConfigOptions Create(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, Compilation compilation, CompilationCategorizedAnalyzerConfigOptions additionalFileBasedOptions)
        {
            analyzerConfigOptionsProvider = analyzerConfigOptionsProvider ?? throw new ArgumentNullException(nameof(analyzerConfigOptionsProvider));
            additionalFileBasedOptions = additionalFileBasedOptions ?? throw new ArgumentNullException(nameof(additionalFileBasedOptions));

            if (analyzerConfigOptionsProvider.IsEmpty() && additionalFileBasedOptions.IsEmpty)
            {
                return Empty;
            }

            var perTreeOptionsBuilder = PooledDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>>.GetInstance();
            foreach (var tree in compilation.SyntaxTrees)
            {
                perTreeOptionsBuilder.Add(tree, new Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>(() => Create(tree, analyzerConfigOptionsProvider)));
            }

            return new AggregateCategorizedAnalyzerConfigOptions(perTreeOptionsBuilder.ToImmutableDictionaryAndFree(), additionalFileBasedOptions);

            static SyntaxTreeCategorizedAnalyzerConfigOptions Create(SyntaxTree tree, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
            {
                var options = analyzerConfigOptionsProvider.GetOptions(tree);
                return SyntaxTreeCategorizedAnalyzerConfigOptions.Create(options);
            }
        }

        public T GetOptionValue<T>(string optionName, SyntaxTree tree, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, T defaultValue)
        {
            if (TryGetOptionValue(optionName, tree, rule, tryParseValue, defaultValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        private bool TryGetOptionValue<T>(string optionName, SyntaxTree tree, DiagnosticDescriptor rule, TryParseValue<T> tryParseValue, T defaultValue, out T value)
        {
            if (ReferenceEquals(this, Empty))
            {
                value = defaultValue;
                return false;
            }

            // Prefer additional file based options for back compat.
            if (_additionalFileBasedOptions.TryGetOptionValue(optionName, rule, tryParseValue, defaultValue, out value))
            {
                return true;
            }

            return _perTreeOptions.TryGetValue(tree, out var lazyTreeOptions) &&
                lazyTreeOptions.Value.TryGetOptionValue(optionName, rule, tryParseValue, defaultValue, out value);
        }
    }
}

#endif
