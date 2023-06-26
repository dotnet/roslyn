// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

using Public = Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    internal sealed partial class EditAndContinueTest : IDisposable
    {
        private readonly CSharpCompilationOptions? _options;
        private readonly CSharpParseOptions? _parseOptions;
        private readonly TargetFramework _targetFramework;
        private readonly Verification _verification;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<GenerationInfo> _generations = new();
        private readonly List<SourceWithMarkedNodes> _sources = new();

        private bool _hasVerified;

        public EditAndContinueTest(
            CSharpCompilationOptions? options = null,
            CSharpParseOptions? parseOptions = null,
            TargetFramework targetFramework = TargetFramework.Standard,
            Verification? verification = null)
        {
            _options = options ?? TestOptions.DebugDll;
            _targetFramework = targetFramework;
            _parseOptions = parseOptions ?? TestOptions.Regular.WithNoRefSafetyRulesAttribute();
            _verification = verification ?? Verification.Passes;
        }

        internal EditAndContinueTest AddBaseline(string source, Action<GenerationVerifier> validator)
            => AddBaseline(EditAndContinueTestBase.MarkedSource(source, options: _parseOptions), validator);

        internal EditAndContinueTest AddBaseline(SourceWithMarkedNodes source, Action<GenerationVerifier> validator)
        {
            _hasVerified = false;

            Assert.Empty(_generations);

            var compilation = CSharpTestBase.CreateCompilation(source.Tree, options: _options, targetFramework: _targetFramework);

            var verifier = new CompilationVerifier(compilation);

            verifier.Emit(
                expectedOutput: null,
                trimOutput: false,
                expectedReturnCode: null,
                args: null,
                manifestResources: null,
                emitOptions: EmitOptions.Default,
                peVerify: _verification,
                expectedSignatures: null);

            var md = ModuleMetadata.CreateFromImage(verifier.EmittedAssemblyData);
            _disposables.Add(md);

            var baseline = EmitBaseline.CreateInitialBaseline(md, EditAndContinueTestBase.EmptyLocalsProvider);

            _generations.Add(new GenerationInfo(compilation, md.MetadataReader, diff: null, verifier, baseline, validator));
            _sources.Add(source);

            return this;
        }

        internal EditAndContinueTest AddGeneration(string source, SemanticEditDescription[] edits, Action<GenerationVerifier> validator)
            => AddGeneration(EditAndContinueTestBase.MarkedSource(source), edits, validator);

        internal EditAndContinueTest AddGeneration(SourceWithMarkedNodes source, SemanticEditDescription[] edits, Action<GenerationVerifier> validator)
        {
            _hasVerified = false;

            Assert.NotEmpty(_generations);
            Assert.NotEmpty(_sources);

            var previousGeneration = _generations[^1];
            var previousSource = _sources[^1];

            Assert.Equal(previousSource.MarkedSpans.IsEmpty, source.MarkedSpans.IsEmpty);

            var compilation = previousGeneration.Compilation.WithSource(source.Tree);

            var unmappedNodes = new List<SyntaxNode>();

            var semanticEdits = GetSemanticEdits(edits, previousGeneration.Compilation, previousSource, compilation, source, unmappedNodes);

            CompilationDifference diff;
            try
            {
                diff = compilation.EmitDifference(previousGeneration.Baseline, semanticEdits);
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception during generation #{_generations.Count}. See inner stack trace for details.", ex);
            }

            Assert.Empty(diff.EmitResult.Diagnostics);

            // EncVariableSlotAllocator attempted to map from current source to the previous one,
            // but the mapping failed for these nodes. Mark the nodes in sources with node markers <N:x>...</N:x>.
            Assert.Empty(unmappedNodes);

            var md = diff.GetMetadata();
            _disposables.Add(md);

            _generations.Add(new GenerationInfo(compilation, md.Reader, diff, compilationVerifier: null, diff.NextGeneration, validator));
            _sources.Add(source);

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
                var verifier = new GenerationVerifier(index, generation, readers);
                generation.Verifier(verifier);

                index++;
            }

            return this;
        }

        private ImmutableArray<SemanticEdit> GetSemanticEdits(
            SemanticEditDescription[] edits,
            Compilation oldCompilation,
            SourceWithMarkedNodes oldSource,
            Compilation newCompilation,
            SourceWithMarkedNodes newSource,
            List<SyntaxNode> unmappedNodes)
        {
            var syntaxMapFromMarkers = oldSource.MarkedSpans.IsEmpty ? null : SourceWithMarkedNodes.GetSyntaxMap(oldSource, newSource, unmappedNodes);

            return ImmutableArray.CreateRange(edits.Select(e =>
            {
                var oldSymbol = e.Kind is SemanticEditKind.Update or SemanticEditKind.Delete ? e.SymbolProvider(oldCompilation) : null;

                // for delete the new symbol is the new containing type
                var newSymbol = e.NewSymbolProvider(newCompilation);

                Func<SyntaxNode, SyntaxNode?>? syntaxMap;
                if (e.PreserveLocalVariables)
                {
                    Assert.Equal(SemanticEditKind.Update, e.Kind);
                    Debug.Assert(oldSymbol != null);
                    Debug.Assert(newSymbol != null);

                    syntaxMap = syntaxMapFromMarkers ?? EditAndContinueTestBase.GetEquivalentNodesMap(
                        ((Public.MethodSymbol)newSymbol).GetSymbol<MethodSymbol>(), ((Public.MethodSymbol)oldSymbol).GetSymbol<MethodSymbol>());
                }
                else
                {
                    syntaxMap = null;
                }

                return new SemanticEdit(e.Kind, oldSymbol, newSymbol, syntaxMap, e.PreserveLocalVariables);
            }));
        }

        public void Dispose()
        {
            // If the test has thrown an exception, or the test host has crashed, we don't want to assert here
            // or we'll hide it, so we need to do this dodgy looking thing.
            var isInException = Marshal.GetExceptionPointers() != IntPtr.Zero;

            Assert.True(isInException || _hasVerified, "No Verify call since the last AddGeneration call.");
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}
