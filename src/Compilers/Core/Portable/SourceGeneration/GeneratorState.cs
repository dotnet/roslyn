// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;

#nullable enable
namespace Microsoft.CodeAnalysis
{
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

        internal GeneratorState WithReceiver(ISyntaxReceiver syntaxReceiver)
        {
            return new GeneratorState(this.Info,
                                      sourceTexts: this.SourceTexts,
                                      trees: this.Trees,
                                      diagnostics: this.Diagnostics,
                                      syntaxReceiver: syntaxReceiver,
                                      exception: null);
        }

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
