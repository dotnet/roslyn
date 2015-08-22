// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    public class DiagnosticsSquiggleTaggerProviderTests
    {
        private class TaggerWrapper : IDisposable
        {
            public readonly DiagnosticsSquiggleTaggerProvider TaggerProvider;
            public readonly Analyzer Analyzer;

            private readonly TestWorkspace workspace;
            private readonly TestDiagnosticAnalyzerService analyzerService;
            private readonly ISolutionCrawlerRegistrationService registrationService;
            private readonly ImmutableArray<IIncrementalAnalyzer> incrementalAnalyzers;
            private readonly SolutionCrawlerRegistrationService solutionCrawlerService;
            private readonly AsynchronousOperationListener asyncListener;

            public TaggerWrapper(TestWorkspace workspace)
            {
                this.workspace = workspace;

                this.registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
                registrationService.Register(workspace);

                this.Analyzer = new Analyzer();
                this.analyzerService = new TestDiagnosticAnalyzerService(
                    new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>() { { LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(this.Analyzer) } }.ToImmutableDictionary());
                this.asyncListener = new AsynchronousOperationListener();
                var listeners = AsynchronousOperationListener.CreateListeners(
                    ValueTuple.Create(FeatureAttribute.DiagnosticService, asyncListener),
                    ValueTuple.Create(FeatureAttribute.ErrorSquiggles, asyncListener));

                var diagnosticService = new DiagnosticService(SpecializedCollections.SingletonEnumerable<IDiagnosticUpdateSource>(analyzerService), listeners);

                this.TaggerProvider = new DiagnosticsSquiggleTaggerProvider(
                    workspace.Services.GetService<IOptionService>(), diagnosticService,
                    workspace.GetService<IForegroundNotificationService>(), listeners);

                this.incrementalAnalyzers = ImmutableArray.Create(analyzerService.CreateIncrementalAnalyzer(workspace));
                this.solutionCrawlerService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
            }

            public void Dispose()
            {
                registrationService.Unregister(workspace);
            }

            public void WaitForTags()
            {
                solutionCrawlerService.WaitUntilCompletion_ForTestingPurposesOnly(workspace, incrementalAnalyzers);
                asyncListener.CreateWaitTask().PumpingWait();
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void Test_TagSourceDiffer()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class A { }", "class E { }" }, CSharpParseOptions.Default))
            using (var wrapper = new TaggerWrapper(workspace))
            {
                var tagger = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    // test first update
                    wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToList();
                    Assert.True(spans.First().Span.Contains(new Span(0, 1)));

                    // test second update
                    wrapper.Analyzer.ChangeSeverity();

                    var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                    var text = document.GetTextAsync().Result;
                    workspace.TryApplyChanges(document.WithText(text.WithChanges(new TextChange(new TextSpan(text.Length - 1, 1), string.Empty))).Project.Solution);

                    wrapper.WaitForTags();

                    snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    spans = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToList();
                    Assert.True(spans.First().Span.Contains(new Span(0, 1)));
                }
            }
        }

        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void MultipleTaggersAndDispose()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class A {" }, CSharpParseOptions.Default))
            using (var wrapper = new TaggerWrapper(workspace))
            {
                // Make two taggers.
                var tagger1 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                var tagger2 = wrapper.TaggerProvider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());

                // But dispose the first one. We still want the second one to work.
                ((IDisposable)tagger1).Dispose();

                using (var disposable = tagger2 as IDisposable)
                {
                    wrapper.WaitForTags();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger2.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToList();
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