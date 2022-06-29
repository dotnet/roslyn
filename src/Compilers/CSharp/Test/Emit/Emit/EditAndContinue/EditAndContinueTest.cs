// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed partial class EditAndContinueTest : IDisposable
    {
        private readonly CSharpCompilationOptions? _options;
        private readonly TargetFramework _targetFramework;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<GenerationInfo> _generations = new();

        private bool _hasVerified;

        public EditAndContinueTest(CSharpCompilationOptions? options, TargetFramework? targetFramework)
        {
            _options = options;
            _targetFramework = targetFramework ?? TargetFramework.Standard;
        }

        internal EditAndContinueTest AddGeneration(string source, Action<GenerationVerifier> validator)
        {
            _hasVerified = false;

            Assert.Empty(_generations);

            var compilation = CSharpTestBase.CreateCompilation(source, options: _options, targetFramework: _targetFramework);

            var bytes = compilation.EmitToArray();
            var md = ModuleMetadata.CreateFromImage(bytes);
            _disposables.Add(md);

            var baseline = EmitBaseline.CreateInitialBaseline(md, EditAndContinueTestBase.EmptyLocalsProvider);

            _generations.Add(new GenerationInfo(compilation, md.MetadataReader, baseline, validator));

            return this;
        }

        internal EditAndContinueTest AddGeneration(string source, SemanticEditDescription[] edits, Action<GenerationVerifier> validator)
        {
            _hasVerified = false;

            Assert.NotEmpty(_generations);

            var prevGeneration = _generations[^1];

            var compilation = prevGeneration.Compilation.WithSource(source);

            var semanticEdits = GetSemanticEdits(edits, prevGeneration.Compilation, compilation);

            var diff = compilation.EmitDifference(prevGeneration.Baseline, semanticEdits);

            var md = diff.GetMetadata();
            _disposables.Add(md);

            _generations.Add(new GenerationInfo(compilation, md.Reader, diff.NextGeneration, validator));

            return this;
        }

        internal EditAndContinueTest Verify()
        {
            _hasVerified = true;

            Assert.NotEmpty(_generations);

            var readers = new List<MetadataReader>();
            int index = 0;
            foreach (var generation in _generations)
            {
                if (readers.Count > 0)
                {
                    EncValidation.VerifyModuleMvid(index, readers[^1], generation.MetadataReader);
                }

                readers.Add(generation.MetadataReader);
                var verifier = new GenerationVerifier(index, generation.MetadataReader, readers);
                generation.Verifier(verifier);

                index++;
            }

            return this;
        }

        private ImmutableArray<SemanticEdit> GetSemanticEdits(SemanticEditDescription[] edits, Compilation oldCompilation, Compilation newCompilation)
        {
            return ImmutableArray.CreateRange(edits.Select(e => new SemanticEdit(e.Kind, e.SymbolProvider(oldCompilation), e.SymbolProvider(newCompilation))));
        }

        public void Dispose()
        {
            Assert.True(_hasVerified, "No Verify call since the last AddGeneration call.");
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
