// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

#nullable enable

namespace Microsoft.CodeAnalysis
{
    public readonly struct GeneratorDriverRunResult
    {
        internal GeneratorDriverRunResult(ImmutableArray<GeneratorRunResult> results, ImmutableArray<SyntaxTree> trees, ImmutableArray<Diagnostic> diagnostics)
        {
            this.Results = results;
            this.SyntaxTrees = trees;
            this.Diagnostics = diagnostics;
        }

        public ImmutableArray<GeneratorRunResult> Results { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public ImmutableArray<SyntaxTree> SyntaxTrees { get; }
    }

    public readonly struct GeneratorRunResult
    {
        internal GeneratorRunResult(ISourceGenerator generator, ImmutableArray<GeneratedSourceResult> generatedTrees, ImmutableArray<Diagnostic> diagnostics, Exception? exception)
        {
            this.Generator = generator;
            this.GeneratedTrees = generatedTrees;
            this.Diagnostics = diagnostics;
            this.Exception = exception;
        }

        public ISourceGenerator Generator { get; }

        public ImmutableArray<GeneratedSourceResult> GeneratedTrees { get; }

        public ImmutableArray<Diagnostic> Diagnostics { get; }

        public Exception? Exception { get; }

        public bool Succeeded => Exception is null;
    }

    public readonly struct GeneratedSourceResult
    {
        internal GeneratedSourceResult(SyntaxTree tree, SourceText text, string hintName)
        {
            this.SyntaxTree = tree;
            this.SourceText = text;
            this.HintName = hintName;
        }

        public SyntaxTree SyntaxTree { get; }

        public SourceText SourceText { get; }

        public string HintName { get; }
    }
}
