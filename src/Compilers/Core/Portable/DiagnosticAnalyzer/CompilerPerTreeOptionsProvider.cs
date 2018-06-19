// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class CompilerPerTreeOptionsProvider : PerTreeOptionsProvider
    {
        public static OptionSet EmptyCompilerOptionSet { get; } = new CompilerOptionSet();

        public const string OptionFeatureName = "analyzer-config";

        private readonly ImmutableDictionary<SyntaxTree, OptionSet> _treeDict;

        public static CompilerPerTreeOptionsProvider Empty { get; }
            = new CompilerPerTreeOptionsProvider(ImmutableDictionary<SyntaxTree, OptionSet>.Empty);

        public CompilerPerTreeOptionsProvider(ImmutableDictionary<SyntaxTree, OptionSet> treeDict)
        {
            _treeDict = treeDict;
        }

        public override OptionSet TryGetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : null;

        /// <summary>Used for testing</summary>
        internal bool IsEmpty => _treeDict.IsEmpty;
    }
}
