// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class DiagnosticTaggerWrapper<TProvider> : IDisposable
        where TProvider : AbstractDiagnosticsAdornmentTaggerProvider<IErrorTag>
    {
        private readonly TestWorkspace _workspace;
        public readonly DiagnosticAnalyzerService AnalyzerService;
        private readonly ISolutionCrawlerRegistrationService _registrationService;
        private readonly ImmutableArray<IIncrementalAnalyzer> _incrementalAnalyzers;
        private readonly SolutionCrawlerRegistrationService _solutionCrawlerService;
        private readonly AsynchronousOperationListener _asyncListener;
        public readonly DiagnosticService DiagnosticService;
        private readonly IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> _listeners;

        private ITaggerProvider _taggerProvider;

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
                AnalyzerService = CreateDiagnosticAnalyzerService(analyzerMap, _asyncListener);
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

        public ITaggerProvider TaggerProvider
        {
            get
            {
                if (_taggerProvider == null)
                {
                    WpfTestCase.RequireWpfFact($"{nameof(DiagnosticTaggerWrapper<TProvider>)}.{nameof(TaggerProvider)} creates asynchronous taggers");

                    if (typeof(TProvider) == typeof(DiagnosticsSquiggleTaggerProvider))
                    {
                        _taggerProvider = new DiagnosticsSquiggleTaggerProvider(
                            DiagnosticService, _workspace.GetService<IForegroundNotificationService>(), _listeners);
                    }
                    else if (typeof(TProvider) == typeof(DiagnosticsSuggestionTaggerProvider))
                    {
                        _taggerProvider = new DiagnosticsSuggestionTaggerProvider(
                            DiagnosticService,
                            _workspace.GetService<IForegroundNotificationService>(), _listeners);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
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
}
