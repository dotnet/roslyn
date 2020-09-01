// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

#nullable enable
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
            : this(info, ImmutableArray<GeneratedSourceText>.Empty, ImmutableArray<SyntaxTree>.Empty, ImmutableArray<Diagnostic>.Empty, syntaxReceiver: null, exception: null)
        {
        }

        /// <summary>
        /// Creates a new generator state that contains an exception and the associated diagnostic
        /// </summary>
        public GeneratorState(GeneratorInfo info, Exception e, Diagnostic error)
            : this(info, ImmutableArray<GeneratedSourceText>.Empty, ImmutableArray<SyntaxTree>.Empty, ImmutableArray.Create(error), syntaxReceiver: null, exception: e)
        {
        }

        /// <summary>
        /// Creates a generator state that contains results
        /// </summary>
        public GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSourceText> sourceTexts, ImmutableArray<SyntaxTree> trees, ImmutableArray<Diagnostic> diagnostics)
            : this(info, sourceTexts, trees, diagnostics, syntaxReceiver: null, exception: null)
        {
        }

        private GeneratorState(GeneratorInfo info, ImmutableArray<GeneratedSourceText> sourceTexts, ImmutableArray<SyntaxTree> trees, ImmutableArray<Diagnostic> diagnostics, ISyntaxReceiver? syntaxReceiver, Exception? exception)
        {
            this.SourceTexts = sourceTexts;
            this.Trees = trees;
            this.Info = info;
            this.Diagnostics = diagnostics;
            this.SyntaxReceiver = syntaxReceiver;
            this.Exception = exception;
        }

        internal ImmutableArray<GeneratedSourceText> SourceTexts { get; }

        internal ImmutableArray<SyntaxTree> Trees { get; }

        internal GeneratorInfo Info { get; }

        internal ISyntaxReceiver? SyntaxReceiver { get; }

        internal Exception? Exception { get; }

        internal ImmutableArray<Diagnostic> Diagnostics { get; }

        /// <summary>
        /// Adds an <see cref="ISyntaxReceiver"/> to this generator state
        /// </summary>
        internal GeneratorState WithReceiver(ISyntaxReceiver syntaxReceiver)
        {
            Debug.Assert(this.Exception is null);
            return new GeneratorState(this.Info,
                                      sourceTexts: this.SourceTexts,
                                      trees: this.Trees,
                                      diagnostics: this.Diagnostics,
                                      syntaxReceiver: syntaxReceiver,
                                      exception: null);
        }
    }
}
