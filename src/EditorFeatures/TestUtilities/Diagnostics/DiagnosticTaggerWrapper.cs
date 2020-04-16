﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
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
        public readonly DiagnosticService DiagnosticService;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        private ITaggerProvider _taggerProvider;

        public DiagnosticTaggerWrapper(
            TestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>> analyzerMap = null,
            IDiagnosticUpdateSource updateSource = null,
            bool createTaggerProvider = true)
        {
            _threadingContext = workspace.GetService<IThreadingContext>();
            _listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            if (updateSource == null)
            {
                updateSource = AnalyzerService = new MyDiagnosticAnalyzerService(_listenerProvider.GetListener(FeatureAttribute.DiagnosticService));
            }

            var analyzerReference = new TestAnalyzerReferenceByLanguage(analyzerMap ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences(new[] { analyzerReference }));

            _workspace = workspace;

            _registrationService = workspace.Services.GetService<ISolutionCrawlerRegistrationService>();
            _registrationService.Register(workspace);

            DiagnosticService = new DiagnosticService(_listenerProvider, Array.Empty<Lazy<IEventListener, EventListenerMetadata>>());
            DiagnosticService.Register(updateSource);

            if (createTaggerProvider)
            {
                _ = TaggerProvider;
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
                    WpfTestRunner.RequireWpfFact($"{nameof(DiagnosticTaggerWrapper<TProvider>)}.{nameof(TaggerProvider)} creates asynchronous taggers");

                    if (typeof(TProvider) == typeof(DiagnosticsSquiggleTaggerProvider))
                    {
                        _taggerProvider = new DiagnosticsSquiggleTaggerProvider(
                            _threadingContext,
                            DiagnosticService, _workspace.GetService<IForegroundNotificationService>(), _listenerProvider);
                    }
                    else if (typeof(TProvider) == typeof(DiagnosticsSuggestionTaggerProvider))
                    {
                        _taggerProvider = new DiagnosticsSuggestionTaggerProvider(
                            _threadingContext,
                            DiagnosticService,
                            _workspace.GetService<IForegroundNotificationService>(), _listenerProvider);
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
            => _registrationService.Unregister(_workspace);

        public async Task WaitForTags()
        {
            if (_solutionCrawlerService != null)
            {
                _solutionCrawlerService.WaitUntilCompletion_ForTestingPurposesOnly(_workspace, _incrementalAnalyzers);
            }

            await _listenerProvider.GetWaiter(FeatureAttribute.DiagnosticService).ExpeditedWaitAsync();
            await _listenerProvider.GetWaiter(FeatureAttribute.ErrorSquiggles).ExpeditedWaitAsync();
        }

        private class MyDiagnosticAnalyzerService : DiagnosticAnalyzerService
        {
            internal MyDiagnosticAnalyzerService(IAsynchronousOperationListener listener)
                : base(new MockDiagnosticUpdateSourceRegistrationService(), listener)
            {
            }
        }
    }
}
