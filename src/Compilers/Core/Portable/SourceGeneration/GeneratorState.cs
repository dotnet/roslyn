// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the current state of a generator
    /// </summary>
    internal readonly struct GeneratorState
    {

        /// <summary>
        /// A generator state that has been initialized but produced no results
        /// </summary>
        public static readonly GeneratorState Empty = new GeneratorState(ImmutableArray<GeneratedSyntaxTree>.Empty,
                                                                         ImmutableArray<SyntaxInputNode>.Empty,
                                                                         ImmutableArray<IIncrementalGeneratorOutputNode>.Empty,
                                                                         ImmutableArray<GeneratedSyntaxTree>.Empty,
                                                                         ImmutableArray<Diagnostic>.Empty,
                                                                         ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                                                         ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                                                         ImmutableArray<(string, string)>.Empty,
                                                                         exception: null,
                                                                         elapsedTime: TimeSpan.Zero);

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(postInitTrees,
                   inputNodes,
                   outputNodes,
                   ImmutableArray<GeneratedSyntaxTree>.Empty,
                   ImmutableArray<Diagnostic>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableArray<(string, string)>.Empty,
                   exception: null,
                   elapsedTime: TimeSpan.Zero)
        {
        }

        private GeneratorState(
            ImmutableArray<GeneratedSyntaxTree> postInitTrees,
            ImmutableArray<SyntaxInputNode> inputNodes,
            ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes,
            ImmutableArray<GeneratedSyntaxTree> generatedTrees,
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps,
            ImmutableArray<(string Key, string Value)> hostOutputs,
            Exception? exception,
            TimeSpan elapsedTime)
        {
            this.Initialized = true;
            this.PostInitTrees = postInitTrees;
            this.InputNodes = inputNodes;
            this.OutputNodes = outputNodes;
            this.GeneratedTrees = generatedTrees;
            this.Diagnostics = diagnostics;
            this.ExecutedSteps = executedSteps;
            this.OutputSteps = outputSteps;
            this.HostOutputs = hostOutputs;
            this.Exception = exception;
            this.ElapsedTime = elapsedTime;
        }

        public GeneratorState WithResults(ImmutableArray<GeneratedSyntaxTree> generatedTrees,
                                          ImmutableArray<Diagnostic> diagnostics,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps,
                                          ImmutableArray<(string Key, string Value)> hostOutputs,
                                          TimeSpan elapsedTime)
        {
            return new GeneratorState(this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      generatedTrees,
                                      diagnostics,
                                      executedSteps,
                                      outputSteps,
                                      hostOutputs,
                                      exception: null,
                                      elapsedTime);
        }

        public GeneratorState WithError(Exception exception, Diagnostic error, TimeSpan elapsedTime)
        {
            return new GeneratorState(this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      ImmutableArray<GeneratedSyntaxTree>.Empty,
                                      ImmutableArray.Create(error),
                                      ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                      ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                      ImmutableArray<(string, string)>.Empty,
                                      exception,
                                      elapsedTime);
        }

        internal bool Initialized { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal ImmutableArray<SyntaxInputNode> InputNodes { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal Exception? Exception { get; }

        internal TimeSpan ElapsedTime { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExecutedSteps { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> OutputSteps { get; }

        internal ImmutableArray<(string Key, string Value)> HostOutputs { get; }

        internal bool RequiresPostInitReparse(ParseOptions parseOptions) => PostInitTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions);
    }
}
