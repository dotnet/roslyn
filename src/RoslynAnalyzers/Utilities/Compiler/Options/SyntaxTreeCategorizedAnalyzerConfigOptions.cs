// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Analyzer.Utilities
{
    /// <summary>
    /// Analyzer configuration options for a given syntax tree from <see cref="AnalyzerConfigOptions"/>
    /// </summary>
    internal sealed class SyntaxTreeCategorizedAnalyzerConfigOptions : AbstractCategorizedAnalyzerConfigOptions
    {
        private readonly AnalyzerConfigOptions? _analyzerConfigOptions;
        private static readonly ConditionalWeakTable<ImmutableDictionary<string, string>, SyntaxTreeCategorizedAnalyzerConfigOptions> s_perTreeOptionsCache = new();

        public static readonly SyntaxTreeCategorizedAnalyzerConfigOptions Empty = new(analyzerConfigOptions: null);

        private SyntaxTreeCategorizedAnalyzerConfigOptions(AnalyzerConfigOptions? analyzerConfigOptions)
        {
            _analyzerConfigOptions = analyzerConfigOptions;
        }

        public static SyntaxTreeCategorizedAnalyzerConfigOptions Create(AnalyzerConfigOptions? analyzerConfigOptions)
        {
            if (analyzerConfigOptions == null)
            {
                return Empty;
            }

            var optionsMap = TryGetBackingOptionsDictionary(analyzerConfigOptions);
            if (optionsMap == null)
            {
                return new SyntaxTreeCategorizedAnalyzerConfigOptions(analyzerConfigOptions);
            }

            if (optionsMap.IsEmpty)
            {
                return Empty;
            }

            return s_perTreeOptionsCache.GetValue(optionsMap, _ => new SyntaxTreeCategorizedAnalyzerConfigOptions(analyzerConfigOptions));

            // Local functions.
            static ImmutableDictionary<string, string>? TryGetBackingOptionsDictionary(AnalyzerConfigOptions analyzerConfigOptions)
            {
                // Reflection based optimization for analyzer config options.
                // Ideally 'AnalyzerConfigOptions' would expose such an the API.
                var type = analyzerConfigOptions.GetType();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                return type.GetField("_backing", flags)?.GetValue(analyzerConfigOptions) as ImmutableDictionary<string, string> ??
                    type.GetField("_analyzerOptions", flags)?.GetValue(analyzerConfigOptions) as ImmutableDictionary<string, string>;
            }
        }

        public override bool IsEmpty => ReferenceEquals(this, Empty);

        protected override bool TryGetOptionValue(string optionKeyPrefix, string? optionKeySuffix, string optionName, [NotNullWhen(returnValue: true)] out string? valueString)
        {
            if (IsEmpty)
            {
                valueString = null;
                return false;
            }

            RoslynDebug.Assert(_analyzerConfigOptions != null);
            var key = optionKeyPrefix;
            if (optionKeySuffix != null)
            {
                key += $"{optionKeySuffix}.";
            }

            key += optionName;

            return _analyzerConfigOptions.TryGetValue(key, out valueString);
        }
    }
}
