// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Common;
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
        private readonly TestWorkspace _workspace;
        public readonly DiagnosticAnalyzerService AnalyzerService;
        private readonly ISolutionCrawlerRegistrationService _registrationService;
        private readonly ImmutableArray<IIncrementalAnalyzer> _incrementalAnalyzers;
        private readonly SolutionCrawlerRegistrationService _solutionCrawlerService;
        private readonly AsynchronousOperationListener _asyncListener;
        public readonly DiagnosticService DiagnosticService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _listeners;

        private DiagnosticsSquiggleTaggerProvider _taggerProvider;

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap = null,
            bool createTaggerProvider = true)
            : this(workspace, analyzerMap, updateSource: null, createTaggerProvider: createTaggerProvider)
        {
        }

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            IDiagnosticUpdateSource updateSource,
            bool createTaggerProvider = true)
            : this(workspace, null, updateSource, createTaggerProvider)
        {
        }

        private static DiagnosticAnalyzerService CreateDiagnosticAnalyzerService(
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap, IAsynchronousOperationListener listener)
        {
            return analyzerMap == null || analyzerMap.Count == 0
                ? new MyDiagnosticAnalyzerService(DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap(), listener: listener)
                : new MyDiagnosticAnalyzerService(analyzerMap.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray()), listener: listener);
        }

        private DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            Dictionary<string, DiagnosticAnalyzer[]> analyzerMap,
            IDiagnosticUpdateSource updateSource,
            bool createTaggerProvider)
        {
            _asyncListener = new AsynchronousOperationListener();
            _listeners = AsynchronousOperationListener.CreateListeners(
                ValueTuple.Create(FeatureAttribute.DiagnosticService, _asyncListener),
                ValueTuple.Create(FeatureAttribute.ErrorSquiggles, _asyncListener));

            if (analyzerMap != null || updateSource == null)
            {
                AnalyzerService = CreateDiagnosticAnalyzerService(analyzerMap, new AggregateAsynchronousOperationListener(_listeners, FeatureAttribute.DiagnosticService));
            }

            if (updateSource == null)
            {
                updateSource = AnalyzerService;
            }

            _workspace = workspace;

            _registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            _registrationService.Register(workspace);

            DiagnosticService = new DiagnosticService(_listeners);
            DiagnosticService.Register(updateSource);

            if (createTaggerProvider)
            {
                var taggerProvider = this.TaggerProvider;
            }

            if (AnalyzerService != null)
            {
                _incrementalAnalyzers = ImmutableArray.Create(AnalyzerService.CreateIncrementalAnalyzer(workspace));
                _solutionCrawlerService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>() as SolutionCrawlerRegistrationService;
            }
        }

        public DiagnosticsSquiggleTaggerProvider TaggerProvider
        {
            get
            {
                if (_taggerProvider == null)
                {
                    WpfTestCase.RequireWpfFact($"{nameof(DiagnosticTaggerWrapper)}.{nameof(TaggerProvider)} creates asynchronous taggers");

                    _taggerProvider = new DiagnosticsSquiggleTaggerProvider(
                        _workspace.Services.GetService<IOptionService>(), DiagnosticService,
                        _workspace.GetService<IForegroundNotificationService>(), _listeners);
                }

                return _taggerProvider;
            }
        }



        public void Dispose()
        {
            _registrationService.Unregister(_workspace);
        }

        public async Task WaitForTags()
        {
            if (_solutionCrawlerService != null)
            {
                _solutionCrawlerService.WaitUntilCompletion_ForTestingPurposesOnly(_workspace, _incrementalAnalyzers);
            }

            await _asyncListener.CreateWaitTask();
        }

        private class MyDiagnosticAnalyzerService : DiagnosticAnalyzerService
        {
            internal MyDiagnosticAnalyzerService(
                ImmutableDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzersMap,
                IAsynchronousOperationListener listener)
                : base(new HostAnalyzerManager(ImmutableArray.Create<AnalyzerReference>(new TestAnalyzerReferenceByLanguage(analyzersMap)), hostDiagnosticUpdateSource: null),
                      hostDiagnosticUpdateSource: null,
                      registrationService: new MockDiagnosticUpdateSourceRegistrationService(),
                      listener: listener)
            {
            }
        }
    }

    public class DiagnosticsSquiggleTaggerProviderTests
    {
        [WpfFact(Skip = "xunit"), Trait(Traits.Feature, Traits.Features.Diagnostics)]
        public async Task Test_TagSourceDiffer()
        {
            var analyzer = new Analyzer();
            var analyzerMap = new Dictionary<string, DiagnosticAnalyzer[]>
            {
                { LanguageNames.CSharp, new DiagnosticAnalyzer[] { analyzer } }
            };

            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(new string[] { "class A { }", "class E { }" }, CSharpParseOptions.Default))
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
                    var text = await document.GetTextAsync();
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
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(new string[] { "class A {" }, CSharpParseOptions.Default))
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
            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(new string[] { "class C {" }, CSharpParseOptions.Default))
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

        [WpfFact]
        public async Task TestWithMockDiagnosticService_TaggerProviderCreatedBeforeInitialDiagnosticsReported()
        {
            // This test produces diagnostics from a mock service so that we are disconnected from
            // all teh asynchrony of hte actual async analyzer engine.  If this fails, then the 
            // issue is almost certainly in the DiagnosticsSquiggleTaggerProvider code.  If this
            // succeed, but other squiggle tests fail, then it is likely an issue with the 
            // diagnostics engine not actually reporting all diagnostics properly.

            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(new string[] { "class A { }" }, CSharpParseOptions.Default))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace))
            {
                var asyncListener = new AsynchronousOperationListener();
                var listeners = AsynchronousOperationListener.CreateListeners(
                    ValueTuple.Create(FeatureAttribute.DiagnosticService, asyncListener),
                    ValueTuple.Create(FeatureAttribute.ErrorSquiggles, asyncListener));

                var diagnosticService = new MockDiagnosticService(workspace);
                var provider = new DiagnosticsSquiggleTaggerProvider(
                        workspace.Services.GetService<IOptionService>(), diagnosticService,
                        workspace.GetService<IForegroundNotificationService>(), listeners);

                // Create the tagger before the first diagnostic event has been fired.
                var tagger = provider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());

                // Now product hte first diagnostic and fire the events.
                var tree = await workspace.CurrentSolution.Projects.Single().Documents.Single().GetSyntaxTreeAsync();
                var span = TextSpan.FromBounds(0, 5);
                diagnosticService.CreateDiagnosticAndFireEvents(Location.Create(tree, span));

                using (var disposable = tagger as IDisposable)
                {
                    await asyncListener.CreateWaitTask();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.Equal(1, spans.Count);
                    Assert.Equal(span.ToSpan(), spans[0].Span.Span);
                }
            }
        }

        [WpfFact]
        public async Task TestWithMockDiagnosticService_TaggerProviderCreatedAfterInitialDiagnosticsReported()
        {
            // This test produces diagnostics from a mock service so that we are disconnected from
            // all teh asynchrony of hte actual async analyzer engine.  If this fails, then the 
            // issue is almost certainly in the DiagnosticsSquiggleTaggerProvider code.  If this
            // succeed, but other squiggle tests fail, then it is likely an issue with the 
            // diagnostics engine not actually reporting all diagnostics properly.

            using (var workspace = await TestWorkspaceFactory.CreateCSharpWorkspaceAsync(new string[] { "class A { }" }, CSharpParseOptions.Default))
            using (var wrapper = new DiagnosticTaggerWrapper(workspace))
            {
                var asyncListener = new AsynchronousOperationListener();
                var listeners = AsynchronousOperationListener.CreateListeners(
                    ValueTuple.Create(FeatureAttribute.DiagnosticService, asyncListener),
                    ValueTuple.Create(FeatureAttribute.ErrorSquiggles, asyncListener));

                var diagnosticService = new MockDiagnosticService(workspace);
                var provider = new DiagnosticsSquiggleTaggerProvider(
                        workspace.Services.GetService<IOptionService>(), diagnosticService,
                        workspace.GetService<IForegroundNotificationService>(), listeners);

                // Create and fire the diagnostic events before hte tagger is even made.
                var tree = await workspace.CurrentSolution.Projects.Single().Documents.Single().GetSyntaxTreeAsync();
                var span = TextSpan.FromBounds(0, 5);
                diagnosticService.CreateDiagnosticAndFireEvents(Location.Create(tree, span));

                var tagger = provider.CreateTagger<IErrorTag>(workspace.Documents.First().GetTextBuffer());
                using (var disposable = tagger as IDisposable)
                {
                    await asyncListener.CreateWaitTask();

                    var snapshot = workspace.Documents.First().GetTextBuffer().CurrentSnapshot;
                    var spans = tagger.GetTags(snapshot.GetSnapshotSpanCollection()).ToList();
                    Assert.Equal(1, spans.Count);
                    Assert.Equal(span.ToSpan(), spans[0].Span.Span);
                }
            }
        }

        private class MockDiagnosticService : IDiagnosticService
        {
            public const string DiagnosticId = "MockId";

            private readonly Workspace _workspace;
            private DiagnosticData _diagnostic;

            public event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

            public MockDiagnosticService(Workspace workspace)
            {
                _workspace = workspace;
            }

            public IEnumerable<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken)
            {
                Assert.Equal(workspace, _workspace);
                Assert.Equal(projectId, GetProjectId());
                Assert.Equal(documentId, GetDocumentId());

                if (_diagnostic == null)
                {
                    yield break;
                }
                else
                {
                    yield return _diagnostic;
                }
            }

            public IEnumerable<UpdatedEventArgs> GetDiagnosticsUpdatedEventArgs(Workspace workspace, ProjectId projectId, DocumentId documentId, CancellationToken cancellationToken)
            {
                Assert.Equal(workspace, _workspace);
                Assert.Equal(projectId, GetProjectId());
                Assert.Equal(documentId, GetDocumentId());

                if (_diagnostic == null)
                {
                    yield break;
                }
                else
                {
                    yield return new UpdatedEventArgs(this, workspace, GetProjectId(), GetDocumentId());
                }
            }

            internal void CreateDiagnosticAndFireEvents(Location location)
            {
                var document = _workspace.CurrentSolution.Projects.Single().Documents.Single();
                _diagnostic = DiagnosticData.Create(document,
                    Diagnostic.Create(DiagnosticId, "MockCategory", "MockMessage", DiagnosticSeverity.Error, DiagnosticSeverity.Error, isEnabledByDefault: true, warningLevel: 0,
                    location: location));

                DiagnosticsUpdated?.Invoke(this, DiagnosticsUpdatedArgs.DiagnosticsCreated(
                    this, _workspace, _workspace.CurrentSolution,
                    GetProjectId(), GetDocumentId(),
                    ImmutableArray.Create(_diagnostic)));
            }

            private DocumentId GetDocumentId()
            {
                return _workspace.CurrentSolution.Projects.Single().Documents.Single().Id;
            }

            private ProjectId GetProjectId()
            {
                return _workspace.CurrentSolution.Projects.Single().Id;
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
