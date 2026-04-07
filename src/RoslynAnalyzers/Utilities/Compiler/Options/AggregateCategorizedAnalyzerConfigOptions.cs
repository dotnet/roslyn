// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

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
        public static readonly AggregateCategorizedAnalyzerConfigOptions Empty = new(
            globalOptions: null,
            ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>>.Empty);

        private readonly Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>? _globalOptions;
        private readonly ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> _perTreeOptions;

        private AggregateCategorizedAnalyzerConfigOptions(Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>? globalOptions, ImmutableDictionary<SyntaxTree, Lazy<SyntaxTreeCategorizedAnalyzerConfigOptions>> perTreeOptions)
        {
            _globalOptions = globalOptions;
            _perTreeOptions = perTreeOptions;
        }

        public bool IsEmpty
        {
            get
            {
                Debug.Assert(ReferenceEquals(this, Empty) || !_perTreeOptions.IsEmpty);
                return ReferenceEquals(this, Empty);
            }
        }

        public static AggregateCategorizedAnalyzerConfigOptions Create(AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider, Compilation compilation)
        {
            analyzerConfigOptionsProvider = analyzerConfigOptionsProvider ?? throw new ArgumentNullException(nameof(analyzerConfigOptionsProvider));

            if (analyzerConfigOptionsProvider.IsEmpty())
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

            return new AggregateCategorizedAnalyzerConfigOptions(globalOptions, perTreeOptionsBuilder.ToImmutableDictionaryAndFree());

            static SyntaxTreeCategorizedAnalyzerConfigOptions Create(SyntaxTree tree, AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider)
            {
                var options = analyzerConfigOptionsProvider.GetOptions(tree);
                return SyntaxTreeCategorizedAnalyzerConfigOptions.Create(options);
            }
        }

        public T GetOptionValue<T>(string optionName, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T> tryParseValue, T defaultValue, OptionKind kind = OptionKind.DotnetCodeQuality)
        {
            if (TryGetOptionValue(
                optionName,
                kind,
                tree,
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
            if (TryGetOptionValue(optionName, kind, tree, rule, tryParseValue, arg, defaultValue, out var value))
            {
                return value;
            }

            return defaultValue;
        }

        private bool TryGetOptionValue<T, TArg>(string optionName, OptionKind kind, SyntaxTree? tree, DiagnosticDescriptor? rule, TryParseValue<T, TArg> tryParseValue, TArg arg, T defaultValue, [MaybeNullWhen(false)] out T value)
        {
            value = defaultValue;

            if (ReferenceEquals(this, Empty))
            {
                return false;
            }

            if (tree is null)
            {
                if (_globalOptions is null)
                {
                    return false;
                }

                return _globalOptions.Value.TryGetOptionValue(optionName, kind, rule, tryParseValue, arg, defaultValue, out value);
            }

            return _perTreeOptions.TryGetValue(tree, out var lazyTreeOptions) &&
                lazyTreeOptions.Value.TryGetOptionValue(optionName, kind, rule, tryParseValue, arg, defaultValue, out value);
        }
    }
}
