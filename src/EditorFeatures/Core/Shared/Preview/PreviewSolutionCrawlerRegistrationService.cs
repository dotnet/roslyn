// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Preview
{
    [ExportWorkspaceService(typeof(ISolutionCrawlerRegistrationService), WorkspaceKind.Preview), Shared]
    internal class PreviewSolutionCrawlerRegistrationService : ISolutionCrawlerRegistrationService
    {
        private static readonly ConditionalWeakTable<Workspace, CancellationTokenSource> s_cancellationTokens =
            new ConditionalWeakTable<Workspace, CancellationTokenSource>();

        private readonly IIncrementalAnalyzerProvider _provider;

        [ImportingConstructor]
        public PreviewSolutionCrawlerRegistrationService(IDiagnosticAnalyzerService diagnosticService)
        {
            _provider = diagnosticService as IIncrementalAnalyzerProvider;
            Contract.ThrowIfNull(_provider);
        }

        public async void Register(Workspace workspace)
        {
            try
            {
                var workerBackOffTimeSpanInMS = workspace.Options.GetOption(InternalSolutionCrawlerOptions.PreviewBackOffTimeSpanInMS);

                var analyzer = _provider.CreateIncrementalAnalyzer(workspace);
                var source = s_cancellationTokens.GetValue(workspace, _ => new CancellationTokenSource());

                var solution = workspace.CurrentSolution;
                foreach (var documentId in workspace.GetOpenDocumentIds())
                {
                    var document = solution.GetDocument(documentId);
                    if (document == null)
                    {
                        continue;
                    }

                    // delay analyzing
                    await Task.Delay(workerBackOffTimeSpanInMS).ConfigureAwait(false);

                    // do actual analysis
                    await analyzer.AnalyzeSyntaxAsync(document, source.Token).ConfigureAwait(false);
                    await analyzer.AnalyzeDocumentAsync(document, bodyOpt: null, cancellationToken: source.Token).ConfigureAwait(false);

                    // don't call project one.
                }
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
        }

        public void Unregister(Workspace workspace, bool blockingShutdown = false)
        {
            CancellationTokenSource source;
            if (s_cancellationTokens.TryGetValue(workspace, out source))
            {
                source.Cancel();
            }
        }
    }
}
