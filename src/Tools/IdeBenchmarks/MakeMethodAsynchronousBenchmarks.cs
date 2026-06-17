// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Composition;

namespace IdeBenchmarks
{
    /// <summary>
    /// Measures the cost of applying the "Make method async" code fix in a large single-project solution
    /// (1000+ documents) where the fixed method is <em>not</em> used as an event handler.
    ///
    /// The branch under test moved an expensive find-all-references (FAR) probe out of
    /// <c>RegisterCodeFixesAsync</c> (the light bulb) and into <c>FixNodeAsync</c> (preview/apply). The probe only runs
    /// for the <c>async Task</c> fix on a <c>void</c>-returning method, and the "no event handler" scenario is the
    /// worst case because the search can never short-circuit on a positive match.
    ///
    /// Three benchmarks isolate the impact:
    /// <list type="bullet">
    /// <item><see cref="RegisterCodeFixes"/> – light bulb cost; should be cheap now that the FAR is deferred.</item>
    /// <item><see cref="ApplyMakeAsyncTask"/> – applies the <c>async Task</c> fix, which runs the FAR probe.</item>
    /// <item><see cref="ApplyMakeAsyncVoid"/> – applies the <c>async void</c> fix, which short-circuits the probe
    /// (baseline). The difference versus <see cref="ApplyMakeAsyncTask"/> is the cost added by the change.</item>
    /// </list>
    /// </summary>
    [MemoryDiagnoser]
    public class MakeMethodAsynchronousBenchmarks
    {
        private const string TargetDocumentName = "Target.cs";

        // The method already ends in "Async" and is a plain instance method, so the fix neither renames the symbol
        // nor walks partial parts. That keeps the only project-size-sensitive work in FixNodeAsync the new FAR probe.
        private const string TargetSource = """
            using System.Threading.Tasks;

            namespace BenchmarkTarget
            {
                public class TargetClass
                {
                    public void HandlerAsync()
                    {
                        await Task.Delay(1);
                    }
                }
            }
            """;

        /// <summary>
        /// Total number of documents in the (single) project that declares the method being fixed. The event-handler
        /// search scope is bounded, so growing this should show the apply cost staying roughly flat.
        /// </summary>
        [Params(100, 1000, 2000)]
        public int DocumentCount { get; set; }

        private readonly UseExportProviderAttribute _useExportProviderAttribute = new();

        private AdhocWorkspace _workspace;
        private Solution _solution;
        private Document _targetDocument;
        private Diagnostic _diagnostic;
        private CodeFixProvider _provider;
        private CodeAction _makeAsyncTaskAction;
        private CodeAction _makeAsyncVoidAction;

        [GlobalSetup]
        public void GlobalSetup()
        {
            // Enables the test export-provider cache for the lifetime of this benchmark process and builds a fully
            // composed Features host (CSharp features + workspaces) so the real exported code fix runs.
            _useExportProviderAttribute.Before(null);

            // Features composition (CSharp features + workspaces) without the editor/WPF layer. This is enough to run
            // the exported code fix and SymbolFinder.FindReferencesAsync in-process.
            var hostServices = FeaturesTestCompositions.Features.GetHostServices(out var exportProvider);

            _provider = exportProvider
                .GetExportedValues<CodeFixProvider>()
                .Single(p => p.GetType().Name == "CSharpMakeMethodAsynchronousCodeFixProvider");

            _workspace = new AdhocWorkspace(hostServices);
            _solution = BuildSolution(_workspace, DocumentCount, out var targetDocumentId);
            _targetDocument = _solution.GetDocument(targetDocumentId);

            // Force the compilation and find the awaitable-in-non-async-method diagnostic the fix attaches to.
            var semanticModel = _targetDocument.GetSemanticModelAsync(CancellationToken.None).GetAwaiter().GetResult();
            _diagnostic = semanticModel.GetDiagnostics()
                .First(d => _provider.FixableDiagnosticIds.Contains(d.Id));

            var actions = RegisterFixesAsync().GetAwaiter().GetResult();
            if (actions.Length != 2)
                throw new InvalidOperationException($"Expected exactly 2 code actions (Task + void), found {actions.Length}.");

            // RegisterCodeFixesAsync always registers the `async Task` fix first, then the `async void` fix.
            _makeAsyncTaskAction = actions[0];
            _makeAsyncVoidAction = actions[1];

            // Warm caches (compilation, syntactic indices) so steady-state measurements reflect the FAR work itself.
            _ = _makeAsyncTaskAction.GetOperationsAsync(_solution, CodeAnalysisProgress.None, CancellationToken.None).GetAwaiter().GetResult();
            _ = _makeAsyncVoidAction.GetOperationsAsync(_solution, CodeAnalysisProgress.None, CancellationToken.None).GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _workspace?.Dispose();
            _workspace = null;
            _solution = null;
            _targetDocument = null;
            _diagnostic = null;
            _provider = null;
            _makeAsyncTaskAction = null;
            _makeAsyncVoidAction = null;

            _useExportProviderAttribute.After(null);
        }

