// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.NET.Sdk.Razor.SourceGenerators;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal partial class RazorProjectBuilder
{
    private sealed class DeterministicRazorSourceGeneratorReference : AnalyzerReference, SerializerService.TestAccessor.IAnalyzerReferenceWithGuid
    {
        private readonly AnalyzerReference _inner = new AnalyzerFileReference(typeof(RazorSourceGenerator).Assembly.Location, TestAnalyzerAssemblyLoader.LoadFromFile);

        // SourceGeneratedDocumentIdentity hashes AnalyzerReference.FullPath into generated DocumentIds.
        // Keep that path stable in tests while still loading the generator from the real assembly location.
        // The exact value is arbitrary, but changing it can change source-generated document ordering and code-action baselines.
        public override string? FullPath => "/_/RazorSourceGenerator.dll";

        public override string Display => _inner.Display;
        public override object Id => _inner.Id;

        public Guid Guid { get; } = Guid.NewGuid();

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages() => _inner.GetAnalyzersForAllLanguages();
        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language) => _inner.GetAnalyzers(language);
        public override ImmutableArray<ISourceGenerator> GetGeneratorsForAllLanguages() => _inner.GetGeneratorsForAllLanguages();
        public override ImmutableArray<ISourceGenerator> GetGenerators(string language) => _inner.GetGenerators(language);
    }
}
