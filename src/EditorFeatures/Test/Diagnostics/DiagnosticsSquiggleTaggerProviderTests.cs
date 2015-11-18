// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{

    internal class DiagnosticTaggerWrapper : IDisposable
    {
        private readonly TestWorkspace workspace;
        private readonly DiagnosticAnalyzerService analyzerService;
        private readonly ISolutionCrawlerRegistrationService registrationService;
        private readonly ImmutableArray<IIncrementalAnalyzer> incrementalAnalyzers;
        private readonly SolutionCrawlerRegistrationService solutionCrawlerService;
        private readonly AsynchronousOperationListener asyncListener;
        private readonly DiagnosticService diagnosticService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> listeners;

        private DiagnosticsSquiggleTaggerProvider _taggerProvider;

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null,
            bool createTaggerProvider = true) 
            : this(workspace, CreateDiagnosticAnalyzerService(analyzerMap), updateSource: null, createTaggerProvider: createTaggerProvider)
        {
        }

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            IDiagnosticUpdateSource updateSource,
            bool createTaggerProvider = true)
            : this(workspace, null, updateSource, createTaggerProvider)
        {
        }

        private static DiagnosticAnalyzerService CreateDiagnosticAnalyzerService(Dictionary<string, DiagnosticAnalyzer[]> analyzerMap)
        {
            return analyzerMap == null || analyzerMap.Count == 0
                ? new TestDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap())
                : new TestDiagnosticAnalyzerService(analyzerMap.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()));
        }

        private DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            DiagnosticAnalyzerService analyzerService,
            IDiagnosticUpdateSource updateSource,
            bool createTaggerProvider)
        {
            if (updateSource == null)
            {
                updateSource = analyzerService;
            }

            this.workspace = workspace;

            this.registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            registrationService.Register(workspace);

            this.asyncListener = new AsynchronousOperationListener();
            this.listeners = AsynchronousOperationListener.CreateListeners(
                ValueTuple.Create(FeatureAttribute.DiagnosticService, asyncListener),
                ValueTuple.Create(FeatureAttribute.ErrorSquiggles, asyncListener));

            this.analyzerService = analyzerService;
            this.diagnosticService = new DiagnosticService(listeners);
            diagnosticService.Register(updateSource);

            if (createTaggerProvider)
            {
                var taggerProvider = this.TaggerProvider;
            }

            if (analyzerService != null)
            {
                this.incrementalAnalyzers = ImmutableArray.Create(analyzerService.CreateIncrementalAnalyzer(workspace));
                this.solutionCrawlerService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
            }
        }

        public DiagnosticsSquiggleTaggerProvider TaggerProvider
        {
            get
            {
                if (_taggerProvider == null)
                {
                    _taggerProvider = new DiagnosticsSquiggleTaggerProvider(
                        workspace.Services.GetService<IOptionService>(), diagnosticService,
                        workspace.GetService<IForegroundNotificationService>(), listeners);
                }

                return _taggerProvider;
            }
        }



        public void Dispose()
        {
            registrationService.Unregister(workspace);
        }

        public async Task WaitForTags()
        {
            if (solutionCrawlerService != null)
            {
                solutionCrawlerService.WaitUntilCompletion_ForTestingPurposesOnly(workspace, incrementalAnalyzers);
            }

            await asyncListener.CreateWaitTask();
        }
    }

    public class DiagnosticsSquiggleTaggerProviderTests
    {
        [WpfFact(Skip ="xunit"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task Test_TagSourceDiffer()
        {
            var analyzer = new Analyzer();
            var analyzerMap = new Dictionary<string, DiagnosticAnalyzer[]>
            {
                { LanguageNames.CSharp, new DiagnosticAnalyzer[] { analyzer } }
            };

            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class A { }", "class E { }" }, CSharpParseOptions.Default))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, analyzerMap))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    // test first update
                    await wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.True(spans.First().Span.Contains(new Span(0, 1)));

                    // test second update
                    analyzer.ChangeSeverity();

                    var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                    var text = document.GetTextAsync().Result;
                    workspace.TryApplyChanges(document.WithText(text.WithChanges(new TextChange(new TextSpan(text.Length - 1, 1), string.Empty))).Project.Solution);

                    await wrapper.WaitForTags();

                    snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.True(spans.First().Span.Contains(new Span(0, 1)));
                }
            }
        }

        [WpfFact(Skip = "xunit"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task MultipleTaggersAndDispose()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class A {" }, CSharpParseOptions.Default))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace))
            {
                // Make two taggers.
                var tagger1 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                var tagger2 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());

                // But dispose the first one. We still want the second one to work.
                ((IDisposable)tagger1).Dispose();

                using (var disposable = tagger2 as IDisposable)
                {
                    await wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger2.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.False(spans.IsEmpty());
                }
            }
        }

        [WpfFact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task TaggerProviderCreatedAfterInitialDiagnosticsReported()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class C {" }, CSharpParseOptions.Default))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace, analyzerMap: null, createTaggerProvider: false))
            {
                // First, make sure all diagnostics have been reported.
                await wrapper.WaitForTags();

                // Now make the tagger.
                var taggerProvider = wrapper.TaggerProvider;

                // Make a taggers.
                var tagger1 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger1 as IDisposable)
                {
                    await wrapper.WaitForTags();

                    // We should have tags at this point.
                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger1.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.False(spans.IsEmpty());
                }
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
                    c.ReportDiagnostic(Diagnostic.Create(_rule, Location.Create(c.Tree, new Text.TextSpan(0, 1))));
                });
            }

            public void ChangeSeverity()
            {
                _rule = new DiagnosticDescriptor("test", "test", "test", "test", DiagnosticSeverity.Warning, true);
            }
        }
    }
}
