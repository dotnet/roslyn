// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents the current state of a generator
    /// </summary>
    internal readonly struct GeneratorState
    {
        /// <summary>
        /// Creates a new generator state that just contains information
        /// </summary>
        public GeneratorState(GeneratorInfo info)
            : this(info, ImmutableArray<GeneratedSyntaxTree>.Empty)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains information and constant trees
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees)
            : this(info, postInitTrees, ImmutableArray<SyntaxInputNode>.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(info, postInitTrees, inputNodes, outputNodes, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray<Diagnostic>.Empty, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty, exception: null, elapsedTime: TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains an exception and the associated diagnostic
        /// </summary>
        public GeneratorState(GeneratorInfo info, Exception e, Diagnostic error, TimeSpan elapsedTime)
            : this(info, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray<SyntaxInputNode>.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray.Create(error), ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty, exception: e, elapsedTime: elapsedTime)
        {
        }

        /// <summary>
        /// Creates a generator state that contains results
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps, TimeSpan elapsedTime)
            : this(info, postInitTrees, inputNodes, outputNodes, generatedTrees, diagnostics, executedSteps, outputSteps, exception: null, elapsedTime)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<SyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps, Exception? exception, TimeSpan elapsedTime)
        {
            this.Initialized = true;
            this.PostInitTrees = postInitTrees;
            this.InputNodes = inputNodes;
            this.OutputNodes = outputNodes;
            this.GeneratedTrees = generatedTrees;
            this.Info = info;
            this.Diagnostics = diagnostics;
            this.ExecutedSteps = executedSteps;
            this.OutputSteps = outputSteps;
            this.Exception = exception;
            this.ElapsedTime = elapsedTime;
        }

        internal bool Initialized { get; }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal ImmutableArray<SyntaxInputNode> InputNodes { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal GeneratorInfo Info { get; }

        internal Exception? Exception { get; }

        internal TimeSpan ElapsedTime { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExecutedSteps { get; }

        internal ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> OutputSteps { get; }
    }
}
