// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigPropertyMap> _treeDict;

        public static CompilerAnalyzerConfigOptionsProvider Empty { get; }
            = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigPropertyMap>.Empty);

        public CompilerAnalyzerConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigPropertyMap> treeDict)
        {
            _treeDict = treeDict;
        }

        public override AnalyzerConfigPropertyMap GetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : CompilerAnalyzerConfigPropertyMap.Empty;

        public override AnalyzerConfigPropertyMap GetOptions(AdditionalText textFile)
            => _treeDict.TryGetValue(textFile, out var options) ? options : CompilerAnalyzerConfigPropertyMap.Empty;

        /// <summary>Used for testing</summary>
        internal bool IsEmpty => _treeDict.IsEmpty;
    }
}
