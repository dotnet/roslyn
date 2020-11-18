// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#if CODEANALYSIS_V3_OR_BETTER

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Analyzer.Utilities.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzer.Utilities
{
    using static CategorizedAnalyzerConfigOptionsExtensions;

    /// <summary>
    /// Aggregate analyzer configuration options:
    ///
    /// <list type="number">
    /// <item><description>Per syntax tree options from <see cref="AnalyzerConfigOptionsProvider"/>.</description></item>
    /// <item><description>Options from an <strong>.editorconfig</strong> file passed in as an additional file (back compat).</description></item>
    /// </list>
    ///
    /// <inheritdoc cref="ICategorizedAnalyzerConfigOptions"/>
    /// </summary>
    internal sealed class AggregateCategorizedAnalyzerConfigOptions : ICategorizedAnalyzerConfigOptions
    {
        public static readonly AggregateCategorizedAnalyzerConfigOptions Empty = new AggregateCategorizedAnalyzerConfigOptions(
            globalOptions: null,
            ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>>.Empty,
            CompilationCategorizedAnalyzerConfigOptions.Empty);

        private readonly Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>? _globalOptions;
        private readonly ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> _perTreeOptions;
        private readonly CompilationCategorizedAnalyzerConfigOptions _additionalFileBasedOptions;

        private AggregateCategorizedAnalyzerConfigOptions(Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>? globalOptions, ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> perTreeOptions, CompilationCategorizedAnalyzerConfigOptions additionalFileBasedOptions)
        {
            _globalOptions = globalOptions;
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

            Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>? globalOptions;
#if CODEANALYSIS_V3_7_OR_BETTER
            globalOptions = new Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>(() => SyntaxTreeCategorizedAnalyzerConfigOptions.Create(analyzerConfigOptionsProvider.GlobalOptions));
#else
            globalOptions = null;
#endif

            var perTreeOptionsBuilder = PooledDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>>.GetInstance();
            foreach (var tree in compilation.SyntaxTrees)
            {
                perTreeOptionsBuilder.Add(tree, new Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>(() => Create(tree, analyzerConfigOptionsProvider)));
            }

            return new AggregateCategorizedAnalyzerConfigOptions(globalOptions, perTreeOptionsBuilder.ToImmutableDictionaryAndFree(), additionalFileBasedOptions);

            static SyntaxTreeCategorizedAnalyzerConfigOptions Create(SyntaxTree tree, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
            {
                var options = analyzerConfigOptionsProvider.GetOptions(tree);
                return SyntaxTreeCategorizedAnalyzerConfigOptions.Create(options);
            }
        }

        [return: MaybeNull, NotNullIfNotNull("defaultValue")]
        public T/*??*/ GetOptionValue<T>(string optionName, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T> tryParseValue, [MaybeNull] T/*??*/ defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            if (TryGetOptionValue(optionName, kind, tree, rule, tryParseValue, defaultValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        private bool TryGetOptionValue<T>(string optionName, OptionKind kind, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T> tryParseValue, [MaybeNull] T/*??*/ defaultValue, [MaybeNullWhen(false), NotNullIfNotNull("defaultValue")] out T value)
        {
            if (ReferenceEquals(this, Empty))
            {
                value = defaultValue;
                return false;
            }

            // Prefer additional file based options for back compat.
            if (_additionalFileBasedOptions.TryGetOptionValue(optionName, kind, rule, tryParseValue, defaultValue, out value))
            {
                return true;
            }

            if (tree is null)
            {
                if (_globalOptions is null)
                {
                    value = defaultValue;
                    return false;
                }

                return _globalOptions.Value.TryGetOptionValue(optionName, kind, rule, tryParseValue, defaultValue, out value);
            }

            return _perTreeOptions.TryGetValue(tree, out var lazyTreeOptions) &&
                lazyTreeOptions.Value.TryGetOptionValue(optionName, kind, rule, tryParseValue, defaultValue, out value);
        }
    }
}

#endif
