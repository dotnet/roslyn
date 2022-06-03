﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// A simple deriviation of <see cref="AnalyzerReference"/> that returns the source generator
    /// passed, for ease in unit tests.
    /// </summary>
    public sealed class TestGeneratorReference : AnalyzerReference, IChecksummedObject
    {
        private readonly ISourceGenerator _generator;
        private readonly Checksum _checksum;

        public TestGeneratorReference(ISourceGenerator generator)
        {
            _generator = generator;
            Guid = Guid.NewGuid();

            // In unit tests, we often simulate OOP interactions by interacting with a in-process remote host, but
            // still going through our serialization pathways. In real OOP cases we only support serializing
            // AnalyzerFileReferences, since we load the file in both places. In unit tests however we often directly
            // create instances of ISourceGenerators so we can test generator support for various features.
            // We'll make up a checksum here so we can "serialize" it to our unit test in-proc "remote" host.
            var checksumArray = Guid.ToByteArray();
            Array.Resize(ref checksumArray, Checksum.HashSize);
            _checksum = Checksum.From(checksumArray);
        }

        public TestGeneratorReference(IIncrementalGenerator generator)
            : this(generator.AsSourceGenerator())
        {
        }

        public override string? FullPath => null;
        public override object Id => this;
        public Guid Guid { get; }

        Checksum IChecksummedObject.Checksum => _checksum;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => ImmutableArray<DiagnosticAnalyzer>.Empty;
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => ImmutableArray<DiagnosticAnalyzer>.Empty;
        public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => ImmutableArray.Create(_generator);
    }
}
