// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.InlineDiagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
{
    internal class DiagnosticTaggerWrapper<TProvider, TTag>
        where TProvider : AbstractDiagnosticsTaggerProvider<TTag>
        where TTag : ITag
    {
        private readonly EditorTestWorkspace _workspace;
        private readonly IThreadingContext _threadingContext;
        private readonly IAsynchronousOperationListenerProvider _listenerProvider;

        private AbstractDiagnosticsTaggerProvider<TTag>? _taggerProvider;

        public DiagnosticTaggerWrapper(
            EditorTestWorkspace workspace,
            IReadOnlyDictionary<string, ImmutableArray<DiagnosticAnalyzer>>? analyzerMap = null,
            bool createTaggerProvider = true)
        {
            _threadingContext = workspace.GetService<IThreadingContext>();
            _listenerProvider = workspace.GetService<IAsynchronousOperationListenerProvider>();

            var analyzerReference = new TestAnalyzerReferenceByLanguage(analyzerMap ?? DiagnosticExtensions.GetCompilerDiagnosticAnalyzersMap());
            workspace.TryApplyChanges(workspace.CurrentSolution.WithAnalyzerReferences([analyzerReference]));

            // Change the background analysis scope to OpenFiles instead of ActiveFile (default),
            // so that every diagnostic tagger test does not need to mark test files as "active" file.
            workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.CSharp, BackgroundAnalysisScope.OpenFiles);
            workspace.GlobalOptions.SetGlobalOption(SolutionCrawlerOptionsStorage.BackgroundAnalysisScopeOption, LanguageNames.VisualBasic, BackgroundAnalysisScope.OpenFiles);

            _workspace = workspace;

            if (createTaggerProvider)
            {
                _ = TaggerProvider;
            }
        }

        public AbstractDiagnosticsTaggerProvider<TTag> TaggerProvider
        {
            get
            {
                if (_taggerProvider == null)
                {
                    WpfTestRunner.RequireWpfFact($"{nameof(DiagnosticTaggerWrapper<TProvider, TTag>)}.{nameof(TaggerProvider)} creates asynchronous taggers");

                    if (typeof(TProvider) == typeof(InlineDiagnosticsTaggerProvider))
                    {
                        _taggerProvider = (AbstractDiagnosticsTaggerProvider<TTag>)(object)_workspace.ExportProvider.GetExportedValues<ITaggerProvider>()
                            .OfType<TProvider>()
                            .Single();
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }

                return _taggerProvider;
            }
        }

        public async Task WaitForTags()
        {
            await _listenerProvider.WaitAllDispatcherOperationAndTasksAsync(
                _workspace,
                FeatureAttribute.Workspace,
                FeatureAttribute.SolutionCrawlerLegacy,
                FeatureAttribute.DiagnosticService,
                FeatureAttribute.ErrorSquiggles,
                FeatureAttribute.Classification);
        }
    }
}
