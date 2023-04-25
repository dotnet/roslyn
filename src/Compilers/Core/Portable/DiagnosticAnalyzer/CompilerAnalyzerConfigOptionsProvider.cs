// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;
        private readonly AnalyzerConfigOptionKeys? _optionKeys;

        public static CompilerAnalyzerConfigOptionsProvider Empty { get; }
            = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                DictionaryAnalyzerConfigOptions.Empty,
                analyzerConfigSet: null);

        private CompilerAnalyzerConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions,
            AnalyzerConfigOptionKeys? optionKeys)
        {
            _treeDict = treeDict;
            GlobalOptions = globalOptions;
            _optionKeys = optionKeys;
        }

        internal CompilerAnalyzerConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions,
            AnalyzerConfigSet? analyzerConfigSet)
            : this(treeDict, globalOptions, analyzerConfigSet?.OptionKeys)
        {
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : DictionaryAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _treeDict.TryGetValue(textFile, out var options) ? options : DictionaryAnalyzerConfigOptions.Empty;

        public override bool TryGetAnalyzerConfigOptionKeys(CancellationToken cancellationToken, out AnalyzerConfigOptionKeys? optionKeys)
        {
            optionKeys = _optionKeys;
            return optionKeys != null;
        }

        internal CompilerAnalyzerConfigOptionsProvider WithAdditionalTreeOptions(ImmutableDictionary<object, AnalyzerConfigOptions> treeDict, IEnumerable<string> optionKeys)
            => new CompilerAnalyzerConfigOptionsProvider(_treeDict.AddRange(treeDict), GlobalOptions, WithAdditionalOptionKeys(_optionKeys, optionKeys));

        internal CompilerAnalyzerConfigOptionsProvider WithGlobalOptions(AnalyzerConfigOptions globalOptions, IEnumerable<string> optionKeys)
            => new CompilerAnalyzerConfigOptionsProvider(_treeDict, globalOptions, WithAdditionalOptionKeys(_optionKeys, optionKeys));

        private static AnalyzerConfigOptionKeys WithAdditionalOptionKeys(AnalyzerConfigOptionKeys? existingOptionKeys, IEnumerable<string> optionKeys)
            => existingOptionKeys?.WithAdditionalAnalyzerConfigOptionKeys(optionKeys)
                ?? new AnalyzerConfigOptionKeys(configuredDiagnosticIds: ImmutableHashSet<string>.Empty, optionKeys.ToImmutableHashSet());
    }
}
