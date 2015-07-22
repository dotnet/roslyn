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
    public class DiagnosticTagSourceTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public void Test_TagSourceDiffer()
        {
            using (var workspace = CSharpWorkspaceFactory.CreateWorkspaceFromFiles(new string[] { "class A { }", "class E { }" }, CSharpParseOptions.Default))
            {
                var registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
                registrationService.Register(workspace);

                var diagnosticWaiter = new DiagnosticServiceWaiter();
                var squiggleWaiter = new ErrorSquiggleWaiter();

                Analyzer analyzer;
                DiagnosticAnalyzerService analyzerService;
                ITagger<IErrorTag> tagger;
                GetTagger(workspace, diagnosticWaiter, squiggleWaiter, out analyzer, out analyzerService, out tagger);

                var service = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
                var incrementalAnalyzers = ImmutableArray.Create(analyzerService.CreateIncrementalAnalyzer(workspace));

                // test first update
                service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, incrementalAnalyzers);

                diagnosticWaiter.CreateWaitTask().PumpingWait();
                squiggleWaiter.CreateWaitTask().PumpingWait();

                var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                var spans = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToList();
                Assert.True(spans.First().Span.Contains(new Span(0, 1)));

                // test second update
                analyzer.ChangeSeverity();

                var document = workspace.CurrentSolution.GetDocument(workspace.Documents.First().Id);
                var text = document.GetTextAsync().Result;
                workspace.TryApplyChanges(document.WithText(text.WithChanges(new TextChange(new TextSpan(text.Length - 1, 1), string.Empty))).Project.Solution);

                service.WaitUntilCompletion_ForTestingPurposesOnly(workspace, incrementalAnalyzers);

                diagnosticWaiter.CreateWaitTask().PumpingWait();
                squiggleWaiter.CreateWaitTask().PumpingWait();

                snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                spans = tagger.GetTags(new NormalizedSnapshotSpanCollection(new SnapshotSpan(snapshot, 0, snapshot.Length))).ToList();
                Assert.True(spans.First().Span.Contains(new Span(0, 1)));

                ((IDisposable)tagger).Dispose();
                registrationService.Unregister(workspace);
            }
        }

        private static void GetTagger(TestWorkspace workspace, DiagnosticServiceWaiter diagnosticWaiter, ErrorSquiggleWaiter squiggleWaiter, out Analyzer analyzer, out DiagnosticAnalyzerService analyzerService, out ITagger<IErrorTag> tagger)
        {
            analyzer = new Analyzer();
            var analyzerMap = new Dictionary<string, ImmutableArray<DiagnosticAnalyzer>>() { { LanguageNames.CSharp, ImmutableArray.Create<DiagnosticAnalyzer>(analyzer) } };
            analyzerService = new TestDiagnosticAnalyzerService(analyzerMap.ToImmutableDictionary());

            var listeners = AsynchronousOperationListener.CreateListeners(
                ValueTuple.Create(FeatureAttribute.DiagnosticService, diagnosticWaiter),
                ValueTuple.Create(FeatureAttribute.ErrorSquiggles, diagnosticWaiter));

            var diagnosticService = new DiagnosticService(SpecializedCollections.SingletonEnumerable<IDiagnosticUpdateSource>(analyzerService), listeners);

            var document = workspace.Documents.First();
            var buffer = document.GetTextBuffer();

            var notificationService = workspace.GetService<IForegroundNotificationService>(); // new TestForegroundNotificationService();
            var optionsService = workspace.Services.GetService<IOptionService>();
            var provider = new DiagnosticsSquiggleTaggerProvider(optionsService, diagnosticService, notificationService, listeners);
            tagger = provider.CreateTagger<IErrorTag>(buffer);
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

        private class DiagnosticServiceWaiter : AsynchronousOperationListener { }
        private class ErrorSquiggleWaiter : AsynchronousOperationListener { }
    }
}