        /// <summary>
        /// Cost of computing the offered fixes (the light bulb). After the change this should be inexpensive because
        /// the FAR probe is no longer performed here.
        /// </summary>
        [Benchmark]
        public async Task<int> RegisterCodeFixes()
        {
            var actions = await RegisterFixesAsync().ConfigureAwait(false);
            return actions.Length;
        }

        /// <summary>
        /// Cost of applying the <c>async Task</c> fix. This path runs the event-handler FAR probe added by the change.
        /// </summary>
        [Benchmark]
        public async Task<ImmutableArray<CodeActionOperation>> ApplyMakeAsyncTask()
        {
            return await _makeAsyncTaskAction.GetOperationsAsync(
                _solution, CodeAnalysisProgress.None, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Baseline: cost of applying the <c>async void</c> fix, which short-circuits the FAR probe.
        /// </summary>
        [Benchmark(Baseline = true)]
        public async Task<ImmutableArray<CodeActionOperation>> ApplyMakeAsyncVoid()
        {
            return await _makeAsyncVoidAction.GetOperationsAsync(
                _solution, CodeAnalysisProgress.None, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<CodeAction>> RegisterFixesAsync()
        {
            var actions = new List<CodeAction>();
            var context = new CodeFixContext(
                _targetDocument,
                _diagnostic,
                (action, _) => actions.Add(action),
                CancellationToken.None);

            await _provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
            return actions.ToImmutableArray();
        }

        private static Solution BuildSolution(AdhocWorkspace workspace, int documentCount, out DocumentId targetDocumentId)
        {
            var references = new[]
            {
                typeof(object),
                typeof(Task),
                typeof(Enumerable),
                typeof(Uri),
            }
            .Select(t => t.Assembly.Location)
            .Distinct()
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

            var projectId = ProjectId.CreateNewId();
            targetDocumentId = DocumentId.CreateNewId(projectId);

            var documents = ImmutableArray.CreateBuilder<DocumentInfo>(documentCount + 1);

            for (var i = 0; i < documentCount; i++)
            {
                var name = $"Filler{i}.cs";
                var text = $$"""
                    namespace Filler{{i}}
                    {
                        internal sealed class Filler{{i}}
                        {
                            internal int Value => {{i}};
                        }
                    }
                    """;
                documents.Add(CreateDocument(DocumentId.CreateNewId(projectId), name, text));
            }

            documents.Add(CreateDocument(targetDocumentId, TargetDocumentName, TargetSource));

            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: "BigProject",
                assemblyName: "BigProject",
                language: LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                documents: documents.ToImmutable(),
                metadataReferences: references);

            var project = workspace.AddProject(projectInfo);
            return project.Solution;
        }

        private static DocumentInfo CreateDocument(DocumentId id, string name, string text)
        {
            var loader = TextLoader.From(TextAndVersion.Create(SourceText.From(text, Encoding.UTF8), VersionStamp.Create(), name));
            return DocumentInfo.Create(id, name, loader: loader, filePath: name);
        }
    }
}
