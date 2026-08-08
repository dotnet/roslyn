// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal sealed partial class ProjectState
{
    /// <summary>
    /// Holds on a map from source path to <see cref="AnalyzerConfigData"/> calculated by the compiler and chained to <paramref name="fallbackOptions"/>.
    /// This cache is stored on <see cref="ProjectState"/> and needs to be invalidated whenever <see cref="SolutionState.FallbackAnalyzerOptions"/> for the language of the project change,
    /// editorconfig file is updated, etc.
    /// </summary>
    private readonly struct AnalyzerConfigOptionsCache(TextDocumentStates<AnalyzerConfigDocumentState> analyzerConfigDocumentStates, StructuredAnalyzerConfigOptions fallbackOptions, ImmutableArray<KeyValuePair<string, string>> pathMap)
    {
        public readonly struct Value(AnalyzerConfigSet configSet, StructuredAnalyzerConfigOptions fallbackOptions)
        {
            private readonly ConcurrentDictionary<string, AnalyzerConfigData> _sourcePathToResult = [];
            private readonly Func<string, AnalyzerConfigData> _computeFunction = path => new AnalyzerConfigData(configSet.GetOptionsForSourcePath(path), fallbackOptions);
            private readonly Lazy<AnalyzerConfigData> _global = new(() => new AnalyzerConfigData(configSet.GlobalConfigOptions, StructuredAnalyzerConfigOptions.Empty));

            public AnalyzerConfigData GlobalConfigOptions
                => _global.Value;

            public AnalyzerConfigData GetOptionsForSourcePath(string sourcePath)
                => _sourcePathToResult.GetOrAdd(sourcePath, _computeFunction);
        }

        public readonly AsyncLazy<Value> Lazy = AsyncLazy.Create(
            asynchronousComputeFunction: static async (args, cancellationToken) =>
            {
                var (analyzerConfigDocumentStates, fallbackOptions, pathMap) = args;
                var tasks = analyzerConfigDocumentStates.States.Values.Select(a => a.GetAnalyzerConfigAsync(cancellationToken));
                var analyzerConfigs = await Task.WhenAll(tasks).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();

                return new Value(AnalyzerConfigSet.Create(analyzerConfigs, pathMap, out _), fallbackOptions);
            },
            synchronousComputeFunction: static (args, cancellationToken) =>
            {
                var (analyzerConfigDocumentStates, fallbackOptions, pathMap) = args;
                var analyzerConfigs = analyzerConfigDocumentStates.SelectAsArray(a => a.GetAnalyzerConfig(cancellationToken));
                return new Value(AnalyzerConfigSet.Create(analyzerConfigs, pathMap, out _), fallbackOptions);
            },
            arg: (analyzerConfigDocumentStates, fallbackOptions, pathMap));

        public StructuredAnalyzerConfigOptions FallbackOptions
            => fallbackOptions;
    }
}
