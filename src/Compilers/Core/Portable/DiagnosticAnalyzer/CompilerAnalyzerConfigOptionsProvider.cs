// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class CompilerAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly ImmutableDictionary<object, AnalyzerConfigOptions> _treeDict;

        public static CompilerAnalyzerConfigOptionsProvider Empty { get; }
            = new CompilerAnalyzerConfigOptionsProvider(
                ImmutableDictionary<object, AnalyzerConfigOptions>.Empty,
                DictionaryAnalyzerConfigOptions.Empty);

        internal CompilerAnalyzerConfigOptionsProvider(
            ImmutableDictionary<object, AnalyzerConfigOptions> treeDict,
            AnalyzerConfigOptions globalOptions)
        {
            _treeDict = treeDict;
            GlobalOptions = globalOptions;
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
            => _treeDict.TryGetValue(tree, out var options) ? options : DictionaryAnalyzerConfigOptions.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
            => _treeDict.TryGetValue(textFile, out var options) ? options : DictionaryAnalyzerConfigOptions.Empty;

        internal CompilerAnalyzerConfigOptionsProvider WithAdditionalTreeOptions(ImmutableDictionary<object, AnalyzerConfigOptions> treeDict)
            => new CompilerAnalyzerConfigOptionsProvider(_treeDict.AddRange(treeDict), GlobalOptions);

        internal CompilerAnalyzerConfigOptionsProvider WithGlobalOptions(AnalyzerConfigOptions globalOptions)
            => new CompilerAnalyzerConfigOptionsProvider(_treeDict, globalOptions);

        // <Metalama>
        private CompilerAnalyzerConfigOptionsProvider WithMappedTrees(
            IEnumerable<(SyntaxTree OldTree, SyntaxTree NewTree)> treeMap)
        {
            var hasChange = false;
            var builder = this._treeDict.ToBuilder();

            foreach (var entry in treeMap)
            {
                if (builder.TryGetValue(entry.OldTree, out var options))
                {
                    hasChange = true;

                    builder.Remove(entry.OldTree);
                    builder[entry.NewTree] = options;
                }

            }

            if (!hasChange)
            {
                return this;
            }
            else
            {
                return new CompilerAnalyzerConfigOptionsProvider(builder.ToImmutable(), this.GlobalOptions);
            }
        }

        public static AnalyzerConfigOptionsProvider MapSyntaxTrees(
             AnalyzerConfigOptionsProvider source,
             IEnumerable<(SyntaxTree OldTree, SyntaxTree NewTree)> treeMap)
        {
            return source switch
            {
                // This is the scenario when the code is compiled from the compiler.
                CompilerAnalyzerConfigOptionsProvider fromCompiler => fromCompiler.WithMappedTrees(treeMap),

                // This is the scenario when the code is compiled from Metalama.Try.
                _ => source
            };
        }
        // </Metalama>
    }
}
