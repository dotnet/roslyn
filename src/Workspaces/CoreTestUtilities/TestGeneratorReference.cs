// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// A simple deriviation of <see cref="AnalyzerReference"/> that returns the source generator
    /// passed, for ease in unit tests.
    /// </summary>
    public sealed class TestGeneratorReference : AnalyzerReference
    {
        private readonly ISourceGenerator _generator;

        public TestGeneratorReference(ISourceGenerator generator)
        {
            _generator = generator;
        }

        public override string? FullPath => null;
        public override object Id => this;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;
        public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => ImmutableArray.Create(_generator);
    }
}
