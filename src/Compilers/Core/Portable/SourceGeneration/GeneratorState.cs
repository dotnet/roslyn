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
        /// Gets an uninitialized generator state
        /// </summary>
        internal static GeneratorState Uninitialized;

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
            : this(info, postInitTrees, ImmutableArray<ISyntaxInputNode>.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<ISyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(info, postInitTrees, inputNodes, outputNodes, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray<Diagnostic>.Empty, exception: null)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains an exception and the associated diagnostic
        /// </summary>
        public GeneratorState(GeneratorInfo info, Exception e, Diagnostic error)
            : this(info, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray<ISyntaxInputNode>.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray.Create(error), exception: e)
        {
        }

        /// <summary>
        /// Creates a generator state that contains results
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<ISyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics)
            : this(info, postInitTrees, inputNodes, outputNodes, generatedTrees, diagnostics, exception: null)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, ImmutableArray<ISyntaxInputNode> inputNodes, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics, Exception? exception)
        {
            this.PostInitTrees = postInitTrees;
            this.InputNodes = inputNodes;
            this.OutputNodes = outputNodes;
            this.GeneratedTrees = generatedTrees;
            this.Info = info;
            this.Diagnostics = diagnostics;
            this.Exception = exception;
        }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal ImmutableArray<ISyntaxInputNode> InputNodes { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal GeneratorInfo Info { get; }

        internal Exception? Exception { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }
    }
}
