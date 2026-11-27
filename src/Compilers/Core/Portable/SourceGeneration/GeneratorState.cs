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
        public static readonly GeneratorState Empty = new GeneratorState(postInitTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                                                                         ImmutableArray<SyntaxInputNode>.Empty,
                                                                         ImmutableArray<IIncrementalGeneratorOutputNode>.Empty,
                                                                         preCompilationTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                                                                         generatedTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                                                                         ImmutableArray<Diagnostic>.Empty,
                                                                         ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                                                         ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                                                         ImmutableDictionary<string, object>.Empty,
                                                                         exception: null,
                                                                         elapsedTime: TimeSpan.Zero,
                                                                         preCompilationFailed: false);

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(postInitTrees, inputNodes, outputNodes, ImmutableArray<GeneratedSyntaxTree>.Empty)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains information, constant trees (including pre-compilation) and an execution pipeline
        /// </summary>
        public GeneratorState(ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> preCompilationTrees)
            : this(postInitTrees: postInitTrees,
                   inputNodes,
                   outputNodes,
                   preCompilationTrees: preCompilationTrees,
                   generatedTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                   ImmutableArray<Diagnostic>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableDictionary<string, object>.Empty,
                   exception: null,
                   elapsedTime: TimeSpan.Zero,
                   preCompilationFailed: false)
        {
        }

        private GeneratorState(
            ImmutableArray<GeneratedSyntaxTree> postInitTrees,
            ImmutableArray<SyntaxInputNode> inputNodes,
            ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes,
            ImmutableArray<GeneratedSyntaxTree> preCompilationTrees,
            ImmutableArray<GeneratedSyntaxTree> generatedTrees,
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps,
            ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps,
            ImmutableDictionary<string, object> hostOutputs,
            Exception? exception,
            TimeSpan elapsedTime,
            bool preCompilationFailed)
        {
            this.Initialized = true;
            this.PostInitTrees = postInitTrees;
            this.InputNodes = inputNodes;
            this.OutputNodes = outputNodes;
            this.PreCompilationTrees = preCompilationTrees;
            this.GeneratedTrees = generatedTrees;
            this.Diagnostics = diagnostics;
            this.ExecutedSteps = executedSteps;
            this.OutputSteps = outputSteps;
            this.HostOutputs = hostOutputs;
            this.Exception = exception;
            this.ElapsedTime = elapsedTime;
            this.PreCompilationFailed = preCompilationFailed;
        }

        public GeneratorState WithPreCompilationTrees(ImmutableArray<GeneratedSyntaxTree> preCompilationTrees)
        {
            return new GeneratorState(postInitTrees: this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      preCompilationTrees: preCompilationTrees,
                                      generatedTrees: this.GeneratedTrees,
                                      this.Diagnostics,
                                      this.ExecutedSteps,
                                      this.OutputSteps,
                                      this.HostOutputs,
                                      exception: null,
                                      elapsedTime: this.ElapsedTime,
                                      preCompilationFailed: false);
        }

        public GeneratorState WithResults(ImmutableArray<GeneratedSyntaxTree> generatedTrees,
                                          ImmutableArray<Diagnostic> diagnostics,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps,
                                          ImmutableDictionary<string, object> hostOutputs,
                                          TimeSpan elapsedTime)
        {
            return new GeneratorState(postInitTrees: this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      preCompilationTrees: this.PreCompilationTrees,
                                      generatedTrees: generatedTrees,
                                      diagnostics,
                                      executedSteps,
                                      outputSteps,
                                      hostOutputs,
                                      exception: null,
                                      elapsedTime,
                                      preCompilationFailed: false);
        }

        public GeneratorState WithError(Exception exception, Diagnostic error, TimeSpan elapsedTime, GeneratorRunPhase phase)
        {
            return new GeneratorState(postInitTrees: this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      // Preserve pre-comp trees only when the standard phase failed: those trees were
                                      // already added to the compilation other generators observed, so dropping them
                                      // would leave this generator's state inconsistent with what those generators saw.
                                      preCompilationTrees: phase == GeneratorRunPhase.Standard ? this.PreCompilationTrees : ImmutableArray<GeneratedSyntaxTree>.Empty,
                                      generatedTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                                      ImmutableArray.Create(error),
                                      ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                      ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                      ImmutableDictionary<string, object>.Empty,
                                      exception,
                                      elapsedTime,
                                      preCompilationFailed: phase == GeneratorRunPhase.PreCompilation);
        }
        internal bool Initialized { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal ImmutableArray<SyntaxInputNode> InputNodes { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PreCompilationTrees { get; }

        internal Exception? Exception { get; }

        internal TimeSpan ElapsedTime { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExecutedSteps { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> OutputSteps { get; }

        internal ImmutableDictionary<string, object> HostOutputs { get; }

        /// <summary>
        /// True iff this generator's most recent pre-compilation evaluation threw.
        /// </summary>
        internal bool PreCompilationFailed { get; }

        internal bool RequiresInputTreeReparse(ParseOptions parseOptions)
            => PostInitTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions)
            || PreCompilationTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions);
    }
}
