// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal partial class MetadataTest : IDisposable
    {
        private readonly CSharpCompilationOptions? _options;
        private readonly TargetFramework _targetFramework;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<MetadataReader> _readers = new();

        private int _genId = 1;
        private GenerationInfo? _prevGeneration;

        public MetadataTest(CSharpCompilationOptions? options, TargetFramework? targetFramework)
        {
            _options = options;
            _targetFramework = targetFramework ?? TargetFramework.Standard;
        }

        internal MetadataTest AddGeneration(string source)
        {
            Assert.Null(_prevGeneration);

            var compilation = CSharpTestBase.CreateCompilation(source, options: _options, targetFramework: _targetFramework);

            var bytes = compilation.EmitToArray();
            var md = ModuleMetadata.CreateFromImage(bytes);
            _disposables.Add(md);

            var reader = md.MetadataReader;
            _readers.Add(reader);

            var baseline = EmitBaseline.CreateInitialBaseline(md, EditAndContinueTestBase.EmptyLocalsProvider);

            _prevGeneration = new GenerationInfo("initial compilation", _readers, compilation, reader, baseline);

            return this;
        }

        internal MetadataTest AddGeneration(string source, SemanticEditDescription[] edits)
        {
            Assert.NotNull(_prevGeneration);
            Debug.Assert(_prevGeneration is not null);

            var compilation = _prevGeneration.Compilation.WithSource(source);

            var semanticEdits = GetSemanticEdits(edits, _prevGeneration.Compilation, compilation);

            var diff = compilation.EmitDifference(_prevGeneration.Baseline, semanticEdits);

            var md = diff.GetMetadata();
            _disposables.Add(md);

            var reader = md.Reader;
            _readers.Add(reader);

            EncValidation.VerifyModuleMvid(_genId, _prevGeneration.MetadataReader, reader);

            _prevGeneration = new GenerationInfo($"generation {_genId++}", _readers, compilation, md.Reader, diff.NextGeneration);

            return this;
        }

        internal MetadataTest Verify(Action<GenerationInfo> verification)
        {
            Assert.NotNull(_prevGeneration);
            Debug.Assert(_prevGeneration is not null);

            verification(_prevGeneration);

            return this;
        }

        private ImmutableArray<SemanticEdit> GetSemanticEdits(SemanticEditDescription[] edits, Compilation oldCompilation, Compilation newCompilation)
        {
            return ImmutableArray.CreateRange(edits.Select(e => new SemanticEdit(e.Kind, e.SymbolProvider(oldCompilation), e.SymbolProvider(newCompilation))));
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
