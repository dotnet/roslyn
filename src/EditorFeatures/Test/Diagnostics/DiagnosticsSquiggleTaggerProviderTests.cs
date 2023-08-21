// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    [UseExportProvider]
    [Trait(Traits.Feature, Traits.Features.Diagnostics), Trait(Traits.Feature, Traits.Features.Tagging)]
    public class DiagnosticsSquiggleTaggerProviderTests
    {
        private static readonly TestComposition s_compositionWithMockDiagnosticService =
            EditorTestCompositions.EditorFeatures
                .AddExcludedPartTypes(typeof(IDiagnosticService), typeof(IDiagnosticAnalyzerService))
                .AddParts(typeof(MockDiagnosticService), typeof(MockDiagnosticAnalyzerService));

        [WpfFact]
        public async Task Test_TagSourceDiffer()
        {
            var analyzer = new Analyzer();
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>
            {
                {  LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(analyzer) }
            };

            using var workspace = TestWorkspace.CreateCSharp(new string[] { "class A { }", "class E { }" }, parseOptions: CSharpParseOptions.Default);

            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace, analyzerMap);

            var firstDocument = workspace.Documents.First();
            var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());
            using var disposable = tagger as IDisposable;
            // test first update
            await wrapper.WaitForTags();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            Assert.True(spans.First().Span.Contains(new Span(0, 1)));

            // test second update
            analyzer.ChangeSeverity();

            var document = workspace.CurrentSolution.GetRequiredDocument(firstDocument.Id);
            var text = await document.GetTextAsync();
            workspace.TryApplyChanges(document.WithText(text.WithChanges(new TextChange(new TextSpan(text.Length - 1, 1), string.Empty))).Project.Solution);

            await wrapper.WaitForTags();

            snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            Assert.True(spans.First().Span.Contains(new Span(0, 1)));
        }

        [WpfFact]
        public async Task MultipleTaggersAndDispose()
        {
            using var workspace = TestWorkspace.CreateCSharp(new string[] { "class A {" }, parseOptions: CSharpParseOptions.Default);

            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace);

            // Make two taggers.
            var firstDocument = workspace.Documents.First();
            var tagger1 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());
            var tagger2 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());

            // But dispose the first one. We still want the second one to work.
            ((IDisposable)tagger1).Dispose();

            using var disposable = tagger2 as IDisposable;
            await wrapper.WaitForTags();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger2.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            Assert.False(spans.IsEmpty());
        }

        [WpfFact]
        public async Task TaggerProviderCreatedAfterInitialDiagnosticsReported()
        {
            using var workspace = TestWorkspace.CreateCSharp(new string[] { "class C {" }, parseOptions: CSharpParseOptions.Default);

            using var wrapper = new DiagnosticTaggerWrapper<DiagnosticsSquiggleTaggerProvider, IErrorTag>(workspace, analyzerMap: null, createTaggerProvider: false);
            // First, make sure all diagnostics have been reported.
            await wrapper.WaitForTags();

            // Now make the tagger.
            var taggerProvider = wrapper.TaggerProvider;

            // Make a taggers.
            var firstDocument = workspace.Documents.First();
            var tagger1 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());
            using var disposable = tagger1 as IDisposable;
            await wrapper.WaitForTags();

            // We should have tags at this point.
            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger1.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            Assert.False(spans.IsEmpty());
        }

        [WpfTheory]
        [InlineData(DiagnosticKind.CompilerSyntax, false)]
        [InlineData(DiagnosticKind.CompilerSemantic, false)]
        [InlineData(DiagnosticKind.AnalyzerSyntax, false)]
        [InlineData(DiagnosticKind.AnalyzerSemantic, false)]
        // Test no tags for suppressed analyzer diagnostics. NOTE: Compiler errors cannot be suppressed.
        [InlineData(DiagnosticKind.AnalyzerSyntax, true)]
        [InlineData(DiagnosticKind.AnalyzerSemantic, true)]
        internal async Task TestWithMockDiagnosticService_TaggerProviderCreatedBeforeInitialDiagnosticsReported(DiagnosticKind diagnosticKind, bool isSuppressed)
        {
            // This test produces diagnostics from a mock service so that we are disconnected from
            // all the asynchrony of the actual async analyzer engine.  If this fails, then the 
            // issue is almost certainly in the DiagnosticsSquiggleTaggerProvider code.  If this
            // succeed, but other squiggle tests fail, then it is likely an issue with the 
            // diagnostics engine not actually reporting all diagnostics properly.

            using var workspace = TestWorkspace.CreateCSharp(
                new string[] { "class A { }" },
                parseOptions: CSharpParseOptions.Default,
                composition: s_compositionWithMockDiagnosticService);

            var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            var diagnosticService = Assert.IsType<MockDiagnosticService>(workspace.ExportProvider.GetExportedValue<IDiagnosticService>());
            var analyzerService = Assert.IsType<MockDiagnosticAnalyzerService>(workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var provider = workspace.ExportProvider.GetExportedValues<ITaggerProvider>().OfType<DiagnosticsSquiggleTaggerProvider>().Single();

            // Create the tagger before the first diagnostic event has been fired.
            var firstDocument = workspace.Documents.First();
            var tagger = provider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());
            Contract.ThrowIfNull(tagger);

            // Now product the first diagnostic and fire the events.
            var tree = await workspace.CurrentSolution.Projects.Single().Documents.Single().GetRequiredSyntaxTreeAsync(CancellationToken.None);
            var span = TextSpan.FromBounds(0, 5);
            diagnosticService.CreateDiagnosticAndFireEvents(workspace, analyzerService, Location.Create(tree, span), diagnosticKind, isSuppressed);

            using var disposable = tagger as IDisposable;
            await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            if (isSuppressed)
            {
                Assert.Empty(spans);
            }
            else
            {
                Assert.Equal(1, spans.Count);
                Assert.Equal(span.ToSpan(), spans[0].Span.Span);
            }
        }

        [WpfTheory]
        [InlineData(DiagnosticKind.CompilerSyntax, false)]
        [InlineData(DiagnosticKind.CompilerSemantic, false)]
        [InlineData(DiagnosticKind.AnalyzerSyntax, false)]
        [InlineData(DiagnosticKind.AnalyzerSemantic, false)]
        // Test no tags for suppressed analyzer diagnostics. NOTE: Compiler errors cannot be suppressed.
        [InlineData(DiagnosticKind.AnalyzerSyntax, true)]
        [InlineData(DiagnosticKind.AnalyzerSemantic, true)]
        internal async Task TestWithMockDiagnosticService_TaggerProviderCreatedAfterInitialDiagnosticsReported(DiagnosticKind diagnosticKind, bool isSuppressed)
        {
            // This test produces diagnostics from a mock service so that we are disconnected from
            // all the asynchrony of the actual async analyzer engine.  If this fails, then the 
            // issue is almost certainly in the DiagnosticsSquiggleTaggerProvider code.  If this
            // succeed, but other squiggle tests fail, then it is likely an issue with the 
            // diagnostics engine not actually reporting all diagnostics properly.

            using var workspace = TestWorkspace.CreateCSharp(
                new string[] { "class A { }" },
                parseOptions: CSharpParseOptions.Default,
                composition: s_compositionWithMockDiagnosticService);

            var listenerProvider = workspace.ExportProvider.GetExportedValue<IAsynchronousOperationListenerProvider>();

            var diagnosticService = Assert.IsType<MockDiagnosticService>(workspace.ExportProvider.GetExportedValue<IDiagnosticService>());
            var analyzerService = Assert.IsType<MockDiagnosticAnalyzerService>(workspace.ExportProvider.GetExportedValue<IDiagnosticAnalyzerService>());
            var provider = workspace.ExportProvider.GetExportedValues<ITaggerProvider>().OfType<DiagnosticsSquiggleTaggerProvider>().Single();

            // Create and fire the diagnostic events before the tagger is even made.
            var tree = await workspace.CurrentSolution.Projects.Single().Documents.Single().GetRequiredSyntaxTreeAsync(CancellationToken.None);
            var span = TextSpan.FromBounds(0, 5);
            diagnosticService.CreateDiagnosticAndFireEvents(workspace, analyzerService, Location.Create(tree, span), diagnosticKind, isSuppressed);

            var firstDocument = workspace.Documents.First();
            var tagger = provider.CreateTagger<IErrorTag>(firstDocument.GetTextBuffer());
            Contract.ThrowIfNull(tagger);

            using var disposable = tagger as IDisposable;
            await listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            await listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync();

            var snapshot = firstDocument.GetTextBuffer().CurrentSnapshot;
            var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
            if (isSuppressed)
            {
                Assert.Empty(spans);
            }
            else
            {
                Assert.Equal(1, spans.Count);
                Assert.Equal(span.ToSpan(), spans[0].Span.Span);
            }
        }

        private class Analyzer : DiagnosticAnalyzer
        {
            private DiagnosticDescriptor _rule = new DiagnosticDescriptor("test", "test", "test", "test", DiagnosticSeverity.Error, true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            {
                get
                {
                    return ImmutableArray.Create(_rule);
                }
            }

            public override void Initialize(AnalysisContext context)
            {
                context.RegisterSyntaxTreeAction(c =>
                {
                    c.ReportDiagnostic(Diagnostic.Create(_rule, Location.Create(c.Tree, new TextSpan(0, 1))));
                });
            }

            public void ChangeSeverity()
                => _rule = new DiagnosticDescriptor("test", "test", "test", "test", DiagnosticSeverity.Warning, true);
        }
    }
}
