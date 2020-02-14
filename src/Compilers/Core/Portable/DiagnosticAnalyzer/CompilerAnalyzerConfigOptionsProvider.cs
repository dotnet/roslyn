// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

        public static CompilerAnalyzerConfigOptionsProvider Empty { get; }
            = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty);

        public CompilerAnalyzerConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict)
        {
            _treeDict = treeDict;
        }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : CompilerAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _treeDict.TryGetValue(textFile, out var options) ? options : CompilerAnalyzerConfigOptions.Empty;
    }
}
