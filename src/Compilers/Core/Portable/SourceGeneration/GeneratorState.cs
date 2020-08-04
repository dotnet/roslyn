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
    /// <remarks>
    /// A generator state is essentially a small state machine:
    ///     1. We start with just info
    ///     2. We can optionally set a receiver that will be used in the next iteration
    ///     3. We set either the result or error of the iteration that just happened
    ///     4. We either go back to state 2 or 3.
    /// </remarks>
    internal readonly struct GeneratorState
    {
        public GeneratorState(GeneratorInfo info)
            : this(info, ImmutableArray<GeneratedSourceText>.Empty, ImmutableArray<SyntaxTree>.Empty, ImmutableArray<Diagnostic>.Empty, syntaxReceiver: null, exception: null)
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
        /// Called to save the created syntax receiver
        /// </summary>
        /// <remarks>
        /// We retain the existing state, as it may be needed for the upcoming generation pass.
        /// </remarks>
        internal GeneratorState WithReceiver(ISyntaxReceiver syntaxReceiver)
        {
            Debug.Assert(this.SyntaxReceiver is null);
            return new GeneratorState(this.Info,
                                      sourceTexts: this.SourceTexts,
                                      trees: this.Trees,
                                      diagnostics: this.Diagnostics,
                                      syntaxReceiver: syntaxReceiver,
                                      exception: null);
        }

        /// <summary>
        /// Called when the generator has a result to store.
        /// </summary>
        /// <remarks>
        /// We discard any receiver that was set as it is no longer needed.
        /// We discard any saved exception as it must refer to a previous iteration.
        /// </remarks>
        internal GeneratorState WithResult(ImmutableArray<GeneratedSourceText> sourceTexts,
                                          ImmutableArray<SyntaxTree> trees,
                                          ImmutableArray<Diagnostic> diagnostics)
        {
            Debug.Assert(sourceTexts.Length == trees.Length);
            return new GeneratorState(this.Info,
                                      sourceTexts,
                                      trees,
                                      diagnostics,
                                      syntaxReceiver: null,
                                      exception: null);
        }

        /// <summary>
        /// Called when the generator threw an exception
        /// </summary>
        /// <remarks>
        /// We discard any other saved state, as it must refer to a previous iteration and is no longer needed.
        /// </remarks>
        internal GeneratorState WithError(Exception e, Diagnostic diagnostic)
        {
            return new GeneratorState(this.Info,
                                      sourceTexts: ImmutableArray<GeneratedSourceText>.Empty,
                                      trees: ImmutableArray<SyntaxTree>.Empty,
                                      diagnostics: ImmutableArray.Create(diagnostic),
                                      syntaxReceiver: null,
                                      exception: e);
        }
    }
}
