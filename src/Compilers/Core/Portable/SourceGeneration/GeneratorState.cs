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
            : this(info, postInitTrees, GeneratorValueSources.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains information, constant trees and an execution pipeline
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, GeneratorValueSources sources, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes)
            : this(info, postInitTrees, sources, outputNodes, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray<Diagnostic>.Empty, syntaxReceiver: null, exception: null)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains an exception and the associated diagnostic
        /// </summary>
        public GeneratorState(GeneratorInfo info, Exception e, Diagnostic error)
            : this(info, ImmutableArray<GeneratedSyntaxTree>.Empty, GeneratorValueSources.Empty, ImmutableArray<IIncrementalGeneratorOutputNode>.Empty, ImmutableArray<GeneratedSyntaxTree>.Empty, ImmutableArray.Create(error), syntaxReceiver: null, exception: e)
        {
        }

        /// <summary>
        /// Creates a generator state that contains results
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, GeneratorValueSources sources, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics)
            : this(info, postInitTrees, sources, outputNodes, generatedTrees, diagnostics, syntaxReceiver: null, exception: null)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSyntaxTree> postInitTrees, GeneratorValueSources sources, ImmutableArray<IIncrementalGeneratorOutputNode> outputNodes, ImmutableArray<GeneratedSyntaxTree> generatedTrees, ImmutableArray<Diagnostic> diagnostics, ISyntaxContextReceiver? syntaxReceiver, Exception? exception)
        {
            this.PostInitTrees = postInitTrees;
            Sources = sources;
            this.OutputNodes = outputNodes;
            this.GeneratedTrees = generatedTrees;
            this.Info = info;
            this.Diagnostics = diagnostics;
            this.SyntaxReceiver = syntaxReceiver;
            this.Exception = exception;
        }

        internal ImmutableArray<GeneratedSyntaxTree> PostInitTrees { get; }

        internal GeneratorValueSources Sources { get; }

        internal ImmutableArray<IIncrementalGeneratorOutputNode> OutputNodes { get; }

        internal ImmutableArray<GeneratedSyntaxTree> GeneratedTrees { get; }

        internal GeneratorInfo Info { get; }

        internal ISyntaxContextReceiver? SyntaxReceiver { get; }

        internal Exception? Exception { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Adds a syntax receiver to this generator state
        /// </summary>
        internal GeneratorState WithReceiver(ISyntaxContextReceiver syntaxReceiver)
        {
            Debug.Assert(this.Exception is null);
            return new GeneratorState(this.Info,
                                      postInitTrees: this.PostInitTrees,
                                      sources: this.Sources,
                                      outputNodes: this.OutputNodes,
                                      generatedTrees: this.GeneratedTrees,
                                      diagnostics: this.Diagnostics,
                                      syntaxReceiver: syntaxReceiver,
                                      exception: null);
        }
    }
}
