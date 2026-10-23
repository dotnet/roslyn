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
        private readonly Exception? _exception;
        private readonly ImmutableArray<Diagnostic> _diagnostics;
        private readonly TimeSpan _elapsedTime;
        private readonly (Exception Exception, ImmutableArray<Diagnostic> Diagnostics)? _preCompilationFailure;

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
                                                                         preCompilationFailure: null);

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(postInitTrees: postInitTrees,
                   inputNodes,
                   outputNodes,
                   preCompilationTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                   generatedTrees: ImmutableArray<GeneratedSyntaxTree>.Empty,
                   ImmutableArray<Diagnostic>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                   ImmutableDictionary<string, object>.Empty,
                   exception: null,
                   elapsedTime: TimeSpan.Zero,
                   preCompilationFailure: null)
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
            (Exception Exception, ImmutableArray<Diagnostic> Diagnostics)? preCompilationFailure)
        {
            this.Initialized = true;
            this.PostInitTrees = postInitTrees;
            this.InputNodes = inputNodes;
            this.OutputNodes = outputNodes;
            this.PreCompilationTrees = preCompilationTrees;
            this.GeneratedTrees = generatedTrees;
            _diagnostics = diagnostics;
            this.ExecutedSteps = executedSteps;
            this.OutputSteps = outputSteps;
            this.HostOutputs = hostOutputs;
            _exception = exception;
            _elapsedTime = elapsedTime;
            _preCompilationFailure = preCompilationFailure;
        }

        public GeneratorState WithReparsedPostInitTrees(ImmutableArray<GeneratedSyntaxTree> postInitTrees)
        {
            return new GeneratorState(postInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      this.PreCompilationTrees,
                                      this.GeneratedTrees,
                                      _diagnostics,
                                      this.ExecutedSteps,
                                      this.OutputSteps,
                                      this.HostOutputs,
                                      _exception,
                                      _elapsedTime,
                                      _preCompilationFailure);
        }

        public GeneratorState WithPreCompilationTrees(
            ImmutableArray<GeneratedSyntaxTree> preCompilationTrees, GeneratorRunStateTable executedSteps)
        {
            // Shared-phase success must not clear a failure from a filtered regular phase.
            return new GeneratorState(postInitTrees: this.PostInitTrees,
                                      this.InputNodes,
                                      this.OutputNodes,
                                      preCompilationTrees: preCompilationTrees,
                                      generatedTrees: this.GeneratedTrees,
                                      _diagnostics,
                                      executedSteps.ExecutedSteps,
                                      executedSteps.OutputSteps,
                                      this.HostOutputs,
                                      exception: _exception,
                                      elapsedTime: _elapsedTime,
                                      preCompilationFailure: null);
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
                                      preCompilationFailure: null);
        }

        public GeneratorState WithError(Exception exception, Diagnostic error, TimeSpan elapsedTime, GeneratorRunPhase phase)
        {
            if (phase == GeneratorRunPhase.PreCompilation)
            {
                // Retain the last regular result for filtered recovery, but hide it while the shared phase fails.
                return new GeneratorState(this.PostInitTrees,
                                          this.InputNodes,
                                          this.OutputNodes,
                                          ImmutableArray<GeneratedSyntaxTree>.Empty,
                                          this.GeneratedTrees,
                                          _diagnostics,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                          ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty,
                                          this.HostOutputs,
                                          _exception,
                                          _elapsedTime,
                                          (exception, ImmutableArray.Create(error)));
            }

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
                                      preCompilationFailure: null);
        }
        internal bool Initialized { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal ImmutableArray<SyntaxInputNode> InputNodes { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PreCompilationTrees { get; }

        internal Exception? Exception => _preCompilationFailure?.Exception ?? _exception;

        internal TimeSpan ElapsedTime => PreCompilationFailed ? TimeSpan.Zero : _elapsedTime;

        internal ImmutableArray<Diagnostic> Diagnostics => _preCompilationFailure?.Diagnostics ?? _diagnostics;

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExecutedSteps { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> OutputSteps { get; }

        internal ImmutableDictionary<string, object> HostOutputs { get; }

        /// <summary>
        /// True iff this generator's most recent pre-compilation evaluation threw.
        /// </summary>
        internal bool PreCompilationFailed => _preCompilationFailure.HasValue;

        internal bool RequiresPostInitTreeReparse(ParseOptions parseOptions)
            => PostInitTrees.Any(static (t, parseOptions) => t.Tree.Options != parseOptions, parseOptions);
    }
}
