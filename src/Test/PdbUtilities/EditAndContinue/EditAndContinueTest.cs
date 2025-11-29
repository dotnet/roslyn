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
using System.Text;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal abstract partial class EditAndContinueTest<TSelf>(ITestOutputHelper? output = null, Verification? verification = null) : IDisposable
        where TSelf : EditAndContinueTest<TSelf>
    {
        private readonly Verification _verification = verification ?? Verification.Passes;
        private readonly List<IDisposable> _disposables = [];
        private readonly List<GenerationInfo> _generations = [];
        private readonly List<SourceWithMarkedNodes> _sources = [];

        private bool _hasVerified;

        protected abstract Compilation CreateCompilation(SyntaxTree tree);
        protected abstract SourceWithMarkedNodes CreateSourceWithMarkedNodes(string source);
        protected abstract Func<SyntaxNode, SyntaxNode> GetEquivalentNodesMap(ISymbol left, ISymbol right);

        private TSelf This => (TSelf)this;

        internal TSelf AddBaseline(
            string source,
            Action<GenerationVerifier>? validator = null,
            Func<MethodDefinitionHandle, EditAndContinueMethodDebugInformation>? debugInformationProvider = null,
            IEnumerable<ResourceDescription>? manifestResources = null)
        {
            _hasVerified = false;

            Assert.Empty(_generations);

            var markedSource = CreateSourceWithMarkedNodes(source);

            var compilation = CreateCompilation(markedSource.Tree);

            var verifier = new CompilationVerifier(compilation);

            output?.WriteLine($"Emitting baseline");

            verifier.EmitAndVerify(
                expectedOutput: null,
                trimOutput: false,
                expectedReturnCode: null,
                args: null,
                manifestResources,
                emitOptions: EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb),
                peVerify: _verification,
                expectedSignatures: null);

            var md = ModuleMetadata.CreateFromImage(verifier.EmittedAssemblyData);
            _disposables.Add(md);

            var baseline = EditAndContinueTestUtilities.CreateInitialBaseline(compilation, md, debugInformationProvider ?? verifier.CreateSymReader().GetEncMethodDebugInfo);

            _generations.Add(new GenerationInfo(compilation, md.MetadataReader, diff: null, verifier, baseline, validator ?? new(x => { })));
            _sources.Add(markedSource);

            return This;
        }

        internal TSelf AddGeneration(string source, SemanticEditDescription[] edits, Action<GenerationVerifier> validator, EmitDifferenceOptions? options = null)
            => AddGeneration(source, _ => edits, validator, options);

        internal TSelf AddGeneration(string source, Func<SourceWithMarkedNodes, SemanticEditDescription[]> edits, Action<GenerationVerifier> validator, EmitDifferenceOptions? options = null)
            => AddGeneration(source, edits, validator, expectedErrors: [], options);

        internal TSelf AddGeneration(string source, SemanticEditDescription[] edits, DiagnosticDescription[] expectedErrors, EmitDifferenceOptions? options = null)
            => AddGeneration(source, _ => edits, validator: static _ => { }, expectedErrors, options);

        private TSelf AddGeneration(string source, Func<SourceWithMarkedNodes, SemanticEditDescription[]> edits, Action<GenerationVerifier> validator, DiagnosticDescription[] expectedErrors, EmitDifferenceOptions? options = null)
        {
            _hasVerified = false;

            Assert.NotEmpty(_generations);
            Assert.NotEmpty(_sources);

            var markedSource = CreateSourceWithMarkedNodes(source);
            var previousGeneration = _generations[^1];
            var previousSource = _sources[^1];

            var compilation = previousGeneration.Compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(markedSource.Tree);
            var unmappedNodes = new List<SyntaxNode>();

            var semanticEdits = GetSemanticEdits(edits(markedSource), previousGeneration.Compilation, previousSource, compilation, markedSource, unmappedNodes);

            output?.WriteLine($"Emitting generation #{_generations.Count}");

            var r = new ResourceDescription(
                "MyResource",
                () =>
                {
                    var s = new MemoryStream();
                    s.WriteByte(12);
                    return s;
                },
                isPublic: true);

            CompilationDifference diff = compilation.EmitDifference(previousGeneration.Baseline, semanticEdits, resourceEdits: [new(ResourceEditKind.Insert, r)], options: options);

            diff.EmitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Verify(expectedErrors);
            if (expectedErrors is not [])
            {
                return This;
            }

            var md = diff.GetMetadata();
            _disposables.Add(md);

            _generations.Add(new GenerationInfo(compilation, md.Reader, diff, compilationVerifier: null, diff.NextGeneration, validator));
            _sources.Add(markedSource);

            return This;
        }

        internal TSelf Verify()
        {
            _hasVerified = true;

            Assert.NotEmpty(_generations);

            var readers = new List<MetadataReader>();
            int index = 0;
            var exceptions = new List<ImmutableArray<Exception>>();

            foreach (var generation in _generations)
            {
                if (readers.Count > 0)
                {
                    EncValidation.VerifyModuleMvid(index, readers[^1], generation.MetadataReader);
                }

                readers.Add(generation.MetadataReader);
                var verifier = new GenerationVerifier(index, generation, [.. readers]);
                generation.Verifier(verifier);

                exceptions.Add([.. verifier.Exceptions]);

                index++;
            }

            var assertMessage = GetAggregateMessage(exceptions);
            Assert.True(assertMessage == "", assertMessage);

            return This;
        }

        private static string GetAggregateMessage(IReadOnlyList<ImmutableArray<Exception>> exceptions)
        {
            var builder = new StringBuilder();
            for (int generation = 0; generation < exceptions.Count; generation++)
            {
                if (exceptions[generation].Any())
                {
                    builder.AppendLine($"-------------------------------------");
                    builder.AppendLine($" Generation #{generation} failures");
                    builder.AppendLine($"-------------------------------------");

                    foreach (var exception in exceptions[generation])
                    {
                        builder.AppendLine(exception.Message);
                        builder.AppendLine();
                        builder.AppendLine(exception.StackTrace);
                    }
                }
            }

            return builder.ToString();
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

            return [.. edits.Select(e =>
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

                    syntaxMap = syntaxMapFromMarkers ?? GetEquivalentNodesMap(newSymbol, oldSymbol);
                }
                else
                {
                    syntaxMap = null;
                }

                return new SemanticEdit(e.Kind, oldSymbol, newSymbol, syntaxMap, e.RudeEdits);
            })];
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
